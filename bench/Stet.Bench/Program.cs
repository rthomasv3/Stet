// Benchmarks for the operations an app does most, on the normal .NET runtime and compiled with Native AOT.
//
//   dotnet run -c Release --project bench/Stet.Bench -- --filter '*'
//   dotnet run -c Release --project bench/Stet.Bench -- --text /path/to/novel.txt --filter '*'
//
// --text names a plain text file with one paragraph per line, to time real prose in place of the made-up
// text. Everything else is passed to BenchmarkDotNet.
using BenchmarkDotNet.Running;
using Stet.Bench;

int at = Array.IndexOf(args, "--text");
if (at >= 0 && at + 1 < args.Length)
{
    // The benchmarks run in processes of their own, which inherit this.
    Environment.SetEnvironmentVariable(BenchText.Variable, Path.GetFullPath(args[at + 1]));
    args = [.. args[..at], .. args[(at + 2)..]];
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
