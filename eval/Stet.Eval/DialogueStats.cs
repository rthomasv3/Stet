using System.Diagnostics;
using System.Globalization;
using System.Text;
using Stet.Eval.Corpora;
using Stet.Segmentation;

namespace Stet.Eval;

/// <summary>
/// Measures the library's text splitting on whole books: how much text is detected as speech, how many
/// quote marks could not be paired, and how fast it runs.
/// </summary>
public static class DialogueStats
{
    /// <summary>Runs the measurement and returns a numbers-only Markdown report.</summary>
    /// <param name="corpora">The corpora to read.</param>
    /// <param name="samplePath">Where to write a sample of detected speech for review (contains book text), or null.</param>
    public static string Run(IReadOnlyList<Corpus> corpora, string? samplePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Text splitting and dialogue detection");
        sb.AppendLine();
        sb.AppendLine("Whole books, not the evaluation sample. The quote style is guessed per book from how its paragraphs open.");
        sb.AppendLine();
        sb.AppendLine("| Corpus | Split | Books | Single-quote books | Words | In dialogue | Paragraphs | Unmatched marks per 1,000 paragraphs | Paragraphs continuing speech | Words per sentence | Worst book: unmatched per 1,000 |");
        sb.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        var sample = new StringBuilder("corpus\tbook\tparagraph\tkind\ttext\n");
        var perBook = new StringBuilder("corpus\tbook\tstyle\tparagraphs\tunmatched\tper1000\n");
        var clock = new Stopwatch();
        long totalWords = 0;

        foreach (Corpus corpus in corpora)
        {
            foreach (Split split in new[] { Split.Dev, Split.Test })
            {
                var books = corpus.Books.Where(b => b.Split == split).ToList();
                long words = 0;
                long dialogueWords = 0;
                long paragraphs = 0;
                long unmatched = 0;
                long continuing = 0;
                long sentences = 0;
                int singleStyle = 0;
                double worst = 0;

                foreach (Book book in books)
                {
                    clock.Start();
                    QuoteStyle style = DialogueDetector.GuessStyle(book.Paragraphs);
                    List<DialogueResult> results = DialogueDetector.DetectDocument(book.Paragraphs, style);
                    long bookUnmatched = 0;
                    for (int p = 0; p < book.Paragraphs.Count; p++)
                    {
                        string text = book.Paragraphs[p];
                        List<Token> tokens = Tokenizer.Tokenize(text);
                        sentences += SentenceSplitter.Split(text, tokens).Count;
                        DialogueResult result = results[p];
                        foreach (Token token in tokens)
                        {
                            if (token.Kind == TokenKind.Word)
                            {
                                words++;
                                if (result.IsDialogue(token.Start))
                                {
                                    dialogueWords++;
                                }
                            }
                        }

                        bookUnmatched += result.UnmatchedQuotes;
                        continuing += result.EndsInDialogue ? 1 : 0;
                    }

                    clock.Stop();
                    paragraphs += book.Paragraphs.Count;
                    unmatched += bookUnmatched;
                    singleStyle += style == QuoteStyle.Single ? 1 : 0;
                    worst = Math.Max(worst, book.Paragraphs.Count == 0 ? 0 : bookUnmatched * 1000.0 / book.Paragraphs.Count);
                    perBook.Append(CultureInfo.InvariantCulture, $"{corpus.Name}\t{book.Id}\t{style}\t{book.Paragraphs.Count}\t{bookUnmatched}\t{(book.Paragraphs.Count == 0 ? 0 : bookUnmatched * 1000.0 / book.Paragraphs.Count):0.0}\n");
                    AddSample(sample, corpus.Name, book, results);
                }

                totalWords += words;
                sb.AppendLine(CultureInfo.InvariantCulture, $"| {corpus.Name} | {(split == Split.Dev ? "dev" : "test")} | {books.Count} | {singleStyle} | {words:N0} | {Percent(dialogueWords, words)} | {paragraphs:N0} | {Rate(unmatched, paragraphs)} | {Percent(continuing, paragraphs)} | {(sentences == 0 ? 0 : (double)words / sentences):0.0} | {worst:0.0} |");
            }
        }

        sb.AppendLine();
        double seconds = Math.Max(clock.Elapsed.TotalSeconds, 0.001);
        Console.WriteLine($"Split {totalWords:N0} words in {seconds:0.00} s ({totalWords / seconds / 1000:N0} thousand words per second).");
        if (samplePath is not null)
        {
            File.WriteAllText(samplePath, sample.ToString());
            File.WriteAllText(Path.ChangeExtension(samplePath, ".books.tsv"), perBook.ToString());
        }

        return sb.ToString();
    }

    // A fixed handful of paragraphs per book: some with speech, and every one with an unmatched mark.
    private static void AddSample(StringBuilder sample, string corpus, Book book, List<DialogueResult> results)
    {
        int speech = 0;
        int problems = 0;
        for (int p = 0; p < book.Paragraphs.Count; p++)
        {
            DialogueResult result = results[p];
            string text = book.Paragraphs[p];
            bool problem = result.UnmatchedQuotes > 0;
            bool pick = problem ? problems++ < 6 : result.Spans.Count > 0 && p % 97 == 0 && speech++ < 6;
            if (!pick)
            {
                continue;
            }

            var marked = new StringBuilder();
            int at = 0;
            foreach (TextSpan span in result.Spans)
            {
                marked.Append(text, at, span.Start - at).Append("[[").Append(text, span.Start, span.Length).Append("]]");
                at = span.End;
            }

            marked.Append(text, at, text.Length - at);
            string shown = marked.Length > 400 ? marked.ToString(0, 400) + "…" : marked.ToString();
            sample.Append(CultureInfo.InvariantCulture, $"{corpus}\t{book.Id}\t{p}\t{(problem ? "unmatched" : "speech")}\t{shown}\n");
        }
    }

    private static string Percent(long part, long whole) =>
        whole == 0 ? "0%" : (part * 100.0 / whole).ToString("0.0", CultureInfo.InvariantCulture) + "%";

    private static string Rate(long count, long paragraphs) =>
        paragraphs == 0 ? "0" : (count * 1000.0 / paragraphs).ToString("0.0", CultureInfo.InvariantCulture);
}
