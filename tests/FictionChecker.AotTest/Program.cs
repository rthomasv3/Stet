// Published with Native AOT to prove the library works without a JIT or reflection.
// Exits with 0 on success so it can be used as a check in CI.
using FictionChecker;

string version = LibraryInfo.CodeVersion;
if (string.IsNullOrEmpty(version) || version == "0.0.0")
{
    Console.Error.WriteLine("FAIL: library version not readable");
    return 1;
}

Console.WriteLine($"FictionChecker {version} (data format {LibraryInfo.DataFormatVersion}) OK");
return 0;
