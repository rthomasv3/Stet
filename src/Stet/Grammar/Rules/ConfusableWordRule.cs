namespace Stet.Grammar.Rules;

/// <summary>
/// Flags a word that is often confused with another ("their" for "there") when the words on either side
/// say, strongly, that the other was meant. One rule per family of words, so each can be switched off alone.
/// </summary>
internal sealed class ConfusableWordRule : GrammarRule
{
    private readonly ConfusableFamily _family;
    private readonly Func<ConfusableData> _data;
    private readonly int _number;
    private readonly RuleInfo _info;

    public ConfusableWordRule(ConfusableFamily family, Func<ConfusableData> data)
    {
        _family = family;
        _data = data;
        _number = Array.IndexOf(ConfusableSets.Families, family);

        _info = new RuleInfo(
            family.RuleId,
            family.Name,
            $"Flags one of these words where the words around it show that another was meant. {family.Explanation}",
            RuleCategory.ConfusableWords,
            ConfusableSets.OnByDefault(family.RuleId),
            false,
            family.Flagged,
            family.Accepted);
    }

    public override RuleInfo Info => _info;

    public override void Check(Sentence sentence, List<Finding> findings)
    {
        foreach ((int i, int family, bool first) in sentence.Confusables())
        {
            if (family != _number)
            {
                continue;
            }

            // A capital in the middle of a sentence marks a name or a title, which follows its own rules.
            // So does a capital followed by another capital, even at the start ("Than Kosis looked up").
            // A word all in capitals is shouting, not a name, and is checked like any other.
            if (sentence.IsCapitalized(i) && !(sentence.IsAllCapitals(i) && sentence.Token(i).Length > 1) && (!first || (sentence.IsWord(i + 1) && sentence.IsCapitalized(i + 1) && !sentence.Is(i + 1, "i"))))
            {
                continue;
            }

            ConfusableData data = _data();
            if (!ConfusableScorer.TryScore(sentence, i, data, out ConfusableEvidence evidence))
            {
                continue;
            }

            string[] members = data.Members(evidence.Group);
            string? meant = ClearCase(sentence, i, members[evidence.Written], members[evidence.Best], evidence, data);
            if (meant is null && ConfusableSets.IsStrongEnough(members[evidence.Written], members[evidence.Best], evidence))
            {
                meant = members[evidence.Best];
            }

            if (meant is null)
            {
                continue;
            }

            var token = sentence.Token(i);
            string fix = MatchWriting(sentence, i, meant);
            findings.Add(new Finding(
                token.Start,
                token.Length,
                $"{CharClassQuote.Open}{sentence.Written(i)}{CharClassQuote.Close} looks like it should be {CharClassQuote.Open}{fix}{CharClassQuote.Close}.",
                _family.Explanation,
                [Replace(token.Start, token.Length, fix)]));
        }
    }

    // Cases that the words two places away settle, which the counts of neighbouring pairs cannot see.
    private static string? ClearCase(Sentence sentence, int i, string written, string best, ConfusableEvidence evidence, ConfusableData data)
    {
        const int Bit = ConfusableData.Unit;
        switch (written)
        {
            case "their" or "there" or "your" or "whose" or "its":
                // "your going to be late": before "going to" and a verb only the contraction can stand.
                bool goingTo = sentence.Is(i + 1, "going") && sentence.Is(i + 2, "to") && sentence.IsWord(i + 3)
                    && (sentence.Type(i + 3) == WordType.Verb || (sentence.Forms(i + 3) & WordForms.VerbBase) != 0);
                if ((goingTo || sentence.Is(i + 1, "gonna")) && !(i > 0 && sentence.IsWord(i - 1) && sentence.Type(i - 1) == WordType.Adposition))
                {
                    // "Is there going to be trouble?" and "the stuff you put there going to waste" are right,
                    // so "there" needs a subject's place: the start, or after "and", "that", "I think".
                    if (written == "there" && i > 0 && sentence.IsWord(i - 1) && !sentence.Is(i - 1, "and", "but", "that", "because", "if", "so", "think", "thought", "hope", "know", "knew", "guess", "bet", "suppose", "believe", "say", "said", "says", "sure", "afraid", "like"))
                    {
                        break;
                    }


                    return written switch
                    {
                        "your" => "you're",
                        "whose" => "who's",
                        "its" => "it's",
                        _ => "they're",
                    };
                }

                // "your not going", "your the one": these words do not follow "your".
                // ("Your not being there" is right: "your" can own an -ing word, so those are left to the counts.)
                if (written == "your" && best == "you're" && evidence.Right >= 6 * Bit && sentence.Is(i + 1, "not", "a", "an", "the", "never", "always", "probably", "already")
                    && !(sentence.IsWord(i + 2) && (sentence.Forms(i + 2) & WordForms.VerbGerund) != 0 && !sentence.Is(i + 2, "going")))
                {
                    return "you're";
                }

                break;
            case "it's":
                // "licked it's paw", "in it's place": after a verb or a word like "in", before a thing that
                // "its" often goes with, "it is" cannot be meant.
                if (best == "its" && evidence.Right >= 6 * Bit && evidence.Total >= 7 * Bit && i > 0 && sentence.IsWord(i - 1)
                    && sentence.Type(i - 1) is WordType.Verb or WordType.Adposition
                    && sentence.IsWord(i + 1) && sentence.Type(i + 1) is WordType.Noun or WordType.Adjective)
                {
                    return "its";
                }

                break;
            case "peace":
                // "a peace of cloth": a part of something. Only "a peace of mind" is right as written.
                if (i > 0 && sentence.Is(i - 1, "a") && sentence.Is(i + 1, "of") && sentence.IsWord(i + 2) && !sentence.Is(i + 2, "mind", "sorts", "a"))
                {
                    return "piece";
                }

                break;
            case "to":
                // "to late to go", "to much.": "too" and a word it often goes with, then the end of the
                // thought. With something else after ("to long for home") "to" may be right.
                if (best == "too" && evidence.Right >= 3 * Bit && evidence.Total >= 6 * Bit && sentence.IsWord(i + 1) && EndsThought(sentence, i + 2))
                {
                    return "too";
                }

                break;
            case "off":
                // "out off", "because off": after these words only "of" can follow.
                if (i > 0 && sentence.Is(i - 1, "out", "instead", "because", "kind", "sort", "none", "several", "plenty", "couple", "number") && sentence.IsWord(i + 1) && sentence.TouchesNext(i - 1))
                {
                    return "of";
                }

                // "one off the best": "off" where "of" fits better. A correct "off" often looks the same
                // from the words beside it ("cut the end off the rope"), but then an earlier verb of the
                // clause goes with "off" ("cut off"). With no such word, less evidence is enough.
                if (best == "of" && evidence.Total >= 10 * Bit && ConfusableScorer.EarlierSupport(sentence, i, evidence.Group, evidence.Written, evidence.Best, data, 6) <= 0)
                {
                    return "of";
                }

                break;
            case "of":
                // "turned of the light": the word before goes with "off" far more than with "of", and the
                // thing follows directly. ("Took" is not enough: "the view she took of her position".)
                if (best == "off" && evidence.Left >= 13 * Bit / 2 && sentence.Is(i + 1, "the", "a", "an", "his", "her", "my", "your", "their", "its", "our"))
                {
                    return "off";
                }

                break;
        }

        return null;
    }

    // Whether a thought ends at a position: the sentence does, or a punctuation mark or "to" or "for" stands there.
    private static bool EndsThought(Sentence sentence, int i)
    {
        if (i >= sentence.Count)
        {
            return true;
        }

        if (sentence.IsWord(i))
        {
            return sentence.Is(i, "to", "for");
        }

        return sentence.Token(i).Kind == Segmentation.TokenKind.Punctuation && sentence.Text[sentence.Token(i).Start] is ',' or '.' or '!' or '?' or ';' or ':';
    }

    // The replacement with the capitals and the kind of apostrophe the writer is using.
    private static string MatchWriting(Sentence sentence, int i, string word)
    {
        ReadOnlySpan<char> written = sentence.Written(i);
        if (sentence.IsAllCapitals(i) && written.Length > 1)
        {
            word = word.ToUpperInvariant();
        }
        else if (sentence.IsCapitalized(i))
        {
            word = char.ToUpperInvariant(word[0]) + word[1..];
        }

        if (word.Contains('\'') && !written.Contains('\'') && (written.Contains(Segmentation.CharClass.RightSingleQuote) || sentence.Text.Contains(Segmentation.CharClass.RightSingleQuote)))
        {
            word = word.Replace('\'', Segmentation.CharClass.RightSingleQuote);
        }

        return word;
    }
}
