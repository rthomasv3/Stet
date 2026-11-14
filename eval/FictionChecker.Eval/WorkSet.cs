using System.Security.Cryptography;
using System.Text;
using FictionChecker.Eval.Corpora;
using FictionChecker.Eval.Planting;
using FictionChecker.Eval.Text;

namespace FictionChecker.Eval;

/// <summary>A run of consecutive clean paragraphs from one book.</summary>
public sealed record CleanSample(string Corpus, Book Book, int FirstParagraph, IReadOnlyList<string> Paragraphs);

/// <summary>A clean paragraph and the same paragraph with one planted error.</summary>
public sealed record PlantedItem(string Corpus, Split Split, string BookId, string Dialect, int ParagraphIndex, string Category, string Clean, PlantedError Error);

/// <summary>Everything one evaluation run checks. The same settings and corpora always give the same set.</summary>
public sealed record WorkSet(IReadOnlyList<CleanSample> Clean, IReadOnlyList<PlantedItem> Planted)
{
    private const int MinPlantWords = 8;
    private const int MaxPlantWords = 300;

    /// <summary>Builds the work set for the given corpora.</summary>
    public static WorkSet Build(IReadOnlyList<Corpus> corpora, EvalSettings settings, IReadOnlyList<IErrorPlanter> planters)
    {
        var clean = new List<CleanSample>();
        var planted = new List<PlantedItem>();

        foreach (Corpus corpus in corpora)
        {
            var samples = corpus.Books.Select(book => Sample(corpus.Name, book, settings)).ToList();
            clean.AddRange(samples);

            foreach (Split split in new[] { Split.Dev, Split.Test })
            {
                var paragraphs = new List<(CleanSample Sample, int Offset)>();
                foreach (CleanSample sample in samples.Where(s => s.Book.Split == split))
                {
                    for (int i = 0; i < sample.Paragraphs.Count; i++)
                    {
                        int words = Words.Count(sample.Paragraphs[i]);
                        if (words is >= MinPlantWords and <= MaxPlantWords)
                        {
                            paragraphs.Add((sample, i));
                        }
                    }
                }

                foreach (IErrorPlanter planter in planters)
                {
                    var rng = new Rng(settings.Seed, corpus.Name, split.ToString(), planter.Category);
                    var order = new List<(CleanSample Sample, int Offset)>(paragraphs);
                    rng.Shuffle(order);
                    int made = 0;
                    foreach ((CleanSample sample, int offset) in order)
                    {
                        if (made >= settings.PlantedPerCategory)
                        {
                            break;
                        }

                        string text = sample.Paragraphs[offset];
                        PlantedError? error = planter.TryPlant(text, rng);
                        if (error is not null && error.Text != text)
                        {
                            planted.Add(new PlantedItem(corpus.Name, split, sample.Book.Id, sample.Book.Dialect, sample.FirstParagraph + offset, planter.Category, text, error));
                            made++;
                        }
                    }
                }
            }
        }

        return new WorkSet(clean, planted);
    }

    /// <summary>
    /// Every distinct paragraph that needs checking, with its dialect, for checkers that run outside this process.
    /// </summary>
    public IEnumerable<(string Text, string Dialect)> AllTexts() => Subset(null, int.MaxValue, int.MaxValue);

    /// <summary>
    /// A smaller set for slow checkers: the first <paramref name="cleanWordsPerBook"/> words of each book's clean
    /// run, and the first <paramref name="plantedPerCategory"/> planted errors of each category, corpus and split
    /// together with their clean paragraphs. Books are interleaved so a run that stops early is still balanced.
    /// </summary>
    public IEnumerable<(string Text, string Dialect)> Subset(string? corpus, int cleanWordsPerBook, int plantedPerCategory)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var perBook = new List<List<(string, string)>>();
        foreach (CleanSample sample in Clean.Where(s => corpus is null || s.Corpus == corpus))
        {
            var list = new List<(string, string)>();
            int words = 0;
            foreach (string text in sample.Paragraphs)
            {
                if (words >= cleanWordsPerBook)
                {
                    break;
                }

                list.Add((text, sample.Book.Dialect));
                words += Words.Count(text);
            }

            perBook.Add(list);
        }

        var counts = new Dictionary<(string, Split, string), int>();
        var plantedTexts = new List<(string, string)>();
        foreach (PlantedItem item in Planted.Where(p => corpus is null || p.Corpus == corpus))
        {
            var key = (item.Corpus, item.Split, item.Category);
            int count = counts.GetValueOrDefault(key);
            if (count < plantedPerCategory)
            {
                counts[key] = count + 1;
                plantedTexts.Add((item.Error.Text, item.Dialect));
                plantedTexts.Add((item.Clean, item.Dialect));
            }
        }

        // Round robin: a slice of clean text from each book, then a slice of planted errors, and so on.
        const int Slice = 8;
        int plantedAt = 0;
        for (int round = 0; ; round++)
        {
            bool any = false;
            foreach (List<(string Text, string Dialect)> list in perBook)
            {
                foreach ((string Text, string Dialect) entry in list.Skip(round * Slice).Take(Slice))
                {
                    any = true;
                    if (seen.Add(entry.Text))
                    {
                        yield return entry;
                    }
                }
            }

            int plantedSlice = Math.Max(Slice, perBook.Count * 2);
            for (int i = 0; i < plantedSlice && plantedAt < plantedTexts.Count; i++, plantedAt++)
            {
                any = true;
                if (seen.Add(plantedTexts[plantedAt].Item1))
                {
                    yield return plantedTexts[plantedAt];
                }
            }

            if (!any)
            {
                yield break;
            }
        }
    }

    /// <summary>A short, stable identifier for a paragraph's text. Lets labels and results refer to text without storing it.</summary>
    public static string HashOf(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)).AsSpan(0, 8));

    private static CleanSample Sample(string corpus, Book book, EvalSettings settings)
    {
        int count = book.Paragraphs.Count;
        if (count == 0)
        {
            return new CleanSample(corpus, book, 0, []);
        }

        // Start somewhere in the first 60% so the run rarely hits the end of the book.
        var rng = new Rng(settings.Seed, corpus, book.Id, "clean");
        int start = rng.Next(Math.Max(1, count * 6 / 10));
        var paragraphs = new List<string>();
        int words = 0;
        for (int i = start; i < count && words < settings.CleanWordsPerBook; i++)
        {
            paragraphs.Add(book.Paragraphs[i]);
            words += Words.Count(book.Paragraphs[i]);
        }

        return new CleanSample(corpus, book, start, paragraphs);
    }
}
