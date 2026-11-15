using System.Collections.Frozen;

namespace FictionChecker.Grammar;

/// <summary>Facts about a function word that rules use.</summary>
[Flags]
internal enum WordTraits : uint
{
    None = 0,
    Pronoun = 1 << 0,
    Subject = 1 << 1,
    Object = 1 << 2,
    Possessive = 1 << 3,
    Reflexive = 1 << 4,
    FirstPerson = 1 << 5,
    SecondPerson = 1 << 6,
    ThirdPerson = 1 << 7,
    Singular = 1 << 8,
    Plural = 1 << 9,
    Be = 1 << 10,
    Have = 1 << 11,
    Do = 1 << 12,
    Modal = 1 << 13,
    Present = 1 << 14,
    Past = 1 << 15,
    Base = 1 << 16,
    Participle = 1 << 17,
    Determiner = 1 << 18,
    Article = 1 << 19,
    Demonstrative = 1 << 20,
    Quantifier = 1 << 21,
    Preposition = 1 << 22,
    Conjunction = 1 << 23,
    Subordinator = 1 << 24,
    Adverb = 1 << 25,
    Negative = 1 << 26,
    Question = 1 << 27,
    Clitic = 1 << 28,
}

/// <summary>The hand-written list of function words (pronouns, determiners, auxiliaries and the like).</summary>
internal sealed class FunctionWords
{
    private const string ResourceName = "FictionChecker.Data.Lists.closed.txt";

    private static readonly Dictionary<string, WordTraits> TraitNames = new(StringComparer.Ordinal)
    {
        ["pron"] = WordTraits.Pronoun, ["subj"] = WordTraits.Subject, ["obj"] = WordTraits.Object, ["poss"] = WordTraits.Possessive,
        ["refl"] = WordTraits.Reflexive, ["p1"] = WordTraits.FirstPerson, ["p2"] = WordTraits.SecondPerson, ["p3"] = WordTraits.ThirdPerson,
        ["sg"] = WordTraits.Singular, ["pl"] = WordTraits.Plural, ["be"] = WordTraits.Be, ["have"] = WordTraits.Have, ["do"] = WordTraits.Do,
        ["modal"] = WordTraits.Modal, ["pres"] = WordTraits.Present, ["past"] = WordTraits.Past, ["base"] = WordTraits.Base,
        ["part"] = WordTraits.Participle, ["det"] = WordTraits.Determiner, ["art"] = WordTraits.Article, ["dem"] = WordTraits.Demonstrative,
        ["quant"] = WordTraits.Quantifier, ["prep"] = WordTraits.Preposition, ["conj"] = WordTraits.Conjunction, ["sub"] = WordTraits.Subordinator,
        ["adv"] = WordTraits.Adverb, ["neg"] = WordTraits.Negative, ["wh"] = WordTraits.Question, ["clitic"] = WordTraits.Clitic,
    };

    private readonly FrozenDictionary<string, WordTraits> _words;
    private readonly FrozenDictionary<string, WordTraits>.AlternateLookup<ReadOnlySpan<char>> _bySpan;

    private FunctionWords(Dictionary<string, WordTraits> words)
    {
        _words = words.ToFrozenDictionary(StringComparer.Ordinal);
        _bySpan = _words.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    /// <summary>Number of words listed.</summary>
    public int Count => _words.Count;

    /// <summary>Every word listed.</summary>
    public IEnumerable<string> Words => _words.Keys;

    /// <summary>Loads the list embedded in this assembly.</summary>
    public static FunctionWords LoadEmbedded()
    {
        using Stream stream = typeof(FunctionWords).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The embedded function word list is missing.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>Reads the list: a word, then its traits, on each line.</summary>
    public static FunctionWords Parse(string text)
    {
        var words = new Dictionary<string, WordTraits>(StringComparer.Ordinal);
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            WordTraits traits = WordTraits.None;
            foreach (string name in parts.AsSpan(1))
            {
                traits |= TraitNames.TryGetValue(name, out WordTraits trait)
                    ? trait
                    : throw new InvalidDataException($"Unknown trait '{name}' for '{parts[0]}' in the function word list.");
            }

            words[parts[0]] = traits;
        }

        return new FunctionWords(words);
    }

    /// <summary>The traits of a word given in lowercase with a straight apostrophe, or none.</summary>
    public WordTraits TraitsOf(ReadOnlySpan<char> lowercaseWord) =>
        _bySpan.TryGetValue(lowercaseWord, out WordTraits traits) ? traits : WordTraits.None;
}
