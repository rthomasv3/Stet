using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Stet.Bench;

/// <summary>
/// The operations an app does most: creating a checker, checking a paragraph, a scene, a chapter and a whole
/// novel, rechecking after an edit, replacing a word list, and asking for suggestions. Each is measured on
/// the normal .NET runtime and compiled with Native AOT.
///
/// The text is a file of your choosing (see <see cref="BenchText"/>) or, without one, made-up text. The
/// made-up text repeats itself far more than a book does, so its document timings are much faster than on
/// real fiction. Timings on real books are in eval/RESULTS.md.
///
/// Everything here is measured warm. The cost of the first use in a process is in
/// <see cref="ColdStartBenchmarks"/>.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class CheckerBenchmarks
{
    private readonly Checker _bare = new();
    private readonly Checker _withNames = new();
    private readonly string[] _twentyThousand = [.. Enumerable.Range(0, 20_000).Select(i => "Nm" + Convert.ToString(i, 16).Replace('0', 'q'))];
    private readonly string[] _typos = SampleText.Typos(6_000);
    private readonly string[] _nameSentences = [.. SampleText.Typos(6_000, seed: 9).Select(t => $"Then {char.ToUpperInvariant(t[0])}{t[1..]} spoke.")];
    private List<string> _novel = null!;
    private List<string> _chapter = null!;
    private List<string> _scene = null!;
    private string _paragraph = null!;
    private CheckCache _cache = null!;
    private List<string> _edited = null!;
    private int _edit;
    private int _name;
    private int _typo;

    [GlobalSetup]
    public void Setup()
    {
        BenchText text = BenchText.Load();
        _novel = text.Novel;
        _chapter = text.Chapter;
        _scene = text.Scene;
        _paragraph = text.Paragraph;
        _withNames.SetWords("world", text.Names);
        _cache = new CheckCache(_withNames);
        _cache.CheckDocument(_novel);
        _edited = [.. _novel];
    }

    [Benchmark]
    public Checker CreateChecker() => new();

    [Benchmark]
    public ParagraphResult Paragraph() => _withNames.CheckParagraph(_paragraph);

    [Benchmark]
    public IReadOnlyList<ParagraphResult> Scene1500Words() => _withNames.CheckDocument(_scene);

    [Benchmark]
    public IReadOnlyList<ParagraphResult> Chapter5000Words() => _withNames.CheckDocument(_chapter);

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

    // An unknown capitalized word, a different one each time: the checker decides whether it is a name or a
    // slip and then works out suggestions.
    [Benchmark]
    public ParagraphResult UnknownNameInSentence() => _bare.CheckParagraph(_nameSentences[_name++ % _nameSentences.Length]);

    // A different misspelling each time, and more of them than the checker remembers, so none is answered
    // from memory.
    [Benchmark]
    public IReadOnlyList<string> SuggestOneWord() => _bare.Suggest(_typos[_typo++ % _typos.Length]);
}
