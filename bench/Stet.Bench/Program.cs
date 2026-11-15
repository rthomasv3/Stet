// Benchmarks for the targets in the requirements (checker creation, paragraph, scene, novel).
// Run with: dotnet run -c Release --project bench/Stet.Bench -- --filter *
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
