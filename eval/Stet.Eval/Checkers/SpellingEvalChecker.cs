using Stet.Eval.Corpora;
using Stet.Segmentation;
using Stet.Spelling;


namespace Stet.Eval.Checkers;

/// <summary>
/// The library's spelling checker, wrapped for the harness. It goes through the public API, so the numbers
/// are for what an app would get.
///
/// With <c>world</c> set, each book gets a stand-in for the word list its writer would have: every unknown
/// word the book uses twice or more is treated as a custom word, on the reasoning that a spelling repeated
/// exactly is intended (a name, a coined word, a character's way of speaking) and not a slip. Words used once
/// stay flagged. Without it, every name and coined word is flagged, as in a text with no word list at all.
/// </summary>
public sealed class SpellingEvalChecker : IEvalChecker
{
    private const int NameMinimum = 2;
    private const int WordMinimum = 2;

    private readonly Lexicon _lexicon = Lexicon.LoadEmbedded();
    private readonly BuiltInLists _lists = BuiltInLists.LoadEmbedded();
    private readonly Dictionary<string, Book> _books = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (Checker Checker, CheckerOptions Options)> _prepared = new(StringComparer.Ordinal);
    private readonly bool _world;
    private readonly bool _spellingOnly;
    private readonly bool _everyRule;
    private readonly Dictionary<string, List<string>> _worldWords = new(StringComparer.Ordinal);
    private readonly IReadOnlyDictionary<string, List<string>>? _extraWords;

    /// <summary>
    /// Creates the wrapper. With <paramref name="spellingOnly"/> the grammar rules are switched off; without
    /// it every rule runs at its default, which is what an app gets (reported as "stet").
    /// </summary>
    public SpellingEvalChecker(IEnumerable<Corpus> corpora, bool world, bool spellingOnly = true, bool everyRule = false, IReadOnlyDictionary<string, List<string>>? extraWords = null)
    {
        _extraWords = extraWords;
        _world = world;
        _spellingOnly = spellingOnly;
        _everyRule = everyRule;
        Name = everyRule ? "stet-all" : !spellingOnly ? "stet" : world ? "spelling-world" : "spelling";
        foreach (Corpus corpus in corpora)
        {
            foreach (Book book in corpus.Books)
            {
                _books[corpus.Name + "/" + book.Id] = book;
            }
        }
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// The stand-in word list this checker gives a book, so that another checker can be given the same one.
    /// Empty for a book that is not in the corpora.
    /// </summary>
    public IReadOnlyList<string> WordListFor(string corpus, string bookId, string dialect)
    {
        Prepare(new DocumentInfo(corpus, bookId, dialect));
        return _worldWords.GetValueOrDefault(corpus + "/" + bookId) ?? [];
    }

    /// <summary>Custom words given to each book, for the record.</summary>
    public Dictionary<string, int> WorldWordCounts { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<IReadOnlyList<EvalIssue>?> CheckDocument(IReadOnlyList<string> paragraphs, DocumentInfo document)
    {
        (Checker checker, CheckerOptions options) = Prepare(document);
        return checker.CheckDocument(paragraphs, options)
            .Select(result => (IReadOnlyList<EvalIssue>?)result.Issues
                .Select(issue => new EvalIssue(
                    issue.Start,
                    issue.Length,
                    issue.RuleId,
                    issue.Category.ToString(),
                    issue.Suggestions.Select(s => new EvalSuggestion([new Edit(s.Start, s.Length, s.Text)])).ToList()))
                .ToList())
            .ToList();
    }

    private (Checker Checker, CheckerOptions Options) Prepare(DocumentInfo document)
    {
        string key = document.Corpus + "/" + document.BookId;
        if (_prepared.TryGetValue(key, out (Checker, CheckerOptions) ready))
        {
            return ready;
        }

        var checker = new Checker(_lexicon, _lists);
        IReadOnlyList<string> text = _books.TryGetValue(key, out Book? book) ? book.Paragraphs : [];
        var options = new CheckerOptions
        {
            Dialect = document.Dialect == "GB" ? PickBritish(checker, text) : Dialect.American,
            QuoteStyle = DialogueDetector.GuessStyle(text),
        };
        if (_everyRule)
        {
            // Rules that are off by default are switched on too, so that each one is measured.
            options = options with { RuleOverrides = checker.Rules.ToDictionary(r => r.Id, _ => true, StringComparer.Ordinal) };
        }

        if (_spellingOnly)
        {
            options = options with
            {
                CategoryOverrides = Enum.GetValues<RuleCategory>().Where(c => c != RuleCategory.Spelling).ToDictionary(c => c, _ => false),
            };
        }

        if (_world && text.Count > 0)
        {
            List<string> world = WorldWords(checker, options, text);
            if (_extraWords is not null && _extraWords.TryGetValue(key, out List<string>? extra))
            {
                // Words the book's list should hold that Stet did not need: see the "names" command.
                world.AddRange(extra.Except(world, StringComparer.Ordinal));
            }

            checker.SetWords("world", world);
            WorldWordCounts[key] = world.Count;
            _worldWords[key] = world;
        }

        return _prepared[key] = (checker, options);
    }

    // British books use -ise or -ize; take whichever leaves fewer other-dialect flags.
    private static Dialect PickBritish(Checker checker, IReadOnlyList<string> paragraphs)
    {
        var sample = paragraphs.Take(1500).ToList();
        int Count(Dialect dialect)
        {
            var context = new ParagraphContext { Options = new CheckerOptions { Dialect = dialect, MaxSuggestions = 0, QuoteStyle = QuoteStyle.Double } };
            return sample.Sum(p => checker.CheckParagraph(p, context).Issues.Count(i => i.Kind == IssueKind.OtherDialectSpelling));
        }

        return Count(Dialect.BritishIze) < Count(Dialect.British) ? Dialect.BritishIze : Dialect.British;
    }

    private static List<string> WorldWords(Checker checker, CheckerOptions options, IReadOnlyList<string> paragraphs)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        CheckerOptions quick = options with { MaxSuggestions = 0 };
        foreach (ParagraphResult result in checker.CheckDocument(paragraphs, quick))
        {
            foreach (Issue issue in result.Issues)
            {
                if (issue.Kind is IssueKind.Misspelling or IssueKind.UnknownName)
                {
                    string word = issue.Text.Replace('’', '\'');
                    if (word.EndsWith("'s", StringComparison.Ordinal))
                    {
                        word = word[..^2];
                    }

                    counts[word] = counts.GetValueOrDefault(word) + 1;
                }
            }
        }

        // A name counts in any of its capitalizations; it is added in the form seen most.
        var world = new List<string>();
        foreach (IGrouping<string, KeyValuePair<string, int>> group in counts.GroupBy(kv => kv.Key.ToLowerInvariant(), StringComparer.Ordinal))
        {
            int total = group.Sum(kv => kv.Value);
            KeyValuePair<string, int> lowerForm = group.FirstOrDefault(kv => kv.Key == group.Key);
            bool mostlyCapitalized = lowerForm.Value * 2 < total;
            if (mostlyCapitalized && total >= NameMinimum)
            {
                // Prefer the Capitalized spelling over an ALL CAPS one.
                world.Add(group.Where(kv => kv.Key != group.Key).OrderByDescending(kv => kv.Key.Any(char.IsLower)).ThenByDescending(kv => kv.Value).First().Key);
            }
            else if (!mostlyCapitalized && total >= WordMinimum)
            {
                world.Add(group.Key);
            }
        }

        return world;
    }
}
