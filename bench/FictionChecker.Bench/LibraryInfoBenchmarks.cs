using BenchmarkDotNet.Attributes;

namespace FictionChecker.Bench;

/// <summary>Placeholder so the benchmark project runs before the checker exists.</summary>
[MemoryDiagnoser]
public class LibraryInfoBenchmarks
{
    [Benchmark]
    public string CodeVersion() => LibraryInfo.CodeVersion;
}
