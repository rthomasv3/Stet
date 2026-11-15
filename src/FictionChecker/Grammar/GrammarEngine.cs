using FictionChecker.Grammar.Rules;
using FictionChecker.Segmentation;
using FictionChecker.Spelling;

namespace FictionChecker.Grammar;

/// <summary>Quote marks for messages.</summary>
internal static class CharClassQuote
{
    public static readonly string Open = CharClass.LeftDoubleQuote.ToString();
    public static readonly string Close = CharClass.RightDoubleQuote.ToString();
}

/// <summary>
/// Runs the grammar rules over a paragraph: splits it into sentences, tags each sentence's words and gives
/// the sentence to every rule that is switched on.
/// </summary>
internal sealed class GrammarEngine
{
    private readonly WordData _data;
    private readonly FunctionWords _functionWords;
    private readonly Tagger _tagger;

    public GrammarEngine(WordData data, FunctionWords functionWords, IReadOnlyList<GrammarRule>? rules = null)
    {
        _data = data;
        _functionWords = functionWords;
        _tagger = new Tagger(data);
        Rules = rules ?? [new RepeatedWordRule()];
    }

    /// <summary>Creates an engine from the data embedded in the assembly.</summary>
    public static GrammarEngine CreateDefault(Lexicon lexicon) => new(WordData.LoadEmbedded(lexicon), FunctionWords.LoadEmbedded());

    /// <summary>Every rule, in a fixed order.</summary>
    public IReadOnlyList<GrammarRule> Rules { get; }

    /// <summary>The tagger, for tests and tools.</summary>
    public Tagger Tagger => _tagger;

    /// <summary>Splits a paragraph into tagged sentences.</summary>
    public IEnumerable<Sentence> Sentences(string text, List<Token> tokens, DialogueResult? dialogue, CustomWords custom)
    {
        List<TextSpan> spans = SentenceSplitter.Split(text, tokens);
        int at = 0;
        foreach (TextSpan span in spans)
        {
            int from = at;
            while (at < tokens.Count && tokens[at].Start < span.End)
            {
                at++;
            }

            if (at == from)
            {
                continue;
            }

            Token[] slice = tokens.GetRange(from, at - from).ToArray();
            var types = new WordType[slice.Length];
            int[] words = new int[slice.Length];
            _tagger.Tag(text, slice, custom, types, words);
            yield return new Sentence(text, slice, types, words, _data, _functionWords, dialogue);
        }
    }

    /// <summary>
    /// Runs the enabled rules and adds an issue for each finding. <paramref name="enabled"/> holds one flag
    /// per rule, in the order of <see cref="Rules"/>.
    /// </summary>
    public void Check(string text, List<Token> tokens, DialogueResult? dialogue, CustomWords custom, bool[] enabled, DialogueStrictness strictness, List<Issue> issues)
    {
        var findings = new List<Finding>();
        foreach (Sentence sentence in Sentences(text, tokens, dialogue, custom))
        {
            for (int r = 0; r < Rules.Count; r++)
            {
                if (!enabled[r])
                {
                    continue;
                }

                GrammarRule rule = Rules[r];
                findings.Clear();
                rule.Check(sentence, findings);
                foreach (Finding finding in findings)
                {
                    // Dialogue strictness: nothing in speech when off; formal rules skip speech when lenient.
                    bool inSpeech = dialogue is not null && dialogue.IsDialogue(finding.Start);
                    if (inSpeech && (strictness == DialogueStrictness.Off || (strictness == DialogueStrictness.Lenient && rule.Info.IsFormal)))
                    {
                        continue;
                    }

                    issues.Add(new Issue(finding.Start, finding.Length, rule.Info, IssueKind.Grammar, text.Substring(finding.Start, finding.Length), finding.Message, finding.Explanation, finding.Suggestions));
                }
            }
        }
    }
}
