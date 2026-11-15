namespace FictionChecker;

/// <summary>The spelling dialects. One is in effect for each check.</summary>
public enum Dialect
{
    /// <summary>American English.</summary>
    American = 0,

    /// <summary>British English with -ise spellings (organise).</summary>
    British = 1,

    /// <summary>British English with -ize spellings (organize, as Oxford prefers).</summary>
    BritishIze = 2,

    /// <summary>Canadian English.</summary>
    Canadian = 3,

    /// <summary>Australian English.</summary>
    Australian = 4,
}

/// <summary>Which quote marks a text uses for speech.</summary>
public enum QuoteStyle
{
    /// <summary>Double quotes for speech (usual in US publishing): “Hello,” she said.</summary>
    Double,

    /// <summary>Single quotes for speech (usual in UK publishing): ‘Hello,’ she said.</summary>
    Single,
}

/// <summary>How much to check inside dialogue.</summary>
public enum DialogueStrictness
{
    /// <summary>Dialogue is checked exactly like narration.</summary>
    Strict,

    /// <summary>Real typos are still caught, but informal speech (gonna, runnin', 'em) is accepted.</summary>
    Lenient,

    /// <summary>Nothing inside dialogue is checked.</summary>
    Off,
}

/// <summary>Where a custom word set applies.</summary>
public enum WordScope
{
    /// <summary>Valid in narration and dialogue.</summary>
    Everywhere,

    /// <summary>Valid only inside dialogue: a book's invented slang or a character's way of speaking.</summary>
    DialogueOnly,
}

/// <summary>The groups rules belong to. A whole category can be turned on or off.</summary>
public enum RuleCategory
{
    /// <summary>Spelling, including other-dialect spellings, capitals and unknown names.</summary>
    Spelling,

    /// <summary>Mechanical slips: repeated words, a/an, sentence capitals, doubled spaces.</summary>
    Mechanics,

    /// <summary>Commonly confused words: their/there, its/it's, then/than.</summary>
    ConfusableWords,

    /// <summary>Subject and verb agreement: "they was".</summary>
    Agreement,

    /// <summary>Punctuation around speech and dialogue tags.</summary>
    DialoguePunctuation,
}

/// <summary>What an issue is, as a hint for how to show it.</summary>
public enum IssueKind
{
    /// <summary>Not a known word.</summary>
    Misspelling,

    /// <summary>
    /// An unknown capitalized word that does not look like a slip for any known word: most likely a name that
    /// is not in a word set yet.
    /// </summary>
    UnknownName,

    /// <summary>A correct spelling in another dialect ("colour" in American English).</summary>
    OtherDialectSpelling,

    /// <summary>A word that needs a capital ("london").</summary>
    Capitalization,

    /// <summary>A grammar or punctuation problem.</summary>
    Grammar,
}
