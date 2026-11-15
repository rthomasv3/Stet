using System.Text;

namespace Stet.Eval;

/// <summary>
/// Runs the library over the small made-up side set (eval/sideset/set.tsv) and reports, per category, how
/// many errors were flagged, and which correct lines were flagged.
/// </summary>
public static class SideSet
{
    /// <summary>Checks every line and returns the report. With <paramref name="detail"/> each line is listed.</summary>
    public static string Run(string path, bool detail)
    {
        var checker = new Checker();
        var caught = new Dictionary<string, (int Hit, int Total)>(StringComparer.Ordinal);
        var order = new List<string>();
        var lines = new StringBuilder();
        int cleanFlagged = 0;
        int cleanFlags = 0;
        int cleanLines = 0;
        int extra = 0;
        foreach (string line in File.ReadLines(path))
        {
            string[] parts = line.Split('\t');
            if (parts.Length != 2)
            {
                continue;
            }

            string category = parts[0];
            string marked = parts[1];
            int start = marked.IndexOf("[[", StringComparison.Ordinal);
            int end = marked.IndexOf("]]", StringComparison.Ordinal) - 2;
            string text = marked.Replace("[[", string.Empty, StringComparison.Ordinal).Replace("]]", string.Empty, StringComparison.Ordinal);
            var options = new CheckerOptions
            {
                QuoteStyle = text.Contains('‘') ? QuoteStyle.Single : QuoteStyle.Double,
                Dialect = text.Contains("colour", StringComparison.Ordinal) || text.Contains("Mum", StringComparison.Ordinal) ? Dialect.British : Dialect.American,
            };
            IReadOnlyList<Issue> issues = checker.CheckParagraph(text, new ParagraphContext { Options = options }).Issues;
            if (start < 0)
            {
                cleanLines++;
                cleanFlags += issues.Count;
                cleanFlagged += issues.Count > 0 ? 1 : 0;
                if (issues.Count > 0)
                {
                    lines.AppendLine($"  flagged: {text}  [{string.Join(", ", issues.Select(i => i.Text + " " + i.RuleId))}]");
                }

                continue;
            }

            if (!caught.ContainsKey(category))
            {
                order.Add(category);
            }

            List<Issue> hits = issues.Where(i => i.Start < end + 1 && i.Start + i.Length > start - 1).ToList();
            (int hit, int total) = caught.GetValueOrDefault(category);
            caught[category] = (hit + (hits.Count > 0 ? 1 : 0), total + 1);
            extra += issues.Count - hits.Count;
            if (detail || hits.Count == 0)
            {
                lines.AppendLine($"  {(hits.Count > 0 ? "caught" : "missed")}: {text}{(hits.Count > 0 ? "  [" + hits[0].RuleId + (hits[0].Suggestions.Count > 0 ? " -> " + hits[0].Suggestions[0].Text : string.Empty) + "]" : string.Empty)}");
            }
        }

        var report = new StringBuilder();
        foreach (string category in order)
        {
            report.AppendLine($"{category,-20} {caught[category].Hit,3} of {caught[category].Total}");
        }

        report.AppendLine($"{"all errors",-20} {caught.Values.Sum(c => c.Hit),3} of {caught.Values.Sum(c => c.Total)}   (other flags on those lines: {extra})");
        report.AppendLine($"{"correct lines",-20} {cleanFlagged,3} of {cleanLines} flagged, {cleanFlags} flags");
        report.Append(lines);
        return report.ToString();
    }
}
