namespace FictionChecker.Grammar;

/// <summary>A test on one token of a sentence.</summary>
internal delegate bool WordTest(Sentence sentence, int index);

/// <summary>The tests rules are written with.</summary>
internal static class Match
{
    /// <summary>Any word.</summary>
    public static WordTest AnyWord { get; } = static (s, i) => s.IsWord(i);

    /// <summary>One of the given words, ignoring capitals.</summary>
    public static WordTest Word(params string[] words)
    {
        var set = new HashSet<string>(words, StringComparer.Ordinal);
        return (s, i) => s.IsWord(i) && set.Contains(s.Lower(i));
    }

    /// <summary>A word of one of the given types.</summary>
    public static WordTest Type(params WordType[] types) => (s, i) => s.IsWord(i) && Array.IndexOf(types, s.Type(i)) >= 0;

    /// <summary>A function word with all the given traits.</summary>
    public static WordTest Trait(WordTraits traits) => (s, i) => (s.Traits(i) & traits) == traits;

    /// <summary>A word that is one of the given forms.</summary>
    public static WordTest Form(WordForms forms) => (s, i) => s.IsWord(i) && (s.Forms(i) & forms) != 0;

    /// <summary>Passes when the test fails.</summary>
    public static WordTest Not(WordTest test) => (s, i) => !test(s, i);

    /// <summary>Passes when every test does.</summary>
    public static WordTest All(params WordTest[] tests) => (s, i) => Array.TrueForAll(tests, t => t(s, i));

    /// <summary>Passes when any test does.</summary>
    public static WordTest Any(params WordTest[] tests) => (s, i) => Array.Exists(tests, t => t(s, i));
}

/// <summary>
/// A run of tests that must pass on tokens standing side by side, such as "could", then "of". Punctuation
/// between two words breaks a match, because punctuation is a token too and no word test passes on it.
/// </summary>
internal sealed class Pattern(params WordTest[] steps)
{
    /// <summary>Number of tokens a match covers.</summary>
    public int Length => steps.Length;

    /// <summary>Whether the pattern matches starting at a token.</summary>
    public bool MatchesAt(Sentence sentence, int index)
    {
        if (index < 0 || index + steps.Length > sentence.Count)
        {
            return false;
        }

        for (int k = 0; k < steps.Length; k++)
        {
            if (!steps[k](sentence, index + k))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Every position where the pattern matches.</summary>
    public IEnumerable<int> FindIn(Sentence sentence)
    {
        for (int i = 0; i + steps.Length <= sentence.Count; i++)
        {
            if (MatchesAt(sentence, i))
            {
                yield return i;
            }
        }
    }
}
