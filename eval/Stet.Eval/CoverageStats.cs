using System.Globalization;
using System.Text;
using Stet.Eval.Corpora;
using Stet.Segmentation;
using Stet.Spelling;

namespace Stet.Eval;

/// <summary>
/// Measures how much of the books' vocabulary the spelling data knows, before the spelling checker exists.
/// The lookup here is a plain preview of the rules the checker will apply (case, possessives, hyphens).
/// </summary>
public static class CoverageStats
{
    /// <summary>Runs the measurement and returns a numbers-only Markdown report.</summary>
    /// <param name="corpora">The corpora to read (whole books).</param>
    /// <param name="unknownsPath">Where to write the most common unknown words of the dev books, or null. Contains words from the books.</param>
    public static string Run(IReadOnlyList<Corpus> corpora, string? unknownsPath)
    {
        Lexicon lexicon = Lexicon.LoadEmbedded();
        BuiltInLists lists = BuiltInLists.LoadEmbedded();
        var sb = new StringBuilder();
        sb.AppendLine("# Spelling data coverage");
        sb.AppendLine();
        sb.AppendLine("Unknown words per 1,000 words on whole books, using a plain preview of the checker's lookup rules.");
        sb.AppendLine("\"Names\" are unknown words written with a capital away from a sentence start; \"other\" is everything else unknown.");
        sb.AppendLine("The first column shows the same total with only SCOWL's normal size and none of this project's lists.");
        sb.AppendLine();
        sb.AppendLine("| Corpus | Split | Words | Normal size only | Unknown | Names | Other | Other-dialect spellings | Known only as informal or archaic |");
        sb.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|");

        var unknownWords = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Corpus corpus in corpora)
        {
            foreach (Split split in new[] { Split.Dev, Split.Test })
            {
                long words = 0, unknown = 0, names = 0, otherDialect = 0, listOnly = 0, unknownNormal = 0;
                foreach (Book book in corpus.Books.Where(b => b.Split == split))
                {
                    bool british = book.Dialect == "GB";
                    foreach (string text in book.Paragraphs)
                    {
                        List<Token> tokens = Tokenizer.Tokenize(text);
                        for (int i = 0; i < tokens.Count; i++)
                        {
                            Token token = tokens[i];
                            if (token.Kind != TokenKind.Word)
                            {
                                continue;
                            }

                            words++;
                            string word = text.Substring(token.Start, token.Length).Replace('’', '\'');
                            Verdict verdict = Judge(word, token, lexicon, lists, british, fullData: true);
                            if (Judge(word, token, lexicon, lists, british, fullData: false) == Verdict.Unknown)
                            {
                                unknownNormal++;
                            }

                            switch (verdict)
                            {
                                case Verdict.OtherDialect:
                                    otherDialect++;
                                    break;
                                case Verdict.ListOnly:
                                    listOnly++;
                                    break;
                                case Verdict.Unknown:
                                    unknown++;
                                    bool sentenceStart = i == 0 || (tokens[i - 1].Kind == TokenKind.Punctuation && text[tokens[i - 1].Start] is '.' or '!' or '?' or '"' or '“' or '‘' or '\'');
                                    if (char.IsUpper(word[0]) && !sentenceStart)
                                    {
                                        names++;
                                    }
                                    else if (split == Split.Dev)
                                    {
                                        string key = corpus.Name + "\t" + word.ToLowerInvariant();
                                        unknownWords[key] = unknownWords.GetValueOrDefault(key) + 1;
                                    }

                                    break;
                            }
                        }
                    }
                }

                sb.AppendLine(CultureInfo.InvariantCulture, $"| {corpus.Name} | {(split == Split.Dev ? "dev" : "test")} | {words:N0} | {Rate(unknownNormal, words)} | {Rate(unknown, words)} | {Rate(names, words)} | {Rate(unknown - names, words)} | {Rate(otherDialect, words)} | {Rate(listOnly, words)} |");
            }
        }

        if (unknownsPath is not null)
        {
            File.WriteAllLines(unknownsPath, unknownWords.OrderByDescending(kv => kv.Value).Take(4000).Select(kv => $"{kv.Value}\t{kv.Key}"));
        }

        return sb.ToString();
    }

    private static Verdict Judge(string word, Token token, Lexicon lexicon, BuiltInLists lists, bool british, bool fullData)
    {
        Verdict best = Verdict.Unknown;
        foreach (string part in word.Split('-'))
        {
            if (part.Length == 0)
            {
                continue;
            }

            Verdict verdict = JudgePart(part, token, lexicon, lists, british, fullData);
            if (verdict == Verdict.Unknown)
            {
                // The whole hyphenated word may be listed even when a part is not.
                return part.Length != word.Length && Lookup(word, lexicon, british, fullData) == Verdict.Known ? Verdict.Known : Verdict.Unknown;
            }

            if (verdict > best || best == Verdict.Unknown)
            {
                best = verdict;
            }
        }

        return best;
    }

    private static Verdict JudgePart(string word, Token token, Lexicon lexicon, BuiltInLists lists, bool british, bool fullData)
    {
        if (word.EndsWith("'s", StringComparison.Ordinal) && word.Length > 2)
        {
            word = word[..^2];
        }

        Verdict result = Verdict.Unknown;
        foreach (string form in Forms(word))
        {
            Verdict verdict = Lookup(form, lexicon, british, fullData);
            if (verdict == Verdict.Known)
            {
                return Verdict.Known;
            }

            if (verdict == Verdict.OtherDialect)
            {
                result = Verdict.OtherDialect;
            }
        }

        if (result != Verdict.Unknown)
        {
            return result;
        }

        string lower = word.ToLowerInvariant();
        if (fullData && (lists.Informal.Contains(lower) || lists.Archaic.Contains(lower) || lists.Informal.Contains("'" + lower)))
        {
            return Verdict.ListOnly;
        }

        // runnin', nothin': a dropped g when the full word is known.
        if (fullData && token.Has(TokenFlags.TrailingApostrophe) && lower.EndsWith("in", StringComparison.Ordinal)
            && Lookup(lower + "g", lexicon, british, fullData) == Verdict.Known)
        {
            return Verdict.ListOnly;
        }

        return Verdict.Unknown;
    }

    private static IEnumerable<string> Forms(string word)
    {
        yield return word;
        if (char.IsUpper(word[0]))
        {
            yield return word.ToLowerInvariant();
            if (word.Length > 1 && char.IsUpper(word[1]))
            {
                yield return word[0] + word[1..].ToLowerInvariant();
            }
        }
    }

    private static Verdict Lookup(string word, Lexicon lexicon, bool british, bool fullData)
    {
        if (!lexicon.TryGet(word, out WordInfo info) || (!fullData && (info.IsRare || info.IsAdded)))
        {
            return Verdict.Unknown;
        }

        bool inDialect = british ? info.IsIn(Dialect.British) || info.IsIn(Dialect.BritishIze) : info.IsIn(Dialect.American);
        return inDialect ? Verdict.Known : Verdict.OtherDialect;
    }

    private static string Rate(long count, long words) =>
        words == 0 ? "0" : (count * 1000.0 / words).ToString("0.00", CultureInfo.InvariantCulture);

    private enum Verdict
    {
        Unknown,
        OtherDialect,
        ListOnly,
        Known,
    }
}
