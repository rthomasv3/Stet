// Published with Native AOT to prove the library works without a JIT or reflection, through its public API.
// Exits with 0 on success so it can be used as a check in CI.
using System.Diagnostics;
using FictionChecker;

string version = LibraryInfo.CodeVersion;
if (string.IsNullOrEmpty(version) || version == "0.0.0")
{
    Console.Error.WriteLine("FAIL: library version not readable");
    return 1;
}

var clock = Stopwatch.StartNew();
var checker = new Checker(new CheckerOptions { Dialect = Dialect.British, QuoteStyle = QuoteStyle.Double });
clock.Stop();

checker.SetWords("world", ["Kaelith Vorn"]);
var cache = new CheckCache(checker);
string[] scene =
[
    "Kaelith Vorn could not beleive the the colour of the sky.",
    "\"It ain't nothin' to me,\" she said, and the color drained from her face.",
];
IReadOnlyList<ParagraphResult> results = cache.CheckDocument(scene);
IReadOnlyList<ParagraphResult> again = cache.CheckDocument(scene);
Issue[] issues = [.. results.SelectMany(r => r.Issues)];

bool ok = issues.Length == 3
    && issues[0] is { RuleId: RuleIds.Misspelling, Text: "beleive" } && issues[0].Suggestions[0].Text == "believe"
    && issues[1] is { RuleId: RuleIds.RepeatedWord, Category: RuleCategory.Mechanics, Text: "the" }
    && issues[2] is { RuleId: RuleIds.OtherDialect, Text: "color" }
    && ReferenceEquals(results[0], again[0])
    && checker.Rules.Count > 0
    && checker.IsValidWord("Kaelith") && !checker.IsValidWord("Kaelth") && checker.Suggest("Kaelth")[0] == "Kaelith"
    && checker.CheckParagraph(scene[0], new ParagraphContext { Options = checker.Options.WithCategory(RuleCategory.Spelling, false) }).Issues.Count == 1
    && checker.CheckParagraph("She was taller then her brother.").Issues is [{ RuleId: RuleIds.ConfusableThenThan, Category: RuleCategory.ConfusableWords }];
if (!ok)
{
    Console.Error.WriteLine("FAIL: unexpected results: " + string.Join("; ", issues.Select(i => i.ToString())));
    return 1;
}

Console.WriteLine($"FictionChecker {version} (data format {LibraryInfo.DataFormatVersion}) OK: checker created in {clock.ElapsedMilliseconds} ms, {issues.Length} issues found as expected");
return 0;
