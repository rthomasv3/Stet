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
        var confusables = new Lazy<ConfusableData>(() => ConfusableData.LoadEmbedded(data.Lexicon));
        Rules = rules ??
        [
            new RepeatedWordRule(),
            new ArticleRule(),
            new CouldOfRule(),
            new DoubledSpaceRule(),
            new CommaSpacingRule(),
            new SentenceCapitalRule(),
            new LowercaseIRule(),
            new PronounVerbRule(),
            new DialectVerbFormRule(),
            new VerbFormRule(),
            new FullStopBeforeTagRule(),
            new CapitalizedTagRule(),
            new MissingCommaBeforeTagRule(),
            new CommaOutsideQuoteRule(),
            .. ConfusableSets.Families.Select(family => new ConfusableWordRule(family, () => confusables.Value)),
        ];
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
    public void Check(string text, List<Token> tokens, DialogueResult? dialogue, CustomWords custom, bool[] enabled, DialogueStrictness strictness, QuoteStyle quoteStyle, List<Issue> issues)
    {
        var findings = new List<Finding>();
        var paragraph = new Paragraph(text, tokens, dialogue, quoteStyle, custom);
        for (int r = 0; r < Rules.Count; r++)
        {
            if (enabled[r])
            {
                findings.Clear();
                Rules[r].CheckParagraph(paragraph, findings);
                Report(Rules[r], findings, text, dialogue, strictness, issues);
            }
        }

        foreach (Sentence sentence in Sentences(text, tokens, dialogue, custom))
        {
            for (int r = 0; r < Rules.Count; r++)
            {
                if (enabled[r])
                {
                    findings.Clear();
                    Rules[r].Check(sentence, findings);
                    Report(Rules[r], findings, text, dialogue, strictness, issues);
                }
            }
        }
    }

    private static void Report(GrammarRule rule, List<Finding> findings, string text, DialogueResult? dialogue, DialogueStrictness strictness, List<Issue> issues)
    {
        foreach (Finding finding in findings)
        {
            // Dialogue strictness: nothing in speech when off; formal rules skip speech when lenient. Rules
            // about the punctuation around speech are not about what is said, so they always apply.
            bool inSpeech = rule.Info.Category != RuleCategory.DialoguePunctuation && dialogue is not null && dialogue.IsDialogue(finding.Start);
            if (inSpeech && (strictness == DialogueStrictness.Off || (strictness == DialogueStrictness.Lenient && rule.Info.IsFormal)))
            {
                continue;
            }

            issues.Add(new Issue(finding.Start, finding.Length, rule.Info, IssueKind.Grammar, text.Substring(finding.Start, finding.Length), finding.Message, finding.Explanation, finding.Suggestions));
        }
    }
}
