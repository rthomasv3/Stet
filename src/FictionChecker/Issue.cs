namespace FictionChecker;

/// <summary>
/// A replacement that fixes an issue. It has its own position because a fix does not always replace exactly
/// the flagged text ("any where" is fixed by replacing both words with "anywhere").
/// </summary>
public sealed class Suggestion
{
    internal Suggestion(int start, int length, string text)
    {
        Start = start;
        Length = length;
        Text = text;
    }

    /// <summary>Where the text to replace starts, in UTF-16 units from the start of the paragraph.</summary>
    public int Start { get; }

    /// <summary>Length of the text to replace, in UTF-16 units.</summary>
    public int Length { get; }

    /// <summary>The replacement text.</summary>
    public string Text { get; }

    /// <inheritdoc />
    public override string ToString() => Text;
}

/// <summary>A problem found in a paragraph. Positions are UTF-16 units, as in .NET and JavaScript strings.</summary>
public sealed class Issue
{
    internal Issue(int start, int length, RuleInfo rule, IssueKind kind, string text, string message, string explanation, IReadOnlyList<Suggestion> suggestions)
    {
        Start = start;
        Length = length;
        Rule = rule;
        Kind = kind;
        Text = text;
        Message = message;
        Explanation = explanation;
        Suggestions = suggestions;
    }

    /// <summary>Where the flagged text starts, from the start of the paragraph.</summary>
    public int Start { get; }

    /// <summary>Length of the flagged text.</summary>
    public int Length { get; }

    /// <summary>The rule that found the issue.</summary>
    public RuleInfo Rule { get; }

    /// <summary>Stable identifier of the rule, for ignore settings.</summary>
    public string RuleId => Rule.Id;

    /// <summary>The rule's category.</summary>
    public RuleCategory Category => Rule.Category;

    /// <summary>What the issue is, as a hint for how to show it.</summary>
    public IssueKind Kind { get; }

    /// <summary>The flagged text as written.</summary>
    public string Text { get; }

    /// <summary>One sentence saying what is wrong.</summary>
    public string Message { get; }

    /// <summary>A short explanation an app can show under the message.</summary>
    public string Explanation { get; }

    /// <summary>Fixes, best first. May be empty.</summary>
    public IReadOnlyList<Suggestion> Suggestions { get; }

    /// <inheritdoc />
    public override string ToString() => $"{RuleId} at {Start}+{Length}: {Text}";
}

/// <summary>The result of checking one paragraph.</summary>
public sealed class ParagraphResult
{
    internal ParagraphResult(IReadOnlyList<Issue> issues, bool endsInDialogue, bool endsMidSentence = false)
    {
        Issues = issues;
        EndsInDialogue = endsInDialogue;
        EndsMidSentence = endsMidSentence;
    }

    /// <summary>The issues found, in text order.</summary>
    public IReadOnlyList<Issue> Issues { get; }

    /// <summary>
    /// The paragraph ended with a quote still open. Pass this as <see cref="ParagraphContext.StartsInDialogue"/>
    /// when checking the next paragraph.
    /// </summary>
    public bool EndsInDialogue { get; }

    /// <summary>
    /// The paragraph stopped without ending its sentence: a line of verse, a heading, or prose broken off by an
    /// inserted letter or picture. Pass this as <see cref="ParagraphContext.StartsMidSentence"/> when checking
    /// the next paragraph, which may then rightly begin without a capital.
    /// </summary>
    public bool EndsMidSentence { get; }
}

/// <summary>What the caller knows about a paragraph beyond its text.</summary>
public sealed record ParagraphContext
{
    /// <summary>The previous paragraph ended with its quote still open.</summary>
    public bool StartsInDialogue { get; init; }

    /// <summary>
    /// The previous paragraph stopped without ending its sentence, so this one may carry on from it and begin
    /// without a capital. Checking a whole document works this out by itself.
    /// </summary>
    public bool StartsMidSentence { get; init; }

    /// <summary>Ranges to leave unchecked: foreign words in italics, a letter written in deliberately bad spelling.</summary>
    public IReadOnlyList<TextSpan>? SkipRanges { get; init; }

    /// <summary>Ranges to treat as dialogue whatever the quote marks say, such as italic thoughts.</summary>
    public IReadOnlyList<TextSpan>? DialogueRanges { get; init; }

    /// <summary>Options for this check. Null means the checker's own.</summary>
    public CheckerOptions? Options { get; init; }
}

/// <summary>One paragraph of a document, with what the caller knows about it.</summary>
/// <param name="Text">The paragraph's text.</param>
public sealed record DocumentParagraph(string Text)
{
    /// <summary>Leave the whole paragraph unchecked: a chapter title or an epigraph.</summary>
    public bool Skip { get; init; }

    /// <summary>Ranges to leave unchecked.</summary>
    public IReadOnlyList<TextSpan>? SkipRanges { get; init; }

    /// <summary>Ranges to treat as dialogue whatever the quote marks say.</summary>
    public IReadOnlyList<TextSpan>? DialogueRanges { get; init; }
}
