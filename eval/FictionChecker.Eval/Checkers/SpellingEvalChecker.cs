using FictionChecker.Eval.Corpora;
using FictionChecker.Segmentation;
using FictionChecker.Spelling;

namespace FictionChecker.Eval.Checkers;

/// <summary>
/// The library's spelling checker, wrapped for the harness.
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
    private readonly Dictionary<string, (SpellChecker Checker, SpellingOptions Options)> _prepared = new(StringComparer.Ordinal);
    private readonly bool _world;

    /// <summary>Creates the wrapper.</summary>
    public SpellingEvalChecker(IEnumerable<Corpus> corpora, bool world)
    {
        _world = world;
        Name = world ? "spelling-world" : "spelling";
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

    /// <summary>Custom words given to each book, for the record.</summary>
    public Dictionary<string, int> WorldWordCounts { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<IReadOnlyList<EvalIssue>?> CheckDocument(IReadOnlyList<string> paragraphs, DocumentInfo document)
    {
        (SpellChecker checker, SpellingOptions options) = Prepare(document);
        return checker.CheckDocument(paragraphs, options)
            .Select(result => (IReadOnlyList<EvalIssue>?)result.Issues
                .Select(issue => new EvalIssue(
                    issue.Start,
                    issue.Length,
                    issue.RuleId,
                    "Spelling",
                    issue.Suggestions.Select(s => new EvalSuggestion([new Edit(s.Start, s.Length, s.Text)])).ToList()))
                .ToList())
            .ToList();
    }

    private (SpellChecker Checker, SpellingOptions Options) Prepare(DocumentInfo document)
    {
        string key = document.Corpus + "/" + document.BookId;
        if (_prepared.TryGetValue(key, out (SpellChecker, SpellingOptions) ready))
        {
            return ready;
        }

        var checker = new SpellChecker(_lexicon, _lists);
        IReadOnlyList<string> text = _books.TryGetValue(key, out Book? book) ? book.Paragraphs : [];
        var options = new SpellingOptions
        {
            Dialect = document.Dialect == "GB" ? PickBritish(checker, text) : Dialect.American,
            QuoteStyle = DialogueDetector.GuessStyle(text),
        };

        if (_world && text.Count > 0)
        {
            List<string> world = WorldWords(checker, options, text);
            checker.SetWords("world", world);
            WorldWordCounts[key] = world.Count;
        }

        return _prepared[key] = (checker, options);
    }

    // British books use -ise or -ize; take whichever leaves fewer other-dialect flags.
    private static Dialect PickBritish(SpellChecker checker, IReadOnlyList<string> paragraphs)
    {
        var sample = paragraphs.Take(1500).ToList();
        int Count(Dialect dialect)
        {
            var options = new SpellingOptions { Dialect = dialect, MaxSuggestions = 0, QuoteStyle = QuoteStyle.Double };
            return sample.Sum(p => checker.CheckParagraph(p, options).Issues.Count(i => i.Kind == SpellingKind.OtherDialect));
        }

        return Count(Dialect.BritishIze) < Count(Dialect.British) ? Dialect.BritishIze : Dialect.British;
    }

    private static List<string> WorldWords(SpellChecker checker, SpellingOptions options, IReadOnlyList<string> paragraphs)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        SpellingOptions quick = options with { MaxSuggestions = 0 };
        foreach (SpellingResult result in checker.CheckDocument(paragraphs, quick))
        {
            foreach (SpellingIssue issue in result.Issues)
            {
                if (issue.Kind is SpellingKind.Misspelling or SpellingKind.UnknownName)
                {
                    string word = issue.Word.Replace('’', '\'');
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
