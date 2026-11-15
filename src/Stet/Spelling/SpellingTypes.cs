using Stet.Segmentation;

namespace Stet.Spelling;

/// <summary>The kind of spelling problem.</summary>
internal enum SpellingKind
{
    /// <summary>Not a known word.</summary>
    Misspelling,

    /// <summary>
    /// An unknown capitalized word that does not look like a slip for any known word: most likely a name
    /// that is not in a word set yet. Reported apart from misspellings so an app can show it differently.
    /// </summary>
    UnknownName,

    /// <summary>A correct spelling in another dialect ("colour" in American English).</summary>
    OtherDialect,

    /// <summary>A word that needs a capital ("london").</summary>
    Capitalization,
}

/// <summary>Settings for a spelling check.</summary>
internal sealed record SpellingOptions
{
    /// <summary>The dialect whose spellings are correct.</summary>
    public Dialect Dialect { get; init; } = Dialect.American;

    /// <summary>Which quote marks delimit speech. Null means the dialect's usual style.</summary>
    public QuoteStyle? QuoteStyle { get; init; }

    /// <summary>How much to check inside dialogue.</summary>
    public DialogueStrictness Dialogue { get; init; } = DialogueStrictness.Lenient;

    /// <summary>Whether another dialect's spelling is reported (as its own kind). When false it is accepted.</summary>
    public bool FlagOtherDialects { get; init; } = true;

    /// <summary>Most suggestions offered for one word. Zero skips working out suggestions, which is faster.</summary>
    public int MaxSuggestions { get; init; } = 5;

    /// <summary>The quote style in effect.</summary>
    public QuoteStyle EffectiveQuoteStyle =>
        QuoteStyle ?? (Dialect is Dialect.British or Dialect.BritishIze ? global::Stet.QuoteStyle.Single : global::Stet.QuoteStyle.Double);
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
        SpellingKind.UnknownName => "spelling.unknown-name",
        _ => "spelling.misspelling",
    };
}

/// <summary>The result of checking one paragraph.</summary>
internal sealed record SpellingResult(IReadOnlyList<SpellingIssue> Issues, bool EndsInDialogue);
