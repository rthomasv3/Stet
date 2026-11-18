using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Stet;
using Stet.Eval;
using Stet.Eval.Checkers;
using Stet.Eval.Corpora;
using Stet.Eval.Planting;
using Stet.Eval.Scoring;
using Stet.Eval.Text;

// Usage:
//   snapshot [--out FILE]   every issue and suggestion on the work set, to compare before and after a change
//   sideset [--detail]      Stet on the small made-up side set (eval/sideset)
//   confusable-dump         how each confusable word in the dev books fits its neighbours, for choosing thresholds
//   names --issues <issues.jsonl> --out <names.json>               names another checker does not know that the word lists lack
//   run --checker none|demo|spelling|spelling-world|stet|stet-all
//   run --checker external --name harper --checker-version "harper.js 3.2.0" --issues <issues.jsonl>   score issues produced outside this process
//   export --out <items.jsonl> [--word-lists <lists.json>]          write every paragraph an external checker must check,
//                                                                   and each book's stand-in word list
//   export --out <items.jsonl> --corpus classic --clean-words 2000 --planted 100     a smaller set for slow checkers
// Options: --extra-words <names.json>  add these words to each book's word list (from the names command)
//          --compare <checker>   add a column comparing with another checker's report
//          --review-flags N      write up to N flags per rule and corpus to the local review file (dev books only)
//          --exclude-categories A,B   drop issues in these categories (with --name for the report name)
//          --root <dir>          repository root (found automatically by default)
if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Commands: run --checker none|demo|spelling|spelling-world|stet|stet-all|external [--name N --issues FILE] [--compare N]");
    Console.WriteLine("          export --out FILE");
    Console.WriteLine("          perf            time the spelling checker against the performance targets");
    Console.WriteLine("          coverage        measure how much of the books' vocabulary the spelling data knows");
    Console.WriteLine("          dialogue        measure text splitting and dialogue detection on whole books");
    return 0;
}

string command = args[0];
if (command == "sideset")
{
    // The small made-up side set; needs no corpora.
    Console.Write(SideSet.Run(Path.Combine(FindRoot(), "eval", "sideset", "set.tsv"), args.Contains("--detail")));
    return 0;
}

if (command == "confusable-explain")
{
    // Shows the evidence behind the confusable-word rules for the sentences given.
    foreach (string sentence in args.Skip(1))
    {
        ConfusableDump.Explain(sentence);
    }

    return 0;
}

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

// More words for each book's word list, beyond the ones Stet's own run finds (see the "names" command).
Dictionary<string, List<string>>? extraWords = options.TryGetValue("--extra-words", out string? extraPath)
    ? JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(extraPath))
    : null;
long cleanWords = work.Clean.Sum(s => s.Paragraphs.Sum(p => (long)Words.Count(p)));
Console.WriteLine($"Work set: {work.Clean.Count} books, {cleanWords:N0} clean words, {work.Planted.Count:N0} planted errors ({clock.ElapsedMilliseconds} ms)");

if (command == "confusable-dump")
{
    Directory.CreateDirectory(Path.Combine(evalDir, "reports", "local"));
    string dumpPath = options.GetValueOrDefault("--out") ?? Path.Combine(evalDir, "reports", "local", "confusables-dev.tsv");
    Console.WriteLine($"Wrote {ConfusableDump.Run(work, dumpPath):N0} rows to {dumpPath}");
    return 0;
}

if (command == "export")
{
    string outPath = options.GetValueOrDefault("--out") ?? throw new ArgumentException("export needs --out FILE");
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
    using var writer = new StreamWriter(outPath);
    int count = 0;
    IEnumerable<(string Text, string Dialect, string Book)> items = options.ContainsKey("--clean-words") || options.ContainsKey("--planted") || options.ContainsKey("--corpus")
        ? work.Subset(
            options.GetValueOrDefault("--corpus"),
            int.Parse(options.GetValueOrDefault("--clean-words") ?? "2000000000", CultureInfo.InvariantCulture),
            int.Parse(options.GetValueOrDefault("--planted") ?? "2000000000", CultureInfo.InvariantCulture))
        : work.AllTexts();
    var books = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach ((string text, string dialect, string book) in items)
    {
        writer.WriteLine(JsonSerializer.Serialize(new { id = WorkSet.HashOf(text), dialect, book, text }));
        books[book] = dialect;
        count++;
    }

    Console.WriteLine($"Wrote {count:N0} paragraphs to {outPath}");
    if (options.TryGetValue("--word-lists", out string? wordListPath))
    {
        // The stand-in word list Stet's own run gives each book, so the other checker can be given the same.
        var standIn = new SpellingEvalChecker(corpora, world: true, spellingOnly: false, extraWords: extraWords);
        var lists = books.ToDictionary(
            kv => kv.Key,
            kv => standIn.WordListFor(kv.Key[..kv.Key.IndexOf('/')], kv.Key[(kv.Key.IndexOf('/') + 1)..], kv.Value),
            StringComparer.Ordinal);
        File.WriteAllText(wordListPath, JsonSerializer.Serialize(lists));
        Console.WriteLine($"Wrote word lists for {lists.Count} books to {wordListPath} ({lists.Values.Average(l => l.Count):0} words per book on average)");
    }
    return 0;
}

if (command == "names")
{
    // The word lists are made from the words Stet does not know, so a made-up name Stet happens to accept
    // (a surname that is also an ordinary word) is missing from them, and another checker is flagged for it
    // every time. This finds those: any capitalized word the other checker flags as misspelled twice or more
    // in a book's clean text, and that is not on the book's list in any form, is added for that book.
    // It cannot tell a made-up name from a real one the other checker lacks, so it forgives both.
    string issuesPath = options.GetValueOrDefault("--issues") ?? throw new ArgumentException("names needs --issues FILE");
    string namesPath = options.GetValueOrDefault("--out") ?? throw new ArgumentException("names needs --out FILE");
    var flaggedById = new Dictionary<string, List<(int Start, int Length)>>(StringComparer.Ordinal);
    foreach (string line in File.ReadLines(issuesPath))
    {
        if (line.Length == 0)
        {
            continue;
        }

        using JsonDocument parsed = JsonDocument.Parse(line);
        var spans = new List<(int, int)>();
        foreach (JsonElement issue in parsed.RootElement.GetProperty("issues").EnumerateArray())
        {
            if (issue.TryGetProperty("category", out JsonElement category) && category.GetString() == "Spelling")
            {
                spans.Add((issue.GetProperty("start").GetInt32(), issue.GetProperty("length").GetInt32()));
            }
        }

        flaggedById[parsed.RootElement.GetProperty("id").GetString()!] = spans;
    }

    var ownLists = new SpellingEvalChecker(corpora, world: true, spellingOnly: false);
    var found = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    foreach (CleanSample sample in work.Clean)
    {
        var onList = ownLists.WordListFor(sample.Corpus, sample.Book.Id, sample.Book.Dialect)
            .SelectMany(entry => entry.Split(' ')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string text in sample.Paragraphs)
        {
            if (!flaggedById.TryGetValue(WorkSet.HashOf(text), out List<(int Start, int Length)>? spans))
            {
                continue;
            }

            foreach ((int start, int length) in spans)
            {
                if (start < 0 || start + length > text.Length || length == 0 || !char.IsUpper(text[start]))
                {
                    continue;
                }

                // A possessive counts as its name: "Venters's" and "Venters'" as "Venters".
                string word = text.Substring(start, length).Replace('\u2019', '\'');
                word = word.EndsWith("'s", StringComparison.Ordinal) ? word[..^2] : word.TrimEnd('\'');
                if (word.Length > 1 && word.All(c => char.IsLetter(c) || c is '\'' or '-') && !onList.Contains(word))
                {
                    counts[word] = counts.GetValueOrDefault(word) + 1;
                }
            }
        }

        found[sample.Corpus + "/" + sample.Book.Id] = [.. counts.Where(kv => kv.Value >= 2).Select(kv => kv.Key).Order(StringComparer.Ordinal)];
    }

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(namesPath))!);
    File.WriteAllText(namesPath, JsonSerializer.Serialize(found));
    Console.WriteLine($"Wrote {found.Values.Sum(l => l.Count):N0} names for {found.Count} books to {namesPath} ({found.Values.Average(l => l.Count):0.0} per book on average)");
    return 0;
}

if (command == "snapshot")
{
    // Every issue Stet reports on the work set, with its suggestions in order, and no word list (so
    // names are judged and suggested for too). Two snapshots that are byte for byte the same show a change
    // to Stet did not alter any result. Holds book text: local only.
    string outPath = options.GetValueOrDefault("--out") ?? Path.Combine(evalDir, "reports", "local", "snapshot.tsv");
    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
    var stet = new Stet.Checker();
    using var writer = new StreamWriter(outPath);
    long issueCount = 0;
    void Write(string id, IReadOnlyList<Stet.ParagraphResult> results)
    {
        for (int p = 0; p < results.Count; p++)
        {
            foreach (Stet.Issue issue in results[p].Issues)
            {
                writer.WriteLine($"{id}\t{p}\t{issue.Start}\t{issue.Length}\t{issue.RuleId}\t{issue.Text}\t{string.Join("|", issue.Suggestions.Select(s => $"{s.Start}+{s.Length}:{s.Text}"))}");
                issueCount++;
            }
        }
    }

    foreach (CleanSample sample in work.Clean)
    {
        foreach (Stet.Dialect dialect in (ReadOnlySpan<Stet.Dialect>)[Stet.Dialect.American, Stet.Dialect.British])
        {
            Write($"{sample.Corpus}/{sample.Book.Id}/{dialect}", stet.CheckDocument(sample.Paragraphs, new Stet.CheckerOptions { Dialect = dialect }));
        }
    }

    foreach (PlantedItem item in work.Planted)
    {
        Write($"planted/{item.BookId}/{item.ParagraphIndex}/{item.Category}", [stet.CheckParagraph(item.Error.Text)]);
    }

    Console.WriteLine($"Wrote {issueCount:N0} issues to {outPath} ({clock.ElapsedMilliseconds} ms)");
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
    "spelling-world" => new SpellingEvalChecker(corpora, world: true, extraWords: extraWords),
    "stet" => new SpellingEvalChecker(corpora, world: true, spellingOnly: false, extraWords: extraWords),
    "stet-all" => new SpellingEvalChecker(corpora, world: true, spellingOnly: false, everyRule: true, extraWords: extraWords),
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
// Reported under the release the code belongs to: a pre-release label ("-rc.1") is left off.
EvalReport report = scorer.Run(checker, work, LibraryInfo.CodeVersion.Split('-')[0]);
report = report with
{
    Description = checkerName switch
    {
        "stet" => "Spelling and grammar at Stet's defaults. Each book has a stand-in for the writer's word list: every unknown word the book uses twice or more.",
        "stet-all" => "Spelling and grammar with every rule switched on. Each book has a stand-in for the writer's word list: every unknown word the book uses twice or more.",
        "spelling" => "Spelling only, with no word list, so every name and invented word in a book is flagged. Grammar rules are off, which is why only typos are caught among the planted errors.",
        "spelling-world" => "Spelling only. Each book has a stand-in for the writer's word list: every unknown word the book uses twice or more.",
        _ => null,
    },
};
if (checkerName == "external")
{
    // Another checker's numbers only mean something with its version beside them.
    report = report with { CheckerVersion = options.GetValueOrDefault("--checker-version") ?? "version not given" };
}

double seconds = clock.Elapsed.TotalSeconds;

ReportWriter.WriteJson(report, jsonPath);
string markdown = ReportWriter.ToMarkdown(report, baseline);
File.WriteAllText(Path.Combine(reports, checker.Name + ".md"), markdown);
ReportWriter.WriteReviewFlags(scorer.ReviewFlags, Path.Combine(localReports, checker.Name + "-flags.tsv"));
ReportWriter.WriteMissed(scorer.Missed, Path.Combine(localReports, checker.Name + "-missed.tsv"));
File.WriteAllLines(Path.Combine(localReports, checker.Name + "-wrong-fix.tsv"), scorer.WrongFirstFix);
File.WriteAllLines(
    Path.Combine(localReports, checker.Name + "-books.tsv"),
    scorer.PerBook.OrderBy(kv => kv.Key.Corpus, StringComparer.Ordinal).ThenBy(kv => kv.Key.BookId, StringComparer.Ordinal)
        .Select(kv => $"{kv.Key.Corpus}\t{kv.Key.BookId}\t{kv.Key.Split}\t{kv.Value.Words}\t{kv.Value.Flags}\t{(kv.Value.Words == 0 ? 0 : kv.Value.Flags * 1000.0 / kv.Value.Words):0.00}"));
File.WriteAllLines(
    Path.Combine(localReports, checker.Name + "-book-rules.tsv"),
    scorer.PerBookRule.OrderBy(kv => kv.Key.RuleId, StringComparer.Ordinal).ThenByDescending(kv => kv.Value)
        .Select(kv => $"{kv.Key.RuleId}\t{kv.Key.Split}\t{kv.Key.Corpus}\t{kv.Key.BookId}\t{kv.Value}"));
File.WriteAllLines(
    Path.Combine(localReports, checker.Name + "-flag-words.tsv"),
    scorer.FlaggedText.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key.Text, StringComparer.Ordinal).Select(kv => $"{kv.Value}\t{kv.Key.Corpus}\t{kv.Key.RuleId}\t{kv.Key.Text}"));

Console.WriteLine(markdown);
string changes = previous is null ? "" : ReportWriter.Changes(report, previous);
if (changes.Length > 0)
{
    Console.WriteLine("Changed since the last run of this checker:");
    Console.WriteLine(changes);
}
else if (previous is not null)
{
    Console.WriteLine("Nothing changed since the last run of this checker.");
}

if (checker is SpellingEvalChecker { WorldWordCounts.Count: > 0 } spelling)
{
    Console.WriteLine($"Stand-in world lists: {spelling.WorldWordCounts.Values.Average():0} words per book on average ({spelling.WorldWordCounts.Values.Min()} to {spelling.WorldWordCounts.Values.Max()}).");
}

Console.WriteLine($"Checked in {seconds:0.0} s. Reports: eval/reports/{checker.Name}.md and .json; review files in eval/reports/local/.");
return 0;

static string FindRoot()
{
    string? dir = Directory.GetCurrentDirectory();
    while (dir is not null && !File.Exists(Path.Combine(dir, "Stet.slnx")))
    {
        dir = Path.GetDirectoryName(dir);
    }

    return dir ?? throw new DirectoryNotFoundException("Run from inside the repository, or pass --root.");
}
