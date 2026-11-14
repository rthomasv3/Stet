namespace FictionChecker.Eval.Checkers;

/// <summary>
/// Wraps a checker and drops issues in the given categories. Used to compare with another tool on only the
/// kinds of problem this library sets out to catch.
/// </summary>
public sealed class FilteredChecker : IEvalChecker
{
    private readonly IEvalChecker _inner;
    private readonly HashSet<string> _excluded;

    /// <summary>Creates the wrapper.</summary>
    public FilteredChecker(IEvalChecker inner, string name, IEnumerable<string> excludedCategories)
    {
        _inner = inner;
        Name = name;
        _excluded = new HashSet<string>(excludedCategories, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<IReadOnlyList<Issue>?> CheckDocument(IReadOnlyList<string> paragraphs, string dialect) =>
        _inner.CheckDocument(paragraphs, dialect)
            .Select(issues => (IReadOnlyList<Issue>?)issues?.Where(i => !_excluded.Contains(i.Category)).ToList())
            .ToList();
}
