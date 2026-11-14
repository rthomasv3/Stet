using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using FictionChecker;
using FictionChecker.Eval;
using FictionChecker.Eval.Checkers;
using FictionChecker.Eval.Corpora;
using FictionChecker.Eval.Planting;
using FictionChecker.Eval.Scoring;
using FictionChecker.Eval.Text;

// Usage:
//   run --checker none|demo|spelling|spelling-world
//   run --checker external --name harper --issues <issues.jsonl>   score issues produced outside this process
//   export --out <items.jsonl>                                      write every paragraph an external checker must check
//   export --out <items.jsonl> --corpus gutenberg --clean-words 2000 --planted 100     a smaller set for slow checkers
// Options: --compare <checker>   add a column comparing with another checker's committed report
//          --review-flags N      write up to N flags per rule and corpus to the local review file (dev books only)
//          --exclude-categories A,B   drop issues in these categories (with --name for the report name)
//          --root <dir>          repository root (found automatically by default)
if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Commands: run --checker none|demo|spelling|spelling-world|external [--name N --issues FILE] [--compare N]");
    Console.WriteLine("          export --out FILE");
    Console.WriteLine("          perf            time the spelling checker against the performance targets");
    Console.WriteLine("          coverage        measure how much of the books' vocabulary the spelling data knows");
    Console.WriteLine("          dialogue        measure text splitting and dialogue detection on whole books");
    return 0;
}

string command = args[0];
var options = new Dictionary<string, string>(StringComparer.Ordinal);
for (int i = 1; i + 1 < args.Length; i += 2)
{
    options[args[i]] = args[i + 1];
}

string root = options.GetValueOrDefault("--root") ?? FindRoot();
string evalDir = Path.Combine(root, "eval");
EvalSettings settings = EvalSettings.Load(Path.Combine(evalDir, "eval.json"));
if (options.TryGetValue("--review-flags", out string? reviewFlags))
{
    settings = settings with { ReviewFlagsPerRule = int.Parse(reviewFlags, CultureInfo.InvariantCulture) };
}

LocalSettings local = LocalSettings.Load(Path.Combine(evalDir, "eval.local.json"));

var corpora = new List<Corpus>();
foreach ((string name, string path) in local.Corpora.OrderBy(c => c.Key, StringComparer.Ordinal))
{
    if (!Directory.Exists(path))
    {
        Console.Error.WriteLine($"Skipping corpus '{name}': folder not found.");
        continue;
    }

    corpora.Add(Corpus.Load(name, path));
}

if (corpora.Count == 0)
{
    Console.Error.WriteLine("No corpora available. Check eval/eval.local.json.");
    return 1;
}

if (command == "dialogue")
{
    string dialogueReports = Path.Combine(evalDir, "reports");
    Directory.CreateDirectory(Path.Combine(dialogueReports, "local"));
    string dialogueMarkdown = DialogueStats.Run(corpora, Path.Combine(dialogueReports, "local", "dialogue-sample.tsv"));
    File.WriteAllText(Path.Combine(dialogueReports, "dialogue.md"), dialogueMarkdown);
    Console.WriteLine(dialogueMarkdown);
    return 0;
}

if (command == "perf")
{
    string perfMarkdown = PerfStats.Run(corpora);
    Directory.CreateDirectory(Path.Combine(evalDir, "reports"));
    File.WriteAllText(Path.Combine(evalDir, "reports", "perf.md"), perfMarkdown);
    Console.WriteLine(perfMarkdown);
    return 0;
}

if (command == "coverage")
{
    string coverageReports = Path.Combine(evalDir, "reports");
    Directory.CreateDirectory(Path.Combine(coverageReports, "local"));
    string coverageMarkdown = CoverageStats.Run(corpora, Path.Combine(coverageReports, "local", "unknown-words.tsv"));
    File.WriteAllText(Path.Combine(coverageReports, "coverage.md"), coverageMarkdown);
    Console.WriteLine(coverageMarkdown);
    return 0;
}

var clock = Stopwatch.StartNew();
WorkSet work = WorkSet.Build(corpora, settings, Planters.All);
long cleanWords = work.Clean.Sum(s => s.Paragraphs.Sum(p => (long)Words.Count(p)));
Console.WriteLine($"Work set: {work.Clean.Count} books, {cleanWords:N0} clean words, {work.Planted.Count:N0} planted errors ({clock.ElapsedMilliseconds} ms)");

if (command == "export")
{
    string outPath = options.GetValueOrDefault("--out") ?? throw new ArgumentException("export needs --out FILE");
    using var writer = new StreamWriter(outPath);
    int count = 0;
    IEnumerable<(string Text, string Dialect)> items = options.ContainsKey("--clean-words") || options.ContainsKey("--planted") || options.ContainsKey("--corpus")
        ? work.Subset(
            options.GetValueOrDefault("--corpus"),
            int.Parse(options.GetValueOrDefault("--clean-words") ?? "2000000000", CultureInfo.InvariantCulture),
            int.Parse(options.GetValueOrDefault("--planted") ?? "2000000000", CultureInfo.InvariantCulture))
        : work.AllTexts();
    foreach ((string text, string dialect) in items)
    {
        writer.WriteLine(JsonSerializer.Serialize(new { id = WorkSet.HashOf(text), dialect, text }));
        count++;
    }

    Console.WriteLine($"Wrote {count:N0} paragraphs to {outPath}");
    return 0;
}

if (command != "run")
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    return 1;
}

string checkerName = options.GetValueOrDefault("--checker") ?? "none";
IEvalChecker checker = checkerName switch
{
    "none" => new NoneChecker(),
    "demo" => new DemoChecker(),
    "spelling" => new SpellingEvalChecker(corpora, world: false),
    "spelling-world" => new SpellingEvalChecker(corpora, world: true),
    "external" => new ExternalChecker(
        options.GetValueOrDefault("--source-name") ?? options.GetValueOrDefault("--name") ?? throw new ArgumentException("external needs --name"),
        options.GetValueOrDefault("--issues") ?? throw new ArgumentException("external needs --issues FILE")),
    _ => throw new ArgumentException($"Unknown checker '{checkerName}'."),
};
if (options.TryGetValue("--exclude-categories", out string? excluded))
{
    string filteredName = options.GetValueOrDefault("--name") ?? checker.Name + "-filtered";
    checker = new FilteredChecker(checker, filteredName, excluded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
}

string reports = Path.Combine(evalDir, "reports");
string localReports = Path.Combine(reports, "local");
Directory.CreateDirectory(localReports);
string jsonPath = Path.Combine(reports, checker.Name + ".json");

EvalReport? previous = ReportWriter.TryRead(jsonPath);
EvalReport? baseline = options.TryGetValue("--compare", out string? compare)
    ? ReportWriter.TryRead(Path.Combine(reports, compare + ".json"))
    : null;

clock.Restart();
var scorer = new Scorer(settings, Labels.Load(Path.Combine(evalDir, "labels.json")));
EvalReport report = scorer.Run(checker, work, LibraryInfo.CodeVersion);
double seconds = clock.Elapsed.TotalSeconds;

ReportWriter.WriteJson(report, jsonPath);
string markdown = ReportWriter.ToMarkdown(report, previous, baseline);
File.WriteAllText(Path.Combine(reports, checker.Name + ".md"), markdown);
ReportWriter.WriteReviewFlags(scorer.ReviewFlags, Path.Combine(localReports, checker.Name + "-flags.tsv"));
ReportWriter.WriteMissed(scorer.Missed, Path.Combine(localReports, checker.Name + "-missed.tsv"));
File.WriteAllLines(Path.Combine(localReports, checker.Name + "-wrong-fix.tsv"), scorer.WrongFirstFix);
File.WriteAllLines(
    Path.Combine(localReports, checker.Name + "-books.tsv"),
    scorer.PerBook.OrderBy(kv => kv.Key.Corpus, StringComparer.Ordinal).ThenBy(kv => kv.Key.BookId, StringComparer.Ordinal)
        .Select(kv => $"{kv.Key.Corpus}\t{kv.Key.BookId}\t{kv.Key.Split}\t{kv.Value.Words}\t{kv.Value.Flags}\t{(kv.Value.Words == 0 ? 0 : kv.Value.Flags * 1000.0 / kv.Value.Words):0.00}"));
File.WriteAllLines(
    Path.Combine(localReports, checker.Name + "-flag-words.tsv"),
    scorer.FlaggedText.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key.Text, StringComparer.Ordinal).Select(kv => $"{kv.Value}\t{kv.Key.Corpus}\t{kv.Key.RuleId}\t{kv.Key.Text}"));

Console.WriteLine(markdown);
if (checker is SpellingEvalChecker { WorldWordCounts.Count: > 0 } spelling)
{
    Console.WriteLine($"Stand-in world lists: {spelling.WorldWordCounts.Values.Average():0} words per book on average ({spelling.WorldWordCounts.Values.Min()} to {spelling.WorldWordCounts.Values.Max()}).");
}

Console.WriteLine($"Checked in {seconds:0.0} s. Reports: eval/reports/{checker.Name}.md and .json; review files in eval/reports/local/.");
return 0;

static string FindRoot()
{
    string? dir = Directory.GetCurrentDirectory();
    while (dir is not null && !File.Exists(Path.Combine(dir, "FictionChecker.slnx")))
    {
        dir = Path.GetDirectoryName(dir);
    }

    return dir ?? throw new DirectoryNotFoundException("Run from inside the repository, or pass --root.");
}
