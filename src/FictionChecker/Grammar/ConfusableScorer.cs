using FictionChecker.Segmentation;

namespace FictionChecker.Grammar;

/// <summary>
/// What the neighbours of a confusable word say about it. Values are differences of log counts, in steps of
/// 1/<see cref="ConfusableData.Unit"/> of a doubling: positive means the other word fits better.
/// </summary>
/// <param name="Group">The group the word belongs to.</param>
/// <param name="Written">Which word of the group was written.</param>
/// <param name="Best">The word of the group that fits best, other than the written one.</param>
/// <param name="Left">How much better the best word fits the word before.</param>
/// <param name="Right">How much better the best word fits the word after.</param>
/// <param name="Prior">How much more common the best word is overall.</param>
/// <param name="LeftKnown">Whether the word before had counts of its own, not only those of its word type.</param>
/// <param name="RightKnown">The same for the word after.</param>
internal readonly record struct ConfusableEvidence(int Group, int Written, int Best, int Left, int Right, int Prior, bool LeftKnown, bool RightKnown)
{
    /// <summary>
    /// How much more likely the best word is than the written one, given both neighbours. Each side's count
    /// already includes how common the word is, so one of the two is taken out again.
    /// </summary>
    public int Total => Left + Right - Prior;
}

/// <summary>Scores a confusable word against the other words of its group using the words on either side.</summary>
internal static class ConfusableScorer
{
    /// <summary>Scores the word at a position, or returns false when it is not a confusable word.</summary>
    public static bool TryScore(Sentence sentence, int i, ConfusableData data, out ConfusableEvidence evidence)
    {
        evidence = default;
        if (!sentence.IsWord(i) || !data.TryFind(sentence.Lower(i), out int group, out int written))
        {
            return false;
        }

        int members = data.Members(group).Length;
        Span<int> left = stackalloc int[members];
        Span<int> right = stackalloc int[members];
        Beside(sentence, i, group, data, left, right, out bool leftKnown, out bool rightKnown);
        ReadOnlySpan<byte> counts = data.Counts(group);

        int best = -1;
        int bestTotal = int.MinValue;
        for (int m = 0; m < members; m++)
        {
            if (m == written)
            {
                continue;
            }

            int total = (left[m] - left[written]) + (right[m] - right[written]) - (counts[m] - counts[written]);
            if (total > bestTotal)
            {
                best = m;
                bestTotal = total;
            }
        }

        evidence = new ConfusableEvidence(group, written, best, left[best] - left[written], right[best] - right[written], counts[best] - counts[written], leftKnown, rightKnown);
        return true;
    }

    /// <summary>Fills in each member's count with the word before and with the word after.</summary>
    public static void Beside(Sentence sentence, int i, int group, ConfusableData data, Span<int> left, Span<int> right, out bool leftKnown, out bool rightKnown)
    {
        Neighbour(sentence, i - 1, true, data, out int leftId, out int leftTag);
        Neighbour(sentence, i + 1, false, data, out int rightId, out int rightTag);
        leftKnown = data.Beside(group, true, leftId, leftTag, left);
        rightKnown = data.Beside(group, false, rightId, rightTag, right);
    }

    /// <summary>
    /// How much more an earlier verb of the clause goes with one member than another when directly before
    /// it: for "cut" in "cut the end off", how much more often "cut off" is seen than "cut of". Looks at up
    /// to <paramref name="reach"/> words before the neighbour, stops at punctuation, and returns the
    /// largest difference found, or <see cref="int.MinValue"/> when no earlier verb has counts.
    /// </summary>
    public static int EarlierSupport(Sentence sentence, int i, int group, int member, int other, ConfusableData data, int reach)
    {
        int best = int.MinValue;
        Span<int> values = stackalloc int[data.Members(group).Length];
        for (int j = i - 2; j >= 0 && j >= i - 1 - reach; j--)
        {
            if (!sentence.IsWord(j))
            {
                break;
            }

            Neighbour(sentence, j, true, data, out int id, out int tag);
            if (sentence.Type(j) == WordType.Verb && data.Beside(group, true, id, tag, values))
            {
                best = Math.Max(best, values[member] - values[other]);
            }
        }

        return best;
    }

    /// <summary>
    /// Describes the token beside a confusable word the way the data stores neighbours: a word of the
    /// lexicon, a punctuation mark or contraction tail, or failing those only a word type.
    /// </summary>
    private static void Neighbour(Sentence sentence, int i, bool left, ConfusableData data, out int id, out int tag)
    {
        id = -1;
        if (i < 0 || i >= sentence.Count)
        {
            tag = left ? ConfusableData.StartTag : ConfusableData.EndTag;
            return;
        }

        Token token = sentence.Token(i);
        tag = (int)sentence.Type(i);
        switch (token.Kind)
        {
            case TokenKind.Word:
                string lower = sentence.Lower(i);
                int apostrophe = lower.LastIndexOf('\'');
                if (apostrophe <= 0)
                {
                    id = sentence.WordIndex(i);
                }
                else if (lower.EndsWith("n't", StringComparison.Ordinal))
                {
                    // The data writes "don't" as "do not".
                    ReadOnlySpan<char> part = left ? "not" : lower switch
                    {
                        "can't" => "can",
                        "won't" => "will",
                        "shan't" => "shall",
                        _ => lower.AsSpan(0, lower.Length - 3),
                    };
                    id = sentence.Data.Lexicon.IndexOf(part);
                }
                else if (data.SpecialId(lower[apostrophe..]) is >= 0 and int tail)
                {
                    // "they're" is "they" then "'re": whichever part is next to the confusable word counts.
                    id = left ? tail : sentence.Data.Lexicon.IndexOf(lower.AsSpan(0, apostrophe));
                }

                break;
            case TokenKind.Punctuation:
                id = data.SpecialId(Mark(sentence.Text, token));
                break;
            case TokenKind.Number:
                tag = (int)WordType.Number;
                break;
            default:
                tag = (int)WordType.Noun;
                break;
        }
    }

    // The punctuation mark as the data writes it: straight quotes, "--" for a long dash, "..." for an ellipsis.
    private static string Mark(string text, Token token)
    {
        char c = text[token.Start];
        bool Doubled(char mark) => (token.End < text.Length && text[token.End] == mark) || (token.Start > 0 && text[token.Start - 1] == mark) || token.Length > 1;
        return c switch
        {
            '.' => Doubled('.') ? "..." : ".",
            CharClass.Ellipsis => "...",
            '-' => Doubled('-') ? "--" : "-",
            CharClass.EmDash or CharClass.HorizontalBar => "--",
            CharClass.EnDash or CharClass.Hyphen => "-",
            _ when CharClass.IsDoubleQuote(c) => "\"",
            _ when CharClass.IsSingleQuote(c) => "'",
            _ => c.ToString(),
        };
    }
}
