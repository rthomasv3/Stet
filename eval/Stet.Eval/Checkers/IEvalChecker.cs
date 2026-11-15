namespace Stet.Eval.Checkers;

/// <summary>One replacement in a paragraph. Positions are UTF-16 units, as in the library's results.</summary>
public sealed record Edit(int Start, int Length, string Text);

/// <summary>A suggested fix: one or more edits applied together.</summary>
public sealed record EvalSuggestion(IReadOnlyList<Edit> Edits)
{
    /// <summary>Returns <paramref name="text"/> with the edits applied.</summary>
    public string ApplyTo(string text)
    {
        string result = text;
        foreach (Edit edit in Edits.OrderByDescending(e => e.Start))
        {
            if (edit.Start < 0 || edit.Length < 0 || edit.Start + edit.Length > result.Length)
            {
                return text;
            }

            result = string.Concat(result.AsSpan(0, edit.Start), edit.Text, result.AsSpan(edit.Start + edit.Length));
        }

        return result;
    }
}

/// <summary>Something a checker flagged in a paragraph.</summary>
public sealed record EvalIssue(int Start, int Length, string RuleId, string Category, IReadOnlyList<EvalSuggestion> Suggestions)
{
    /// <summary>Whether this issue's span touches the given span.</summary>
    public bool Overlaps(int start, int length) => Start < start + length && start < Start + Math.Max(Length, 1);
}

/// <summary>Where a run of paragraphs comes from.</summary>
/// <param name="Corpus">Corpus name.</param>
/// <param name="BookId">Book identifier within the corpus.</param>
/// <param name="Dialect">The spelling the book uses: "US" or "GB".</param>
public sealed record DocumentInfo(string Corpus, string BookId, string Dialect);

/// <summary>What the harness needs from a checker. The library's checker is wrapped in one of these.</summary>
public interface IEvalChecker
{
    /// <summary>Name used for report files.</summary>
    string Name { get; }

    /// <summary>
    /// Checks consecutive paragraphs of one text and returns the issues for each. Paragraphs are given together
    /// so a checker can carry state (such as an open quote) from one to the next. An entry is null when the
    /// paragraph was not checked (only checkers that run outside this process do that).
    /// </summary>
    /// <param name="paragraphs">The paragraphs, in order.</param>
    /// <param name="document">Which book the paragraphs come from and the spelling it uses.</param>
    IReadOnlyList<IReadOnlyList<EvalIssue>?> CheckDocument(IReadOnlyList<string> paragraphs, DocumentInfo document);
}
