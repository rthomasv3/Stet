using System.Globalization;
using System.Text;
using FictionChecker.Eval.Corpora;
using FictionChecker.Grammar;
using FictionChecker.Segmentation;
using FictionChecker.Spelling;

namespace FictionChecker.Eval;

/// <summary>
/// Writes, for every confusable word in the development books' clean text, how well each word of its group
/// fits the words around it. Used to choose how strong the evidence must be before a rule flags. The file
/// holds short pieces of the books, so it goes to the local (ignored) report folder. Test books are left out.
/// </summary>
public static class ConfusableDump
{
    /// <summary>Writes the table and returns how many rows it holds.</summary>
    public static int Run(WorkSet work, string outPath)
    {
        Lexicon lexicon = Lexicon.LoadEmbedded();
        GrammarEngine engine = GrammarEngine.CreateDefault(lexicon);
        ConfusableData data = ConfusableData.LoadEmbedded(lexicon);
        using var writer = new StreamWriter(outPath, false, new UTF8Encoding(false));
        writer.WriteLine("corpus\tbook\twritten\tmembers\tleft\tright\tcounts\tleftKnown\trightKnown\tcapital\tfirst\tspeech\tearlier\tcontext");
        int rows = 0;
        Span<int> left = stackalloc int[8];
        Span<int> right = stackalloc int[8];
        foreach (CleanSample sample in work.Clean.Where(s => s.Book.Split == Split.Dev))
        {
            QuoteStyle style = DialogueDetector.GuessStyle(sample.Paragraphs);
            List<DialogueResult> dialogue = DialogueDetector.DetectDocument(sample.Paragraphs, style);
            for (int p = 0; p < sample.Paragraphs.Count; p++)
            {
                string text = sample.Paragraphs[p];
                foreach (Sentence sentence in engine.Sentences(text, Tokenizer.Tokenize(text), dialogue[p], CustomWords.Empty))
                {
                    bool leading = true;
                    for (int i = 0; i < sentence.Count; i++)
                    {
                        if (!sentence.IsWord(i))
                        {
                            continue;
                        }

                        bool first = leading;
                        leading = false;
                        if (!data.TryFind(sentence.Lower(i), out int group, out int written))
                        {
                            continue;
                        }

                        string[] members = data.Members(group);
                        ConfusableScorer.Beside(sentence, i, group, data, left, right, out bool leftKnown, out bool rightKnown);
                        Token token = sentence.Token(i);
                        int from = Math.Max(0, token.Start - 40);
                        int to = Math.Min(text.Length, token.End + 40);
                        string context = (text[from..token.Start] + "[" + text.Substring(token.Start, token.Length) + "]" + text[token.End..to]).Replace('\t', ' ').Replace('\n', ' ');
                        writer.WriteLine(string.Join('\t',
                            sample.Corpus,
                            sample.Book.Id,
                            members[written],
                            string.Join(',', members),
                            Join(left[..members.Length]),
                            Join(right[..members.Length]),
                            string.Join(',', data.Counts(group).ToArray()),
                            leftKnown ? 1 : 0,
                            rightKnown ? 1 : 0,
                            sentence.IsCapitalized(i) ? 1 : 0,
                            first ? 1 : 0,
                            sentence.InDialogue(i) ? 1 : 0,
                            members.Length == 2 ? ConfusableScorer.EarlierSupport(sentence, i, group, written, 1 - written, data, 6) : 0,
                            context));
                        rows++;
                    }
                }
            }
        }

        return rows;
    }

    /// <summary>Prints, for each confusable word of a text, the evidence for the other words of its group.</summary>
    public static void Explain(string text)
    {
        Lexicon lexicon = Lexicon.LoadEmbedded();
        GrammarEngine engine = GrammarEngine.CreateDefault(lexicon);
        ConfusableData data = ConfusableData.LoadEmbedded(lexicon);
        foreach (Sentence sentence in engine.Sentences(text, Tokenizer.Tokenize(text), null, CustomWords.Empty))
        {
            for (int i = 0; i < sentence.Count; i++)
            {
                if (ConfusableScorer.TryScore(sentence, i, data, out ConfusableEvidence e))
                {
                    string[] members = data.Members(e.Group);
                    double unit = ConfusableData.Unit;
                    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{members[e.Written]}>{members[e.Best]} total={e.Total / unit:0.0} left={e.Left / unit:0.0}{(e.LeftKnown ? "w" : "t")} right={e.Right / unit:0.0}{(e.RightKnown ? "w" : "t")} prior={e.Prior / unit:0.0} flag={ConfusableSets.IsStrongEnough(members[e.Written], members[e.Best], e)} | {text}"));
                }
            }
        }
    }

    private static string Join(ReadOnlySpan<int> values)
    {
        var builder = new StringBuilder();
        foreach (int value in values)
        {
            if (builder.Length > 0)
            {
                builder.Append(',');
            }

            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
