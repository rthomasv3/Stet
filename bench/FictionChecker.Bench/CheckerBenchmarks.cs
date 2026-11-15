using BenchmarkDotNet.Attributes;

namespace FictionChecker.Bench;

/// <summary>
/// The operations that have a target in the requirements (section 8). Targets: create under 500 ms, a
/// paragraph under 5 ms, a 1,500-word scene under 50 ms, a 100,000-word novel under 3 s, a 20,000-word set
/// swap under 50 ms, suggestions under 5 ms.
///
/// The text is made up and repeats itself far more than a book does, so the document timings here are much
/// faster than on real fiction. They are for spotting slowdowns between changes. Timings on real books are
/// in eval/reports/perf.md.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class CheckerBenchmarks
{
    private readonly Checker _bare = new();
    private readonly Checker _withNames = new();
    private readonly List<string> _novel = SampleText.Paragraphs(100_000);
    private readonly List<string> _scene = SampleText.Paragraphs(1_500, seed: 11);
    private readonly string _paragraph = SampleText.Paragraphs(150, 150, seed: 13)[0];
    private readonly string[] _twentyThousand = [.. Enumerable.Range(0, 20_000).Select(i => "Nm" + Convert.ToString(i, 16).Replace('0', 'q'))];
    private CheckCache _cache = null!;
    private List<string> _edited = null!;
    private readonly string[] _typos = SampleText.Typos(6_000);
    private int _edit;
    private int _typo;

    [GlobalSetup]
    public void Setup()
    {
        _withNames.SetWords("world", SampleText.Names);
        _cache = new CheckCache(_withNames);
        _cache.CheckDocument(_novel);
        _edited = [.. _novel];
    }

    [Benchmark]
    public Checker CreateChecker() => new();

    [Benchmark]
    public ParagraphResult Paragraph150Words() => _withNames.CheckParagraph(_paragraph);

    [Benchmark]
    public IReadOnlyList<ParagraphResult> Scene1500Words() => _withNames.CheckDocument(_scene);

    [Benchmark]
    public IReadOnlyList<ParagraphResult> Novel100kWords() => _withNames.CheckDocument(_novel);

    [Benchmark]
    public IReadOnlyList<ParagraphResult> Novel100kWordsNoWordList() => _bare.CheckDocument(_novel);

    [Benchmark]
    public IReadOnlyList<ParagraphResult> NovelRecheckAfterOneEditCached()
    {
        int at = (_edit++ * 7919) % _edited.Count;
        _edited[at] += " More.";
        return _cache.CheckDocument(_edited);
    }

    [Benchmark]
    public void ReplaceWordSet20k() => _bare.SetWords("big", _twentyThousand);

    // A different misspelling each time, and more of them than the checker remembers, so none is answered
    // from memory.
    [Benchmark]
    public IReadOnlyList<string> SuggestOneWord() => _bare.Suggest(_typos[_typo++ % _typos.Length]);
}
