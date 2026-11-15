using FictionChecker.Eval.Checkers;
using FictionChecker.Eval.Corpora;
using FictionChecker.Eval.Text;

namespace FictionChecker.Eval.Scoring;

/// <summary>Runs a checker over a work set and turns its issues into numbers.</summary>
public sealed class Scorer
{
    /// <summary>Name of the group that adds every corpus together.</summary>
    public const string AllCorpora = "all";

    private readonly Labels _labels;
    private readonly EvalSettings _settings;

    /// <summary>Creates a scorer.</summary>
    public Scorer(EvalSettings settings, Labels labels)
    {
        _settings = settings;
        _labels = labels;
    }

    /// <summary>Flags on clean text sampled for review. Filled by <see cref="Run"/>.</summary>
    public List<ReviewFlag> ReviewFlags { get; } = [];

    /// <summary>
    /// How often each piece of text was flagged on the dev books, by corpus and rule. Contains words from the
    /// books: local reports only. Test books are left out so they are never looked at.
    /// </summary>
    public Dictionary<(string Corpus, string RuleId, string Text), int> FlaggedText { get; } = [];

    /// <summary>
    /// Planted errors on dev books that were caught but whose first suggestion was not the original text:
    /// category, kind, what was planted, what was wanted, what was offered. Local reports only.
    /// </summary>
    public List<string> WrongFirstFix { get; } = [];

    /// <summary>Clean-text words and flags per book, to see how uneven the books are. Local reports only.</summary>
    public Dictionary<(string Corpus, string BookId, string Split), (long Words, int Flags)> PerBook { get; } = [];

    /// <summary>Planted errors that were not caught, sampled for review. Filled by <see cref="Run"/>.</summary>
    public List<MissedPlant> Missed { get; } = [];

    /// <summary>Checks everything in the work set and returns the report.</summary>
    public EvalReport Run(IEvalChecker checker, WorkSet work, string libraryVersion)
    {
        var groups = new Dictionary<(string Corpus, Split Split), Tally>();
        var cleanIssues = new Dictionary<string, IReadOnlyList<EvalIssue>?>(StringComparer.Ordinal);
        var perRuleSeen = new Dictionary<(string Corpus, string RuleId), int>();

        foreach (CleanSample sample in work.Clean)
        {
            Tally tally = GetTally(groups, sample.Corpus, sample.Book.Split);
            tally.Books++;
            long bookWords = 0;
            int bookFlags = 0;
            IReadOnlyList<IReadOnlyList<EvalIssue>?> results = checker.CheckDocument(sample.Paragraphs, new DocumentInfo(sample.Corpus, sample.Book.Id, sample.Book.Dialect));
            for (int i = 0; i < sample.Paragraphs.Count; i++)
            {
                string text = sample.Paragraphs[i];
                IReadOnlyList<EvalIssue>? issues = i < results.Count ? results[i] : null;
                cleanIssues[text] = issues;
                tally.Paragraphs++;
                if (issues is null)
                {
                    tally.Unchecked++;
                    continue;
                }

                int paragraphWords = Words.Count(text);
                tally.Words += paragraphWords;
                bookWords += paragraphWords;
                string hash = WorkSet.HashOf(text);
                foreach (EvalIssue issue in issues)
                {
                    if (_labels.IsRealError(hash, issue.Start, issue.Length, issue.RuleId))
                    {
                        tally.RealErrors++;
                        continue;
                    }

                    tally.Flags++;
                    bookFlags++;
                    tally.CountRule(issue);
                    if (sample.Book.Split == Split.Dev && issue.Start >= 0 && issue.Start + issue.Length <= text.Length)
                    {
                        var flagged = (sample.Corpus, issue.RuleId, text.Substring(issue.Start, issue.Length));
                        FlaggedText[flagged] = FlaggedText.GetValueOrDefault(flagged) + 1;
                    }

                    var key = (sample.Corpus, issue.RuleId);
                    int seen = perRuleSeen.GetValueOrDefault(key);
                    perRuleSeen[key] = seen + 1;
                    // Only dev books are written out for review; test books are measured, never read.
                    if (seen < _settings.ReviewFlagsPerRule && sample.Book.Split == Split.Dev)
                    {
                        ReviewFlags.Add(new ReviewFlag(sample.Corpus, sample.Book.Id, sample.FirstParagraph + i, hash, issue.Start, issue.Length, issue.RuleId, Snippet(text, issue.Start, issue.Length)));
                    }
                }
            }

            PerBook[(sample.Corpus, sample.Book.Id, SplitName(sample.Book.Split))] = (bookWords, bookFlags);
        }

        // (per-book totals are filled in the loop above)
        var missedSeen = new Dictionary<(string Corpus, string Category), int>();
        foreach (PlantedItem item in work.Planted)
        {
            Tally tally = GetTally(groups, item.Corpus, item.Split);
            PlantedTally planted = tally.GetPlanted(item.Category);
            planted.Planted++;
            IReadOnlyList<EvalIssue>? issues = checker.CheckDocument([item.Error.Text], new DocumentInfo(item.Corpus, item.BookId, item.Dialect))[0];
            cleanIssues.TryGetValue(item.Clean, out IReadOnlyList<EvalIssue>? before);
            if (issues is null || before is null)
            {
                continue;
            }

            planted.Checked++;
            PlantOutcome outcome = Judge(item, issues, before);
            if (outcome.Caught)
            {
                planted.Caught++;
                planted.FirstFixRight += outcome.FirstFixRight ? 1 : 0;
                if (!outcome.FirstFixRight && item.Split == Split.Dev)
                {
                    EvalIssue hit = issues.First(issue => issue.Overlaps(item.Error.Start, item.Error.Length));
                    string plantedText = item.Error.Text.Substring(item.Error.Start, item.Error.Length);
                    string wanted = item.Clean.Substring(item.Error.OriginalStart, item.Error.OriginalLength);
                    string offered = string.Join(", ", hit.Suggestions.Take(5).Select(s => s.Edits.Count == 1 ? s.Edits[0].Text : "(multi)"));
                    WrongFirstFix.Add($"{item.Category}\t{item.Error.Kind}\t{plantedText}\t{wanted}\t{offered}");
                }
                planted.FixInTop5 += outcome.FixInTop5 ? 1 : 0;
            }
            else
            {
                var key = (item.Corpus, item.Category);
                int seen = missedSeen.GetValueOrDefault(key);
                missedSeen[key] = seen + 1;
                if (seen < _settings.ReviewFlagsPerRule)
                {
                    Missed.Add(new MissedPlant(item.Corpus, item.BookId, item.ParagraphIndex, item.Category, item.Error.Kind, Snippet(item.Error.Text, item.Error.Start, item.Error.Length)));
                }
            }
        }

        var results2 = new List<GroupResult>();
        foreach (string corpus in groups.Keys.Select(k => k.Corpus).Distinct().Order(StringComparer.Ordinal))
        {
            foreach (Split split in new[] { Split.Dev, Split.Test })
            {
                if (groups.TryGetValue((corpus, split), out Tally? tally))
                {
                    results2.Add(tally.ToResult(corpus, SplitName(split)));
                }
            }
        }

        foreach (Split split in new[] { Split.Dev, Split.Test })
        {
            var total = new Tally();
            foreach (((string _, Split s), Tally tally) in groups)
            {
                if (s == split)
                {
                    total.Add(tally);
                }
            }

            results2.Add(total.ToResult(AllCorpora, SplitName(split)));
        }

        return new EvalReport(checker.Name, libraryVersion, _settings, results2);
    }

    /// <summary>
    /// Decides whether a planted error was caught: a flag must touch the planted region, and must not be a flag
    /// the clean paragraph already had there under the same rule.
    /// </summary>
    public static PlantOutcome Judge(PlantedItem item, IReadOnlyList<EvalIssue> issues, IReadOnlyList<EvalIssue> cleanIssues)
    {
        var catching = issues
            .Where(issue => issue.Overlaps(item.Error.Start, item.Error.Length))
            .Where(issue => !cleanIssues.Any(c => c.RuleId == issue.RuleId && c.Overlaps(item.Error.OriginalStart, item.Error.OriginalLength)))
            .ToList();
        if (catching.Count == 0)
        {
            return new PlantOutcome(false, false, false);
        }

        var suggestions = catching.SelectMany(issue => issue.Suggestions).Take(5).ToList();
        bool first = suggestions.Count > 0 && suggestions[0].ApplyTo(item.Error.Text) == item.Clean;
        bool top5 = suggestions.Any(s => s.ApplyTo(item.Error.Text) == item.Clean);
        return new PlantOutcome(true, first, top5);
    }

    private static string SplitName(Split split) => split == Split.Dev ? "dev" : "test";

    private static Tally GetTally(Dictionary<(string, Split), Tally> groups, string corpus, Split split)
    {
        if (!groups.TryGetValue((corpus, split), out Tally? tally))
        {
            groups[(corpus, split)] = tally = new Tally();
        }

        return tally;
    }

    private static string Snippet(string text, int start, int length)
    {
        start = Math.Clamp(start, 0, text.Length);
        int end = Math.Clamp(start + length, start, text.Length);
        int from = Math.Max(0, start - 60);
        int to = Math.Min(text.Length, end + 60);
        return $"{text[from..start]}[[{text[start..end]}]]{text[end..to]}";
    }

    /// <summary>The outcome for one planted error.</summary>
    public readonly record struct PlantOutcome(bool Caught, bool FirstFixRight, bool FixInTop5);

    private sealed class PlantedTally
    {
        public int Planted;
        public int Checked;
        public int Caught;
        public int FirstFixRight;
        public int FixInTop5;
    }

    private sealed class Tally
    {
        private readonly Dictionary<string, (string Category, int Flags)> _rules = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PlantedTally> _planted = new(StringComparer.Ordinal);
        private readonly List<string> _plantedOrder = [];

        public int Books;
        public int Paragraphs;
        public int Unchecked;
        public long Words;
        public int Flags;
        public int RealErrors;

        public void CountRule(EvalIssue issue)
        {
            (string category, int flags) = _rules.GetValueOrDefault(issue.RuleId, (issue.Category, 0));
            _rules[issue.RuleId] = (category, flags + 1);
        }

        public PlantedTally GetPlanted(string category)
        {
            if (!_planted.TryGetValue(category, out PlantedTally? tally))
            {
                _planted[category] = tally = new PlantedTally();
                _plantedOrder.Add(category);
            }

            return tally;
        }

        public void Add(Tally other)
        {
            Books += other.Books;
            Paragraphs += other.Paragraphs;
            Unchecked += other.Unchecked;
            Words += other.Words;
            Flags += other.Flags;
            RealErrors += other.RealErrors;
            foreach ((string rule, (string category, int flags)) in other._rules)
            {
                (string _, int mine) = _rules.GetValueOrDefault(rule, (category, 0));
                _rules[rule] = (category, mine + flags);
            }

            foreach (string category in other._plantedOrder)
            {
                PlantedTally mine = GetPlanted(category);
                PlantedTally theirs = other._planted[category];
                mine.Planted += theirs.Planted;
                mine.Checked += theirs.Checked;
                mine.Caught += theirs.Caught;
                mine.FirstFixRight += theirs.FirstFixRight;
                mine.FixInTop5 += theirs.FixInTop5;
            }
        }

        public GroupResult ToResult(string corpus, string split)
        {
            var rules = _rules
                .OrderByDescending(r => r.Value.Flags)
                .ThenBy(r => r.Key, StringComparer.Ordinal)
                .Select(r => new RuleResult(r.Key, r.Value.Category, r.Value.Flags, Words == 0 ? 0 : Math.Round(r.Value.Flags * 1000.0 / Words, 4)))
                .ToList();
            var planted = _plantedOrder
                .Select(c => new PlantedResult(c, _planted[c].Planted, _planted[c].Checked, _planted[c].Caught, _planted[c].FirstFixRight, _planted[c].FixInTop5))
                .ToList();
            return new GroupResult(corpus, split, Books, Paragraphs, Unchecked, Words, Flags, RealErrors, rules, planted);
        }
    }
}
