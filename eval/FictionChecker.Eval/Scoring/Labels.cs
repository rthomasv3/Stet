using System.Text.Json;

namespace FictionChecker.Eval.Scoring;

/// <summary>
/// A reviewer's verdict on one flag. The paragraph is identified by a hash of its text, so labels can be
/// committed without copying any book text.
/// </summary>
public sealed record Label(string Hash, int Start, int Length, string RuleId, string Verdict);

/// <summary>Review labels for flags on clean text.</summary>
public sealed class Labels
{
    /// <summary>The verdict for a flag that is a genuine error in the published text.</summary>
    public const string RealError = "real-error";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HashSet<(string Hash, int Start, int Length, string RuleId)> _realErrors = [];

    /// <summary>Creates a label set.</summary>
    public Labels(IEnumerable<Label> labels)
    {
        foreach (Label label in labels)
        {
            if (label.Verdict == RealError)
            {
                _realErrors.Add((label.Hash, label.Start, label.Length, label.RuleId));
            }
        }
    }

    /// <summary>No labels.</summary>
    public static Labels Empty { get; } = new([]);

    /// <summary>Loads labels from a JSON file, or returns none when it does not exist.</summary>
    public static Labels Load(string path) =>
        File.Exists(path) ? new Labels(JsonSerializer.Deserialize<List<Label>>(File.ReadAllText(path), JsonOptions) ?? []) : Empty;

    /// <summary>Whether a flag was confirmed as a genuine error in the source text.</summary>
    public bool IsRealError(string paragraphHash, int start, int length, string ruleId) =>
        _realErrors.Contains((paragraphHash, start, length, ruleId));
}
