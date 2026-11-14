// Published with Native AOT to prove the library works without a JIT or reflection.
// Exits with 0 on success so it can be used as a check in CI.
using System.Diagnostics;
using FictionChecker;
using FictionChecker.Spelling;

string version = LibraryInfo.CodeVersion;
if (string.IsNullOrEmpty(version) || version == "0.0.0")
{
    Console.Error.WriteLine("FAIL: library version not readable");
    return 1;
}

var clock = Stopwatch.StartNew();
Lexicon lexicon = Lexicon.LoadEmbedded();
BuiltInLists lists = BuiltInLists.LoadEmbedded();
clock.Stop();
if (!lexicon.TryGet("the", out WordInfo the) || !the.IsInEveryDialect || lists.Misspellings.Count == 0)
{
    Console.Error.WriteLine("FAIL: embedded spelling data did not load");
    return 1;
}

Console.WriteLine($"FictionChecker {version} (data format {LibraryInfo.DataFormatVersion}) OK: {lexicon.Count:N0} words loaded in {clock.ElapsedMilliseconds} ms");
return 0;
