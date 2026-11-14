namespace FictionChecker.Eval.Checkers;

/// <summary>Flags nothing. The placeholder until the library has a checker.</summary>
public sealed class NoneChecker : IEvalChecker
{
    /// <inheritdoc />
    public string Name => "none";

    /// <inheritdoc />
    public IReadOnlyList<IReadOnlyList<Issue>?> CheckDocument(IReadOnlyList<string> paragraphs, string dialect)
    {
        var results = new IReadOnlyList<Issue>[paragraphs.Count];
        Array.Fill(results, []);
        return results;
    }
}
