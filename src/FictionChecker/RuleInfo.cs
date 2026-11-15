namespace FictionChecker;

/// <summary>Describes one rule, for a settings screen or help text.</summary>
public sealed class RuleInfo
{
    internal RuleInfo(string id, string name, string description, RuleCategory category, bool enabledByDefault, bool isFormal, string[] flagged, string[] accepted)
    {
        Id = id;
        Name = name;
        Description = description;
        Category = category;
        EnabledByDefault = enabledByDefault;
        IsFormal = isFormal;
        FlaggedExamples = flagged;
        AcceptedExamples = accepted;
    }

    /// <summary>Stable identifier, safe to store in settings. Its meaning never changes between versions.</summary>
    public string Id { get; }

    /// <summary>Short name.</summary>
    public string Name { get; }

    /// <summary>One or two sentences on what the rule finds.</summary>
    public string Description { get; }

    /// <summary>The category the rule belongs to.</summary>
    public RuleCategory Category { get; }

    /// <summary>Whether the rule is on unless the options say otherwise.</summary>
    public bool EnabledByDefault { get; }

    /// <summary>A formal rule is skipped inside dialogue when dialogue strictness is lenient.</summary>
    public bool IsFormal { get; }

    /// <summary>Examples the rule flags.</summary>
    public IReadOnlyList<string> FlaggedExamples { get; }

    /// <summary>Examples the rule leaves alone.</summary>
    public IReadOnlyList<string> AcceptedExamples { get; }
}

/// <summary>Identifiers of the built-in rules.</summary>
public static class RuleIds
{
    /// <summary>A word that is not in the dictionary or the word sets.</summary>
    public const string Misspelling = "spelling.misspelling";

    /// <summary>An unknown capitalized word that is probably a name.</summary>
    public const string UnknownName = "spelling.unknown-name";

    /// <summary>A spelling that belongs to another dialect.</summary>
    public const string OtherDialect = "spelling.other-dialect";

    /// <summary>A word that needs a capital letter.</summary>
    public const string Capitalization = "spelling.capitalization";

    /// <summary>The same word twice in a row.</summary>
    public const string RepeatedWord = "mechanics.repeated-word";
}

internal static class RuleCatalog
{
    public static readonly RuleInfo Misspelling = new(
        RuleIds.Misspelling,
        "Misspelled words",
        "Flags words that are not in the dictionary or in your word lists.",
        RuleCategory.Spelling,
        true,
        false,
        ["She could not beleive it.", "He opened teh door."],
        ["She could not believe it.", "\"I'm gonna find 'em,\" he said."]);

    public static readonly RuleInfo UnknownName = new(
        RuleIds.UnknownName,
        "Unknown names",
        "Flags capitalized words that are not known and do not look like a typing slip. These are usually names that are not in your word lists yet.",
        RuleCategory.Spelling,
        true,
        false,
        ["They rode north to Vaelmoor."],
        ["They rode north to London."]);

    public static readonly RuleInfo OtherDialect = new(
        RuleIds.OtherDialect,
        "Other-dialect spellings",
        "Flags spellings that are correct in another dialect of English, such as \"colour\" in American English. When this is off, those spellings are accepted.",
        RuleCategory.Spelling,
        true,
        false,
        ["The colour had faded."],
        ["The color had faded."]);

    public static readonly RuleInfo Capitalization = new(
        RuleIds.Capitalization,
        "Missing capitals",
        "Flags names written without their capital letter.",
        RuleCategory.Spelling,
        true,
        false,
        ["She sailed for london."],
        ["She sailed for London."]);

    /// <summary>The spelling rules. Grammar rules describe themselves (see GrammarRule).</summary>
    public static readonly IReadOnlyList<RuleInfo> Spelling = [Misspelling, UnknownName, OtherDialect, Capitalization];
}
