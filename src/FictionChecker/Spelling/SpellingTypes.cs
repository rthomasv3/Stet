using FictionChecker.Segmentation;

namespace FictionChecker.Spelling;

/// <summary>How much to check inside dialogue.</summary>
internal enum DialogueStrictness
{
    /// <summary>Dialogue is checked exactly like narration.</summary>
    Strict,

    /// <summary>Real typos are still caught, but informal speech (gonna, runnin', 'em) is accepted.</summary>
    Lenient,

    /// <summary>Nothing inside dialogue is checked.</summary>
    Off,
}

/// <summary>The kind of spelling problem.</summary>
internal enum SpellingKind
{
    /// <summary>Not a known word.</summary>
    Misspelling,

    /// <summary>A correct spelling in another dialect ("colour" in American English).</summary>
    OtherDialect,

    /// <summary>A word that needs a capital ("london").</summary>
    Capitalization,
}

/// <summary>Settings for a spelling check.</summary>
internal sealed record SpellingOptions
{
    /// <summary>The dialect whose spellings are correct.</summary>
    public Dialect Dialect { get; init; } = Dialect.US;

    /// <summary>Which quote marks delimit speech. Null means the dialect's usual style.</summary>
    public QuoteStyle? QuoteStyle { get; init; }

    /// <summary>How much to check inside dialogue.</summary>
    public DialogueStrictness Dialogue { get; init; } = DialogueStrictness.Lenient;

    /// <summary>Whether another dialect's spelling is reported (as its own kind). When false it is accepted.</summary>
    public bool FlagOtherDialects { get; init; } = true;

    /// <summary>Most suggestions offered for one word.</summary>
    public int MaxSuggestions { get; init; } = 5;

    /// <summary>The quote style in effect.</summary>
    public QuoteStyle EffectiveQuoteStyle =>
        QuoteStyle ?? (Dialect is Dialect.GBIse or Dialect.GBIze ? Segmentation.QuoteStyle.Single : Segmentation.QuoteStyle.Double);
}

/// <summary>A replacement that fixes an issue. It can cover more than the flagged word ("any where" → "anywhere").</summary>
internal sealed record SpellingSuggestion(int Start, int Length, string Text);

/// <summary>A spelling problem found in a paragraph. Positions are UTF-16 units.</summary>
internal sealed record SpellingIssue(int Start, int Length, SpellingKind Kind, string Word, IReadOnlyList<SpellingSuggestion> Suggestions)
{
    /// <summary>Stable identifier of the rule, for ignore settings.</summary>
    public string RuleId => Kind switch
    {
        SpellingKind.OtherDialect => "spelling.other-dialect",
        SpellingKind.Capitalization => "spelling.capitalization",
        _ => "spelling.misspelling",
    };
}

/// <summary>The result of checking one paragraph.</summary>
internal sealed record SpellingResult(IReadOnlyList<SpellingIssue> Issues, bool EndsInDialogue);
