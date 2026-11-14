using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FictionChecker.Eval.Scoring;

/// <summary>Writes reports: the committed numbers-only files and the local review files.</summary>
public static class ReportWriter
{
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
    /// Renders the report as Markdown. <paramref name="previous"/> is the last run of the same checker (changes
    /// are shown against it); <paramref name="baseline"/> is another checker to compare with, such as Harper.
    /// </summary>
    public static string ToMarkdown(EvalReport report, EvalReport? previous, EvalReport? baseline)
    {
        var sb = new StringBuilder();
        string vs = baseline is null ? "" : $" | {baseline.Checker}";
        string vsRule = baseline is null ? "" : "|---:";

        sb.AppendLine(CultureInfo.InvariantCulture, $"# Evaluation: {report.Checker}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Library {report.LibraryVersion}. Seed {report.Settings.Seed}, {report.Settings.CleanWordsPerBook:N0} clean words per book, {report.Settings.PlantedPerCategory} planted errors per category for each corpus and split.");
        sb.AppendLine();
        sb.AppendLine(previous is null ? "No earlier run to compare with." : "Changes are against the previous run of this checker.");
        sb.AppendLine();

        sb.AppendLine("## False alarms on clean text");
        sb.AppendLine();
        sb.AppendLine("Target: under 1 per 1,000 words. Flags confirmed as real errors in the source are not counted.");
        sb.AppendLine();
        sb.AppendLine($"| Corpus | Split | Books | Words | Flags | Per 1,000 words | Change{vs} | Confirmed real errors | Not checked |");
        sb.AppendLine($"|---|---|---:|---:|---:|---:|---:{vsRule}|---:|---:|");
        foreach (GroupResult g in report.Groups)
        {
            GroupResult? old = previous?.Find(g.Corpus, g.Split);
            GroupResult? other = baseline?.Find(g.Corpus, g.Split);
            string otherCell = baseline is null ? "" : $" | {(other is null ? "" : Num(other.FlagsPer1000))}";
            string notChecked = g.UncheckedParagraphs == 0 ? "0" : $"{g.UncheckedParagraphs} of {g.Paragraphs} paragraphs";
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {g.Corpus} | {g.Split} | {g.Books} | {g.Words:N0} | {g.Flags:N0} | {Num(g.FlagsPer1000)} | {Delta(g.FlagsPer1000, old?.FlagsPer1000)}{otherCell} | {g.ConfirmedRealErrors} | {notChecked} |");
        }

        sb.AppendLine();
        sb.AppendLine("## False alarms per rule");
        sb.AppendLine();
        sb.AppendLine("Flags per 1,000 words over all corpora. Target for a rule to be on by default: under 0.05.");
        sb.AppendLine();
        GroupResult? dev = report.Find(Scorer.AllCorpora, "dev");
        GroupResult? test = report.Find(Scorer.AllCorpora, "test");
        GroupResult? oldDev = previous?.Find(Scorer.AllCorpora, "dev");
        var ruleIds = (dev?.Rules ?? []).Concat(test?.Rules ?? []).Select(r => (r.RuleId, r.Category)).Distinct().ToList();
        if (ruleIds.Count == 0)
        {
            sb.AppendLine("No flags.");
        }
        else
        {
            sb.AppendLine("| Rule | Category | Dev | Test | Change (dev) |");
            sb.AppendLine("|---|---|---:|---:|---:|");
            foreach ((string ruleId, string category) in ruleIds)
            {
                double d = dev?.Rules.FirstOrDefault(r => r.RuleId == ruleId)?.Per1000 ?? 0;
                double t = test?.Rules.FirstOrDefault(r => r.RuleId == ruleId)?.Per1000 ?? 0;
                double? o = oldDev is null ? null : oldDev.Rules.FirstOrDefault(r => r.RuleId == ruleId)?.Per1000 ?? 0;
                sb.AppendLine(CultureInfo.InvariantCulture, $"| {ruleId} | {category} | {Num(d)} | {Num(t)} | {Delta(d, o)} |");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Planted errors");
        sb.AppendLine();
        sb.AppendLine("Caught: share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion restores the original text.");
        foreach (GroupResult g in report.Groups)
        {
            GroupResult? old = previous?.Find(g.Corpus, g.Split);
            GroupResult? other = baseline?.Find(g.Corpus, g.Split);
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"### {g.Corpus}, {g.Split}");
            sb.AppendLine();
            sb.AppendLine($"| Category | Planted | Checked | Caught | Change{(baseline is null ? "" : $" | {baseline.Checker} caught")} | First fix right | Fix in top 5 |");
            sb.AppendLine($"|---|---:|---:|---:|---:{vsRule}|---:|---:|");
            foreach (PlantedResult p in g.Planted)
            {
                PlantedResult? oldP = old?.Planted.FirstOrDefault(x => x.Category == p.Category);
                PlantedResult? otherP = other?.Planted.FirstOrDefault(x => x.Category == p.Category);
                string otherCell = baseline is null ? "" : $" | {(otherP is null ? "" : Percent(otherP.CatchRate))}";
                sb.AppendLine(CultureInfo.InvariantCulture, $"| {p.Category} | {p.Planted} | {p.Checked} | {Percent(p.CatchRate)} | {DeltaPercent(p.CatchRate, oldP?.CatchRate)}{otherCell} | {Percent(p.FirstFixRate)} | {Percent(p.Top5FixRate)} |");
            }
        }

        return sb.ToString();
    }

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
