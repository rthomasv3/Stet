using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace Stet.Bench;

/// <summary>
/// Checking text the checker has not seen before. A checker remembers what it worked out for the unknown
/// words it has met, so checking the same text a second time is faster than the first. Here every
/// measurement starts with a new checker, which is what opening a book in an app is like. The code itself is
/// warm: the cost of the very first use in a process is in <see cref="ColdStartBenchmarks"/>.
///
/// <see cref="CheckerBenchmarks"/> measures the same documents on a checker that has seen them.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, RuntimeMoniker.Net10_0, warmupCount: 5, iterationCount: 30, invocationCount: 1)]
[SimpleJob(RunStrategy.Monitoring, RuntimeMoniker.NativeAot10_0, warmupCount: 5, iterationCount: 30, invocationCount: 1)]
public class FirstCheckBenchmarks
{
    private BenchText _text = null!;
    private Checker _withNames = null!;
    private Checker _bare = null!;

    [GlobalSetup]
    public void Setup() => _text = BenchText.Load();

    [IterationSetup]
    public void NewCheckers()
    {
        _bare = new Checker();
        _withNames = new Checker();
        _withNames.SetWords("world", _text.Names);
    }

    [Benchmark]
    public IReadOnlyList<ParagraphResult> Scene1500Words() => _withNames.CheckDocument(_text.Scene);

    [Benchmark]
    public IReadOnlyList<ParagraphResult> Chapter5000Words() => _withNames.CheckDocument(_text.Chapter);

    [Benchmark]
    public IReadOnlyList<ParagraphResult> Novel100kWords() => _withNames.CheckDocument(_text.Novel);

    [Benchmark]
    public IReadOnlyList<ParagraphResult> Novel100kWordsNoWordList() => _bare.CheckDocument(_text.Novel);
}
