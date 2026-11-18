using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Stet.Eval.Scoring;

/// <summary>Writes reports: the numbers-only files and the local review files.</summary>
public static class ReportWriter
{
    private const double RuleBar = 0.05;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Reads a report written earlier, or null when there is none.</summary>
    public static EvalReport? TryRead(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<EvalReport>(File.ReadAllText(path), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Writes the report as JSON.</summary>
    public static void WriteJson(EvalReport report, string path) =>
        File.WriteAllText(path, JsonSerializer.Serialize(report, JsonOptions) + "\n");

    /// <summary>
    /// Renders the report as Markdown, as it is saved: what was measured and nothing else.
    /// <paramref name="baseline"/> is another checker to show alongside, such as Harper.
    /// </summary>
    public static string ToMarkdown(EvalReport report, EvalReport? baseline)
    {
        var sb = new StringBuilder();
        string vs = baseline is null ? "" : $" | {baseline.Checker}";
        string vsRule = baseline is null ? "" : "|---:";

        // A group with next to nothing checked is left out: a run on part of the text reaches a few paragraphs
        // of the other corpora too, where a short paragraph is word for word the same in both.
        var groups = report.Groups.Where(g => g.Paragraphs == 0 || (g.Paragraphs - g.UncheckedParagraphs) * 100L >= g.Paragraphs).ToList();
        bool anyLabels = groups.Any(g => g.ConfirmedRealErrors > 0);
        bool anyUnchecked = groups.Any(g => g.UncheckedParagraphs > 0);

        sb.AppendLine(CultureInfo.InvariantCulture, $"# Evaluation: {report.Checker}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"{Subject(report)}. {report.Settings.CleanWordsPerBook:N0} words of clean text per book, and {report.Settings.PlantedPerCategory} planted errors per category for each corpus and each side of the split (seed {report.Settings.Seed}).");
        if (report.Description is not null)
        {
            sb.AppendLine();
            sb.AppendLine(report.Description);
        }

        if (baseline is not null)
        {
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"Shown alongside: {Subject(baseline)}.");
        }

        sb.AppendLine();
        sb.AppendLine("Every book is on one side of a split. **Dev** books could be read and tuned against while Stet was built. **Test** books were only ever measured, so their numbers show how a checker does on text nobody tuned it for.");
        sb.AppendLine();

        sb.AppendLine("## False alarms on clean text");
        sb.AppendLine();
        sb.AppendLine(anyLabels
            ? "Flags on published text, which is taken to be correct. Flags reviewed and confirmed as real errors in the books are not counted."
            : "Flags on published text, which is taken to be correct.");
        sb.AppendLine();
        sb.AppendLine($"| Corpus | Split | Books | Words | Flags | Per 1,000 words{vs}{(anyLabels ? " | Confirmed real errors" : "")}{(anyUnchecked ? " | Not checked" : "")} |");
        sb.AppendLine($"|---|---|---:|---:|---:|---:{vsRule}{(anyLabels ? "|---:" : "")}{(anyUnchecked ? "|---:" : "")}|");
        foreach (GroupResult g in groups)
        {
            GroupResult? other = baseline?.Find(g.Corpus, g.Split);
            string otherCell = baseline is null ? "" : $" | {(other is null ? "" : Num(other.FlagsPer1000))}";
            string labelCell = anyLabels ? $" | {g.ConfirmedRealErrors}" : "";
            string uncheckedCell = !anyUnchecked ? "" : g.UncheckedParagraphs == 0 ? " | 0" : $" | {g.UncheckedParagraphs:N0} of {g.Paragraphs:N0} paragraphs";
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {g.Corpus} | {g.Split} | {g.Books} | {g.Words:N0} | {g.Flags:N0} | {Num(g.FlagsPer1000)}{otherCell}{labelCell}{uncheckedCell} |");
        }

        sb.AppendLine();
        sb.AppendLine("## False alarms per rule");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Flags per 1,000 words over all the books. Rules under {RuleBar} on both sides are counted, not listed.");
        sb.AppendLine();
        GroupResult? dev = report.Find(Scorer.AllCorpora, "dev");
        GroupResult? test = report.Find(Scorer.AllCorpora, "test");
        var ruleIds = (dev?.Rules ?? []).Concat(test?.Rules ?? []).Select(r => (r.RuleId, r.Category)).Distinct().ToList();
        if (ruleIds.Count == 0)
        {
            sb.AppendLine("No flags.");
        }
        else
        {
            sb.AppendLine("| Rule | Category | Dev | Test |");
            sb.AppendLine("|---|---|---:|---:|");
            int underBar = 0;
            foreach ((string ruleId, string category) in ruleIds)
            {
                double d = dev?.Rules.FirstOrDefault(r => r.RuleId == ruleId)?.Per1000 ?? 0;
                double t = test?.Rules.FirstOrDefault(r => r.RuleId == ruleId)?.Per1000 ?? 0;
                if (d < RuleBar && t < RuleBar)
                {
                    underBar++;
                    continue;
                }

                sb.AppendLine(CultureInfo.InvariantCulture, $"| {ruleId} | {category} | {Num(d)} | {Num(t)} |");
            }

            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"{underBar} other rules flagged something but stayed under {RuleBar}.");
        }

        sb.AppendLine();
        sb.AppendLine("## Planted errors");
        sb.AppendLine();
        sb.AppendLine("Over all the books. Caught: the share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion gives back the original text. The same figures for each corpus are in the JSON file beside this one.");
        foreach (string split in (ReadOnlySpan<string>)["test", "dev"])
        {
            GroupResult? g = report.Find(Scorer.AllCorpora, split);
            if (g is null || g.Planted.All(p => p.Checked == 0))
            {
                continue; // nothing on this side was checked (a partial run)
            }

            GroupResult? other = baseline?.Find(g.Corpus, g.Split);
            bool allChecked = g.Planted.All(p => p.Checked == p.Planted);
            sb.AppendLine();
            sb.AppendLine(split == "test" ? "### Test books" : "### Dev books");
            sb.AppendLine();
            sb.AppendLine($"| Category | Planted{(allChecked ? "" : " | Checked")} | Caught{(baseline is null ? "" : $" | {baseline.Checker} caught")} | First fix right | Fix in top 5 |");
            sb.AppendLine($"|---|---:{(allChecked ? "" : "|---:")}|---:{vsRule}|---:|---:|");
            foreach (PlantedResult p in g.Planted)
            {
                PlantedResult? otherP = other?.Planted.FirstOrDefault(x => x.Category == p.Category);
                string otherCell = baseline is null ? "" : $" | {(otherP is null ? "" : Percent(otherP.CatchRate))}";
                string checkedCell = allChecked ? "" : $" | {p.Checked:N0}";
                sb.AppendLine(CultureInfo.InvariantCulture, $"| {p.Category} | {p.Planted:N0}{checkedCell} | {Percent(p.CatchRate)}{otherCell} | {Percent(p.FirstFixRate)} | {Percent(p.Top5FixRate)} |");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// What moved since the last run of the same checker, for the terminal: it matters to whoever just made a
    /// change and to nobody who reads the saved report. Empty when nothing moved.
    /// </summary>
    public static string Changes(EvalReport report, EvalReport previous)
    {
        var sb = new StringBuilder();
        foreach (GroupResult g in report.Groups)
        {
            GroupResult? old = previous.Find(g.Corpus, g.Split);
            if (old is null)
            {
                continue;
            }

            string flags = Delta(g.FlagsPer1000, old.FlagsPer1000);
            if (flags != "0")
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  {g.Corpus}, {g.Split}: flags per 1,000 words {Num(old.FlagsPer1000)} -> {Num(g.FlagsPer1000)} ({flags})");
            }

            foreach (PlantedResult p in g.Planted)
            {
                PlantedResult? oldP = old.Planted.FirstOrDefault(x => x.Category == p.Category);
                string caught = DeltaPercent(p.CatchRate, oldP?.CatchRate);
                if (caught is not ("" or "0"))
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  {g.Corpus}, {g.Split}: {p.Category} caught {Percent(oldP!.CatchRate)} -> {Percent(p.CatchRate)} ({caught})");
                }
            }

            if (g.Corpus != Scorer.AllCorpora)
            {
                continue;
            }

            foreach (string ruleId in g.Rules.Select(r => r.RuleId).Concat(old.Rules.Select(r => r.RuleId)).Distinct())
            {
                double now = g.Rules.FirstOrDefault(r => r.RuleId == ruleId)?.Per1000 ?? 0;
                double before = old.Rules.FirstOrDefault(r => r.RuleId == ruleId)?.Per1000 ?? 0;
                string rule = Delta(now, before);
                if (rule != "0")
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  {g.Corpus}, {g.Split}: {ruleId} {Num(before)} -> {Num(now)} ({rule})");
                }
            }
        }

        return sb.ToString();
    }

    // What a report measured: Stet at its version, or another checker at the version it was run at.
    private static string Subject(EvalReport report) =>
        report.CheckerVersion is null ? $"Stet {report.LibraryVersion}" : $"{report.Checker} ({report.CheckerVersion})";

    /// <summary>Writes flags on clean text for review. Contains book text, so it goes to the local folder only.</summary>
    public static void WriteReviewFlags(IEnumerable<ReviewFlag> flags, string path)
    {
        var sb = new StringBuilder("corpus\tbook\tparagraph\thash\tstart\tlength\trule\tsnippet\n");
        foreach (ReviewFlag f in flags)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{f.Corpus}\t{f.BookId}\t{f.Paragraph}\t{f.Hash}\t{f.Start}\t{f.Length}\t{f.RuleId}\t{f.Snippet.Replace('\t', ' ')}\n");
        }

        File.WriteAllText(path, sb.ToString());
    }

    /// <summary>Writes planted errors that were not caught. Contains book text, so it goes to the local folder only.</summary>
    public static void WriteMissed(IEnumerable<MissedPlant> missed, string path)
    {
        var sb = new StringBuilder("corpus\tbook\tparagraph\tcategory\tkind\tsnippet\n");
        foreach (MissedPlant m in missed)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{m.Corpus}\t{m.BookId}\t{m.Paragraph}\t{m.Category}\t{m.Kind}\t{m.Snippet.Replace('\t', ' ')}\n");
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static string Num(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Percent(double rate) => (rate * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";

    private static string Delta(double now, double? before)
    {
        if (before is null)
        {
            return "";
        }

        double change = Math.Round(now - before.Value, 3);
        return change == 0 ? "0" : change.ToString("+0.###;-0.###", CultureInfo.InvariantCulture);
    }

    private static string DeltaPercent(double now, double? before)
    {
        if (before is null)
        {
            return "";
        }

        double change = Math.Round((now - before.Value) * 100, 1);
        return change == 0 ? "0" : change.ToString("+0.#;-0.#", CultureInfo.InvariantCulture) + " pts";
    }
}
