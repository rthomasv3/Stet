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

    /// <summary>"a" where "an" is needed, or the reverse.</summary>
    public const string Article = "mechanics.a-an";

    /// <summary>"could of" for "could have".</summary>
    public const string CouldOf = "mechanics.could-of";

    /// <summary>Two or more spaces between words.</summary>
    public const string DoubledSpace = "mechanics.doubled-space";

    /// <summary>A sentence that starts without a capital letter.</summary>
    public const string SentenceCapital = "mechanics.sentence-capital";

    /// <summary>The word "I" written in lowercase.</summary>
    public const string LowercaseI = "mechanics.lowercase-i";

    /// <summary>A space before a comma, or two commas in a row.</summary>
    public const string CommaSpacing = "mechanics.comma-spacing";

    /// <summary>"more stronger", "most quickest".</summary>
    public const string DoubleComparative = "mechanics.double-comparative";

    /// <summary>A space before a full stop.</summary>
    public const string FullStopSpacing = "mechanics.full-stop-spacing";

    /// <summary>"wont" or "cant" written for "won't" or "can't".</summary>
    public const string MissingApostrophe = "mechanics.missing-apostrophe";

    /// <summary>The wrong form of a verb after "have" or "did" ("have went").</summary>
    public const string VerbForm = "agreement.verb-form";

    /// <summary>A plural subject with a singular verb ("the children was").</summary>
    public const string NounVerb = "agreement.noun-verb";

    /// <summary>A pronoun with a verb form that does not agree with it ("they was").</summary>
    public const string PronounVerb = "agreement.pronoun-verb";

    /// <summary>A mismatch that is ordinary in dialect speech ("they was", "he don't").</summary>
    public const string DialectVerbForm = "agreement.dialect-verb-form";

    /// <summary>A full stop where speech runs on into its tag.</summary>
    public const string FullStopBeforeTag = "dialogue.full-stop-before-tag";

    /// <summary>A dialogue tag that starts with a capital after a comma.</summary>
    public const string CapitalizedTag = "dialogue.capitalized-tag";

    /// <summary>No comma between speech and its tag.</summary>
    public const string MissingCommaBeforeTag = "dialogue.missing-comma";

    /// <summary>A comma placed after the closing quote mark, before a tag.</summary>
    public const string CommaOutsideQuote = "dialogue.comma-outside-quote";

    /// <summary>Mixing up their, there and they're.</summary>
    public const string ConfusableTheirThere = "confusable.their-there";

    /// <summary>Mixing up its and it's.</summary>
    public const string ConfusableItsIts = "confusable.its-its";

    /// <summary>Mixing up your and you're.</summary>
    public const string ConfusableYourYoure = "confusable.your-youre";

    /// <summary>Mixing up whose and who's.</summary>
    public const string ConfusableWhoseWhos = "confusable.whose-whos";

    /// <summary>Mixing up then and than.</summary>
    public const string ConfusableThenThan = "confusable.then-than";

    /// <summary>Mixing up lose and loose.</summary>
    public const string ConfusableLoseLoose = "confusable.lose-loose";

    /// <summary>Mixing up affect and effect.</summary>
    public const string ConfusableAffectEffect = "confusable.affect-effect";

    /// <summary>Mixing up accept and except.</summary>
    public const string ConfusableAcceptExcept = "confusable.accept-except";

    /// <summary>Mixing up to and too.</summary>
    public const string ConfusableToToo = "confusable.to-too";

    /// <summary>Mixing up of and off.</summary>
    public const string ConfusableOfOff = "confusable.of-off";

    /// <summary>Mixing up lead and led.</summary>
    public const string ConfusableLeadLed = "confusable.lead-led";

    /// <summary>Mixing up past and passed.</summary>
    public const string ConfusablePastPassed = "confusable.past-passed";

    /// <summary>Mixing up breath and breathe.</summary>
    public const string ConfusableBreathBreathe = "confusable.breath-breathe";

    /// <summary>Mixing up quite and quiet.</summary>
    public const string ConfusableQuiteQuiet = "confusable.quite-quiet";

    /// <summary>Mixing up peek, peak and pique.</summary>
    public const string ConfusablePeekPeak = "confusable.peek-peak";

    /// <summary>Mixing up wander and wonder.</summary>
    public const string ConfusableWanderWonder = "confusable.wander-wonder";

    /// <summary>Mixing up desert and dessert.</summary>
    public const string ConfusableDesertDessert = "confusable.desert-dessert";

    /// <summary>Mixing up brake and break.</summary>
    public const string ConfusableBrakeBreak = "confusable.brake-break";

    /// <summary>Mixing up principal and principle.</summary>
    public const string ConfusablePrincipalPrinciple = "confusable.principal-principle";

    /// <summary>Mixing up compliment and complement.</summary>
    public const string ConfusableComplimentComplement = "confusable.compliment-complement";

    /// <summary>Mixing up whether and weather.</summary>
    public const string ConfusableWhetherWeather = "confusable.whether-weather";

    /// <summary>Mixing up here and hear.</summary>
    public const string ConfusableHereHear = "confusable.here-hear";

    /// <summary>Mixing up bear and bare.</summary>
    public const string ConfusableBearBare = "confusable.bear-bare";

    /// <summary>Mixing up advice and advise.</summary>
    public const string ConfusableAdviceAdvise = "confusable.advice-advise";

    /// <summary>Mixing up week and weak.</summary>
    public const string ConfusableWeekWeak = "confusable.week-weak";

    /// <summary>Mixing up know and no.</summary>
    public const string ConfusableKnowNo = "confusable.know-no";

    /// <summary>Mixing up piece and peace.</summary>
    public const string ConfusablePiecePeace = "confusable.piece-peace";
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
