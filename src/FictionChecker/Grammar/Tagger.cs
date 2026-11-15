using FictionChecker.Segmentation;
using FictionChecker.Spelling;

namespace FictionChecker.Grammar;

/// <summary>
/// Picks each word's type in its sentence. For every word it knows how likely each type is, and from the
/// Google Books counts how likely one type is to follow another; it then takes the most likely sequence for
/// the whole sentence (a hidden Markov model solved with the Viterbi method). All arithmetic is in whole
/// numbers, so the result is the same on every machine.
/// </summary>
internal sealed class Tagger(WordData data)
{
    private const int Types = WordData.TypeCount;
    private const int Blocked = int.MaxValue / 4;

    // A word with nothing known is most likely a noun, then an adjective, then a verb.
    private static readonly byte[] UnknownCosts = Costs((WordType.Noun, 1), (WordType.Adjective, 2), (WordType.Verb, 3), (WordType.Adverb, 5));
    private static readonly byte[] LyCosts = Costs((WordType.Adverb, 0), (WordType.Adjective, 3));
    private static readonly byte[] IngCosts = Costs((WordType.Verb, 1), (WordType.Noun, 2), (WordType.Adjective, 3));
    private static readonly byte[] EdCosts = Costs((WordType.Verb, 1), (WordType.Adjective, 2));
    private static readonly byte[] SCosts = Costs((WordType.Noun, 1), (WordType.Verb, 2));

    private readonly Lexicon _lexicon = data.Lexicon;

    /// <summary>
    /// Tags the tokens of one sentence. <paramref name="types"/> receives one type per token, and
    /// <paramref name="words"/> the lexicon position of each word (or -1).
    /// </summary>
    public void Tag(string text, ReadOnlySpan<Token> tokens, CustomWords custom, Span<WordType> types, Span<int> words)
    {
        int n = tokens.Length;
        if (n == 0)
        {
            return;
        }

        // cost[i * Types + t]: the cheapest way to reach token i as type t. back: the type before it on that way.
        int[] cost = new int[n * Types];
        byte[] back = new byte[n * Types];
        byte[] leaving = new byte[n * Types];
        short[]?[] after = new short[]?[n];
        Span<char> lowerBuffer = stackalloc char[64];
        Span<byte> wordCosts = stackalloc byte[Types];
        Span<char> buffer = stackalloc char[64];
        bool first = true;
        for (int i = 0; i < n; i++)
        {
            Token token = tokens[i];
            words[i] = -1;
            WordType? fixedType = null;
            WordType? leaves = null;
            switch (token.Kind)
            {
                case TokenKind.Punctuation:
                    fixedType = WordType.Punctuation;
                    break;
                case TokenKind.Number:
                    fixedType = WordType.Number;
                    break;
                case TokenKind.Word when token.Length <= 64:
                    ReadOnlySpan<char> word = Normalize(token.TextIn(text), buffer);
                    Describe(word, first, custom, wordCosts, out words[i], out fixedType, out leaves);
                    after[i] = data.CostsAfter(Key(word, lowerBuffer));
                    first = false;
                    break;
                default:
                    fixedType = WordType.Noun;
                    first = false;
                    break;
            }

            for (int t = 0; t < Types; t++)
            {
                int emit = fixedType is { } only
                    ? (t == (int)only ? 0 : Blocked)
                    : wordCosts[t] >= WordData.Never ? Blocked : (wordCosts[t] * WordData.CostUnit) - data.TypeCost(t);
                int best = Blocked;
                int from = 0;
                if (emit < Blocked)
                {
                    if (i == 0)
                    {
                        best = data.TransitionCost(WordData.StartState, t);
                    }
                    else
                    {
                        for (int p = 0; p < Types; p++)
                        {
                            int before = cost[((i - 1) * Types) + p];
                            if (before >= Blocked)
                            {
                                continue;
                            }

                            int total = before + (after[i - 1] is { } known ? known[t] : data.TransitionCost(leaving[((i - 1) * Types) + p], t));
                            if (total < best)
                            {
                                best = total;
                                from = p;
                            }
                        }
                    }

                    best = best >= Blocked ? Blocked : best + emit;
                }

                cost[(i * Types) + t] = best;
                back[(i * Types) + t] = (byte)from;

                // "I'm" arrives as a pronoun and leaves as a verb.
                leaving[(i * Types) + t] = (byte)(leaves ?? (WordType)t);
            }
        }

        int last = 0;
        int lastCost = int.MaxValue;
        for (int t = 0; t < Types; t++)
        {
            int c = cost[((n - 1) * Types) + t];
            int toEnd = after[n - 1] is { } known ? known[Types] : data.TransitionCost(leaving[((n - 1) * Types) + t], WordData.EndState);
            if (c < Blocked && c + toEnd < lastCost)
            {
                lastCost = c + toEnd;
                last = t;
            }
        }

        for (int i = n - 1; i >= 0; i--)
        {
            types[i] = (WordType)last;
            last = back[(i * Types) + last];
        }
    }

    /// <summary>A word with curly apostrophes and unusual hyphens made plain, as the lexicon spells it.</summary>
    public static ReadOnlySpan<char> Normalize(ReadOnlySpan<char> word, Span<char> buffer)
    {
        Span<char> plain = buffer[..word.Length];
        for (int i = 0; i < word.Length; i++)
        {
            char c = word[i];
            plain[i] = CharClass.IsApostropheVariant(c) ? '\'' : CharClass.IsHyphen(c) ? '-' : c;
        }

        return plain;
    }

    // The name a word's following-type costs are stored under: the word in lowercase, or for a contraction its
    // tail ("n't", "'ll"), which is how the Google Books data splits them.
    private static ReadOnlySpan<char> Key(ReadOnlySpan<char> word, Span<char> buffer)
    {
        if (word.EndsWith("n't", StringComparison.OrdinalIgnoreCase))
        {
            return "n't";
        }

        int apostrophe = word.LastIndexOf('\'');
        ReadOnlySpan<char> part = apostrophe > 0 ? word[apostrophe..] : word;
        Span<char> lower = buffer[..part.Length];
        part.ToLowerInvariant(lower);
        return lower;
    }

    /// <summary>The lexicon position of a word as written, or of its lowercase form, or -1.</summary>
    public int Find(ReadOnlySpan<char> word)
    {
        int index = _lexicon.IndexOf(word);
        if (index >= 0 || word.Length > 64)
        {
            return index;
        }

        Span<char> lower = stackalloc char[word.Length];
        word.ToLowerInvariant(lower);
        return _lexicon.IndexOf(lower);
    }

    private void Describe(ReadOnlySpan<char> word, bool sentenceStart, CustomWords custom, Span<byte> costs, out int index, out WordType? fixedType, out WordType? leaves)
    {
        fixedType = null;
        leaves = null;
        index = -1;
        bool capitalized = char.IsUpper(word[0]);
        bool allCaps = capitalized && word.Length > 1 && !word[1..].ContainsAnyInRange('a', 'z');

        // Contractions: the Google Books data splits them, so they are described by their parts.
        int apostrophe = word.LastIndexOf('\'');
        if (apostrophe > 0)
        {
            ReadOnlySpan<char> ending = word[apostrophe..];
            if (word.EndsWith("n't", StringComparison.OrdinalIgnoreCase))
            {
                index = Find(word);
                fixedType = WordType.Verb;
                leaves = WordType.Adverb;
                return;
            }

            if (ending.Equals("'m", StringComparison.OrdinalIgnoreCase) || ending.Equals("'re", StringComparison.OrdinalIgnoreCase)
                || ending.Equals("'ve", StringComparison.OrdinalIgnoreCase) || ending.Equals("'ll", StringComparison.OrdinalIgnoreCase)
                || ending.Equals("'d", StringComparison.OrdinalIgnoreCase))
            {
                index = Find(word);
                leaves = WordType.Verb;
                word = word[..apostrophe];
            }
            else if (ending.Equals("'s", StringComparison.OrdinalIgnoreCase))
            {
                index = Find(word);
                word = word[..apostrophe];
                if (word.Equals("it", StringComparison.OrdinalIgnoreCase) || word.Equals("he", StringComparison.OrdinalIgnoreCase)
                    || word.Equals("she", StringComparison.OrdinalIgnoreCase) || word.Equals("that", StringComparison.OrdinalIgnoreCase)
                    || word.Equals("there", StringComparison.OrdinalIgnoreCase) || word.Equals("what", StringComparison.OrdinalIgnoreCase)
                    || word.Equals("who", StringComparison.OrdinalIgnoreCase) || word.Equals("here", StringComparison.OrdinalIgnoreCase)
                    || word.Equals("let", StringComparison.OrdinalIgnoreCase))
                {
                    leaves = WordType.Verb;
                }
            }
        }

        // The writer's own words and capitalized words inside a sentence are names.
        bool isCustom = !custom.IsEmpty && custom.Contains(word, false);
        if (word.Length == 1 && word[0] == 'I')
        {
            capitalized = false;
        }

        int exact = _lexicon.IndexOf(word);
        int found = exact >= 0 ? exact : Find(word);
        if (index < 0)
        {
            index = found;
        }

        if ((isCustom && found < 0) || (capitalized && !allCaps && !sentenceStart) || (isCustom && capitalized))
        {
            fixedType = WordType.Noun;
            return;
        }

        // At the start of a sentence the capital says nothing, so the lowercase word is preferred.
        int lookup = found;
        if (capitalized && exact >= 0)
        {
            int lower = Find(word);
            Span<char> lowered = stackalloc char[word.Length];
            word.ToLowerInvariant(lowered);
            int lowerIndex = _lexicon.IndexOf(lowered);
            lookup = lowerIndex >= 0 ? lowerIndex : lower;
        }

        if (lookup >= 0 && data.TryGetTypeCosts(lookup, costs))
        {
            return;
        }

        if (capitalized && lookup < 0)
        {
            fixedType = WordType.Noun;
            return;
        }

        Guess(word).CopyTo(costs);
    }

    private static byte[] Guess(ReadOnlySpan<char> word)
    {
        if (word.Length > 4)
        {
            if (word.EndsWith("ly", StringComparison.OrdinalIgnoreCase))
            {
                return LyCosts;
            }

            if (word.EndsWith("ing", StringComparison.OrdinalIgnoreCase) || word.EndsWith("in'", StringComparison.OrdinalIgnoreCase))
            {
                return IngCosts;
            }

            if (word.EndsWith("ed", StringComparison.OrdinalIgnoreCase))
            {
                return EdCosts;
            }

            if (word.EndsWith('s') && !word.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
            {
                return SCosts;
            }
        }

        return UnknownCosts;
    }

    private static byte[] Costs(params (WordType Type, byte Cost)[] known)
    {
        byte[] costs = new byte[Types];
        Array.Fill(costs, (byte)WordData.Never);
        foreach ((WordType type, byte cost) in known)
        {
            costs[(int)type] = cost;
        }

        return costs;
    }
}
