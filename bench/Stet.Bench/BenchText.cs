namespace Stet.Bench;

/// <summary>
/// The text the benchmarks check. With the environment variable <c>STET_BENCH_TEXT</c> set to a plain text
/// file (one paragraph per line), it is that file, so the timings are for real prose. Without it, it is the
/// made-up text of <see cref="SampleText"/>, which repeats itself far more than a book does and so checks
/// faster.
/// </summary>
public sealed class BenchText
{
    /// <summary>The environment variable that names the text file. Program.cs sets it from <c>--text</c>.</summary>
    public const string Variable = "STET_BENCH_TEXT";

    private BenchText(List<string> novel, IReadOnlyList<string> names)
    {
        Novel = novel;
        Chapter = Take(novel, novel.Count / 3, 5_000);
        Scene = Take(novel, novel.Count / 2, 1_500);
        Paragraph = Scene.OrderBy(p => p.Length).ElementAt(Scene.Count / 2);
        Names = names;
    }

    /// <summary>The first 100,000 words of the text, or all of it when it is shorter.</summary>
    public List<string> Novel { get; }

    /// <summary>About 5,000 words of consecutive paragraphs.</summary>
    public List<string> Chapter { get; }

    /// <summary>About 1,500 words of consecutive paragraphs.</summary>
    public List<string> Scene { get; }

    /// <summary>One paragraph of typical length: the middle one of the scene by length.</summary>
    public string Paragraph { get; }

    /// <summary>The words a writer's word list would hold for this text.</summary>
    public IReadOnlyList<string> Names { get; }

    /// <summary>Loads the text file named by the environment variable, or builds the made-up text.</summary>
    public static BenchText Load()
    {
        string? path = Environment.GetEnvironmentVariable(Variable);
        if (string.IsNullOrEmpty(path))
        {
            return new BenchText(SampleText.Paragraphs(100_000), SampleText.Names);
        }

        var novel = Take(File.ReadLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).ToList(), 0, 100_000);

        // A stand-in for the writer's word list, as the evaluation harness makes one: a word the checker does
        // not know that the text uses twice or more is taken to be meant.
        var checker = new Checker();
        var names = checker.CheckDocument(novel)
            .SelectMany(result => result.Issues)
            .Where(issue => issue.Category == RuleCategory.Spelling)
            .GroupBy(issue => issue.Text, StringComparer.Ordinal)
            .Where(group => group.Count() >= 2)
            .Select(group => group.Key)
            .ToList();
        return new BenchText(novel, names);
    }

    // Consecutive paragraphs from one place in the text, up to about a number of words.
    private static List<string> Take(List<string> paragraphs, int from, int words)
    {
        var taken = new List<string>();
        int count = 0;
        for (int i = from; i < paragraphs.Count && count < words; i++)
        {
            taken.Add(paragraphs[i]);
            count += paragraphs[i].Count(c => c == ' ') + 1;
        }

        return taken;
    }
}
