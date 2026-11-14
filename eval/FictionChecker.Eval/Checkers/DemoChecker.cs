using FictionChecker.Eval.Text;

namespace FictionChecker.Eval.Checkers;

/// <summary>
/// A deliberately naive checker (any word repeated twice in a row) that exists only to exercise the harness:
/// it produces false alarms, catches, and correct and incorrect fixes.
/// </summary>
public sealed class DemoChecker : IEvalChecker
{
    /// <inheritdoc />
    public string Name => "demo";

    /// <inheritdoc />
    public IReadOnlyList<IReadOnlyList<Issue>?> CheckDocument(IReadOnlyList<string> paragraphs)
    {
        var results = new IReadOnlyList<Issue>[paragraphs.Count];
        for (int p = 0; p < paragraphs.Count; p++)
        {
            string text = paragraphs[p];
            List<Token> tokens = Words.Tokenize(text);
            var issues = new List<Issue>();
            for (int i = 1; i < tokens.Count; i++)
            {
                Token previous = tokens[i - 1];
                Token current = tokens[i];
                bool adjacent = current.Start == previous.End + 1 && text[previous.End] == ' ';
                if (adjacent && string.Equals(previous.Text, current.Text, StringComparison.OrdinalIgnoreCase))
                {
                    var removeSecond = new Suggestion([new Edit(previous.End, current.Length + 1, "")]);
                    issues.Add(new Issue(previous.Start, current.End - previous.Start, "demo.repeated-word", "Mechanics", [removeSecond]));
                }
            }

            results[p] = issues;
        }

        return results;
    }
}
