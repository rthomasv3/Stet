namespace Stet.Grammar;

/// <summary>
/// The type of a word in its sentence, using the coarse set the Google Books data is tagged with. The data's
/// habits are kept as they are, because the tagger's statistics follow them: "to" before a verb is a
/// particle, "her" and "my" are pronouns, and "that" introducing a clause is an adposition.
/// </summary>
internal enum WordType : byte
{
    Noun = 0,
    Verb = 1,
    Adjective = 2,
    Adverb = 3,
    Pronoun = 4,
    Determiner = 5,
    Adposition = 6,
    Number = 7,
    Conjunction = 8,
    Particle = 9,
    Punctuation = 10,
    Other = 11,
}

/// <summary>How a word is formed from its base word. A word can be several at once ("walked").</summary>
[Flags]
internal enum WordForms : byte
{
    None = 0,
    NounPlural = 1,
    VerbThirdPerson = 2,
    VerbPast = 4,
    VerbPastParticiple = 8,
    VerbGerund = 16,
    Comparative = 32,
    Superlative = 64,

    /// <summary>The base form of a verb ("walk"). Worked out from the links, not stored.</summary>
    VerbBase = 128,
}

/// <summary>Which sound a word starts with, where its spelling would mislead ("an hour", "a unicorn").</summary>
internal enum StartSound : byte
{
    /// <summary>As the first letter suggests.</summary>
    AsSpelled,
    Consonant,
    Vowel,

    /// <summary>Said both ways ("herb").</summary>
    Either,

    /// <summary>Not known, and the spelling is no guide ("usurer", "heirloom" look alike but differ).</summary>
    Unknown,
}

/// <summary>Whether a noun is counted ("a chair") or measured ("much water").</summary>
internal enum Countability : byte
{
    Counted,
    Measured,
    Both,
}
