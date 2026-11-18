using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace Stet.Bench;

/// <summary>
/// What the first use of Stet in a process costs. Every measurement is taken in a process of its own, once,
/// with no warm-up, so it includes everything a real first use does: loading the built-in data and, on the
/// normal runtime, compiling the code as it first runs.
///
/// This class holds no checker of its own: one made before the measurement would do that work early.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, RuntimeMoniker.Net10_0, launchCount: 20, warmupCount: 0, iterationCount: 1, invocationCount: 1)]
[SimpleJob(RunStrategy.ColdStart, RuntimeMoniker.NativeAot10_0, launchCount: 20, warmupCount: 0, iterationCount: 1, invocationCount: 1)]
public class ColdStartBenchmarks
{
    [Benchmark]
    public Checker FirstChecker() => new();

    // Some data loads when a check first needs it, so the first check is part of starting up too.
    [Benchmark]
    public ParagraphResult FirstCheckerAndFirstCheck() =>
        new Checker().CheckParagraph("“They went over there to see their friends,” she said, and beleived it.");
}
