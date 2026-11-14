using System.Text.Json;

namespace FictionChecker.Eval.Checkers;

/// <summary>
/// Replays issues produced by a checker that ran outside this process (the Harper baseline).
/// The issues file has one JSON object per line: <c>{"id": "&lt;paragraph hash&gt;", "issues": [...]}</c>.
/// Paragraphs missing from the file count as not checked.
/// </summary>
public sealed class ExternalChecker : IEvalChecker
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, IReadOnlyList<Issue>> _issues = new(StringComparer.Ordinal);

    /// <summary>Loads an issues file.</summary>
    public ExternalChecker(string name, string issuesPath)
    {
        Name = name;
        foreach (string line in File.ReadLines(issuesPath))
        {
            if (line.Length == 0)
            {
                continue;
            }

            Line? parsed = JsonSerializer.Deserialize<Line>(line, JsonOptions);
            if (parsed?.Id is null)
            {
                continue;
            }

            _issues[parsed.Id] = (parsed.Issues ?? [])
                .Select(i => new Issue(
                    i.Start,
                    i.Length,
                    i.RuleId ?? "unknown",
                    i.Category ?? "",
                    (i.Suggestions ?? []).Select(edits => new Suggestion(edits)).ToList()))
                .ToList();
        }
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<IReadOnlyList<Issue>?> CheckDocument(IReadOnlyList<string> paragraphs, DocumentInfo document) =>
        paragraphs.Select(p => _issues.GetValueOrDefault(WorkSet.HashOf(p))).ToList();

    private sealed record Line(string? Id, List<LineIssue>? Issues);

    private sealed record LineIssue(int Start, int Length, string? RuleId, string? Category, List<List<Edit>>? Suggestions);
}
