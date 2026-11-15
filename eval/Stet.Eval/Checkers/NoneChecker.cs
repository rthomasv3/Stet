namespace Stet.Eval.Checkers;

/// <summary>Flags nothing. The placeholder until the library has a checker.</summary>
public sealed class NoneChecker : IEvalChecker
{
    /// <inheritdoc />
    public string Name => "none";

    /// <inheritdoc />
    public IReadOnlyList<IReadOnlyList<EvalIssue>?> CheckDocument(IReadOnlyList<string> paragraphs, DocumentInfo document)
    {
        var results = new IReadOnlyList<EvalIssue>[paragraphs.Count];
        Array.Fill(results, []);
        return results;
    }
}
