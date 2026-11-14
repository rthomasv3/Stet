using System.Diagnostics;
using System.Globalization;
using System.Text;
using FictionChecker.Eval.Corpora;
using FictionChecker.Segmentation;
using FictionChecker.Spelling;

namespace FictionChecker.Eval;

/// <summary>Times the spelling checker against the performance targets in the requirements.</summary>
public static class PerfStats
{
    /// <summary>Runs the timings on the longest book of each corpus and returns a Markdown table.</summary>
    public static string Run(IReadOnlyList<Corpus> corpora)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Spelling checker speed");
        sb.AppendLine();
        sb.AppendLine("Measured on this machine, on the first 100,000 words of the longest book in each corpus. \"No word list\" is the worst case: every name is unknown and gets suggestions.");
        sb.AppendLine();
        sb.AppendLine("| Operation | Target | " + string.Join(" | ", corpora.Select(c => c.Name)) + " |");
        sb.AppendLine("|---|---:|" + string.Concat(corpora.Select(_ => "---:|")));

        long memoryBefore = GC.GetTotalMemory(true);
        var clock = Stopwatch.StartNew();
        SpellChecker first = SpellChecker.CreateDefault();
        clock.Stop();
        long firstLoad = clock.ElapsedMilliseconds;
        double checkerMegabytes = (GC.GetTotalMemory(true) - memoryBefore) / (1024.0 * 1024.0);
        GC.KeepAlive(first);
        clock.Restart();
        SpellChecker warm = SpellChecker.CreateDefault();
        clock.Stop();
        long load = clock.ElapsedMilliseconds;

        var rows = new List<string[]>
        {
            new[] { "Create a checker (first time in the process)", "500 ms" },
            new[] { "Create a checker (again)", "500 ms" },
            new[] { "100,000-word novel, no word list", "3 s" },
            new[] { "100,000-word novel, with word list", "3 s" },
            new[] { "1,500-word scene, with word list (median / worst)", "50 ms" },
            new[] { "One paragraph, with word list (median / worst)", "5 ms / 20 ms" },
            new[] { "Suggestions for one word (median / worst)", "5 ms" },
            new[] { "Replace a 20,000-word word set", "50 ms" },
        };

        foreach (Corpus corpus in corpora)
        {
            Book book = corpus.Books.OrderByDescending(b => b.Paragraphs.Count).First();
            var paragraphs = new List<string>();
            int words = 0;
            foreach (string p in book.Paragraphs)
            {
                if (words >= 100_000)
                {
                    break;
                }

                paragraphs.Add(p);
                words += Text.Words.Count(p);
            }

            var options = new SpellingOptions { Dialect = book.Dialect == "GB" ? Dialect.GBIse : Dialect.US, QuoteStyle = DialogueDetector.GuessStyle(paragraphs) };
            var checker = SpellChecker.CreateDefault();
            checker.CheckDocument(paragraphs.Take(50).ToList(), options); // warm up the JIT

            checker = SpellChecker.CreateDefault();
            clock.Restart();
            List<SpellingResult> results = checker.CheckDocument(paragraphs, options);
            clock.Stop();
            long novelBare = clock.ElapsedMilliseconds;

            // A word list like a writer's: every unknown word the book uses twice or more.
            var unknown = results.SelectMany(r => r.Issues).Where(i => i.Kind is SpellingKind.Misspelling or SpellingKind.UnknownName)
                .GroupBy(i => i.Word, StringComparer.Ordinal).Where(g => g.Count() >= 2).Select(g => g.Key).ToList();
            checker = SpellChecker.CreateDefault();
            checker.SetWords("world", unknown);
            clock.Restart();
            results = checker.CheckDocument(paragraphs, options);
            clock.Stop();
            long novelWorld = clock.ElapsedMilliseconds;

            // Scenes of about 1,500 words and single paragraphs, on a fresh checker so nothing is cached.
            checker = SpellChecker.CreateDefault();
            checker.SetWords("world", unknown);
            var sceneTimes = new List<double>();
            var paragraphTimes = new List<double>();
            double sceneTime = 0;
            int sceneWords = 0;
            foreach (string p in paragraphs)
            {
                clock.Restart();
                checker.CheckParagraph(p, options);
                clock.Stop();
                paragraphTimes.Add(clock.Elapsed.TotalMilliseconds);
                sceneTime += clock.Elapsed.TotalMilliseconds;
                sceneWords += Text.Words.Count(p);
                if (sceneWords >= 1500)
                {
                    sceneTimes.Add(sceneTime);
                    sceneTime = 0;
                    sceneWords = 0;
                }
            }

            var suggestTimes = new List<double>();
            checker = SpellChecker.CreateDefault();
            foreach (string word in results.SelectMany(r => r.Issues).Select(i => i.Word).Distinct(StringComparer.Ordinal).Take(400))
            {
                clock.Restart();
                checker.Suggest(word, options);
                clock.Stop();
                suggestTimes.Add(clock.Elapsed.TotalMilliseconds);
            }

            string[] names = Enumerable.Range(0, 20000).Select(i => "Nm" + Convert.ToString(i, 16).Replace('0', 'q')).ToArray();
            checker.SetWords("warm", names);
            clock.Restart();
            checker.SetWords("world", names);
            clock.Stop();

            string[] cells =
            [
                $"{firstLoad} ms",
                $"{load} ms",
                $"{novelBare / 1000.0:0.00} s",
                $"{novelWorld / 1000.0:0.00} s",
                $"{Median(sceneTimes):0.0} ms / {(sceneTimes.Count == 0 ? 0 : sceneTimes.Max()):0.0} ms",
                $"{Median(paragraphTimes):0.00} ms / {paragraphTimes.Max():0.0} ms",
                $"{Median(suggestTimes):0.00} ms / {(suggestTimes.Count == 0 ? 0 : suggestTimes.Max()):0.0} ms",
                $"{clock.ElapsedMilliseconds} ms",
            ];
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i] = [.. rows[i], cells[i]];
            }
        }

        foreach (string[] row in rows)
        {
            sb.AppendLine("| " + string.Join(" | ", row) + " |");
        }

        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Memory held by a loaded checker: about {checkerMegabytes:0} MB (target under 100 MB).");
        return sb.ToString();
    }

    private static double Median(List<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        values.Sort();
        return values[values.Count / 2];
    }
}
