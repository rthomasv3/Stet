using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Stet.Grammar;
using Stet.Segmentation;
using Stet.Spelling;

namespace Stet;

/// <summary>
/// Checks fiction text. Create one and keep it: creating it loads the built-in data, and it then works
/// entirely in memory with no file or network access.
///
/// A checker is safe to use from several threads at once. Changing a word set swaps in a new copy, so a check
/// that is running never sees half an update. Checks run on the calling thread; call from a background thread
/// to keep an interface responsive.
/// </summary>
public sealed class Checker
{
    private static readonly IReadOnlyList<Issue> NoIssues = [];

    private readonly SpellChecker _spelling;
    private readonly GrammarEngine _grammar;
    private readonly RuleInfo[] _rules;
    private readonly ConditionalWeakTable<CheckerOptions, ResolvedOptions> _resolved = [];
    private volatile WordState _words = new(CustomWords.Empty, null);

    /// <summary>Creates a checker and loads its data.</summary>
    /// <param name="options">Settings used when a check does not bring its own. Null means the defaults.</param>
    public Checker(CheckerOptions? options = null)
        : this(Lexicon.LoadEmbedded(), BuiltInLists.LoadEmbedded(), options)
    {
    }

    internal Checker(Lexicon lexicon, BuiltInLists lists, CheckerOptions? options = null)
    {
        _spelling = new SpellChecker(lexicon, lists);
        _grammar = GrammarEngine.CreateDefault(lexicon);
        _rules = [.. RuleCatalog.Spelling, .. _grammar.Rules.Select(r => r.Info)];
        Options = options ?? CheckerOptions.Default;
    }

    /// <summary>The settings used when a check does not bring its own.</summary>
    public CheckerOptions Options { get; }

    /// <summary>Every rule the checker has, for a settings screen.</summary>
    public IReadOnlyList<RuleInfo> Rules => _rules;

    /// <summary>Names of the word sets the checker holds.</summary>
    public IReadOnlyCollection<string> WordSetNames => [.. _words.Master.SetNames];

    /// <summary>
    /// Replaces a named word set in full, creating it if needed. Each entry is split on spaces, so
    /// "Kaelith Vorn" adds both names. A word keeps the capitals it is added with: "Kaelith" is valid as
    /// Kaelith or KAELITH but not kaelith, while a lowercase word is also valid capitalized. The possessive of
    /// every word is accepted too.
    ///
    /// Setting a set to the words it already holds does nothing, and results remembered by a
    /// <see cref="CheckCache"/> stay valid. An app can hand over its whole word list before every check.
    /// </summary>
    /// <param name="setName">The set's name, such as "world" or "personal".</param>
    /// <param name="words">The words. The checker keeps its own copy.</param>
    /// <param name="scope">Whether the words are valid everywhere or only inside dialogue.</param>
    public void SetWords(string setName, IEnumerable<string> words, WordScope scope = WordScope.Everywhere)
    {
        ArgumentNullException.ThrowIfNull(setName);
        ArgumentNullException.ThrowIfNull(words);
        Update(custom => custom.WithSet(setName, words, scope));
    }

    /// <summary>Adds words to a named set. A set that does not exist yet is created, valid everywhere.</summary>
    public void AddWords(string setName, IEnumerable<string> words)
    {
        ArgumentNullException.ThrowIfNull(setName);
        ArgumentNullException.ThrowIfNull(words);
        Update(custom => custom.WithAdded(setName, words));
    }

    /// <summary>Removes words from a named set. Words must be written as they were added.</summary>
    public void RemoveWords(string setName, IEnumerable<string> words)
    {
        ArgumentNullException.ThrowIfNull(setName);
        ArgumentNullException.ThrowIfNull(words);
        Update(custom => custom.WithRemoved(setName, words));
    }

    /// <summary>Removes a named set and all its words.</summary>
    public void ClearWords(string setName)
    {
        ArgumentNullException.ThrowIfNull(setName);
        Update(custom => custom.WithoutSet(setName));
    }

    /// <summary>Checks one paragraph.</summary>
    /// <param name="text">The paragraph, as plain text.</param>
    /// <param name="context">What is known about the paragraph beyond its text, and options for this check.</param>
    public ParagraphResult CheckParagraph(string text, ParagraphContext? context = null) => CheckParagraph(text, context, null);

    /// <summary>
    /// Checks consecutive paragraphs, such as a scene, carrying an open quote from each paragraph to the next.
    /// </summary>
    /// <param name="paragraphs">The paragraphs in order.</param>
    /// <param name="options">Options for this check. Null means the checker's own.</param>
    /// <param name="cancellationToken">Stops the check between paragraphs.</param>
    /// <returns>One result for each paragraph, in the same order.</returns>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public IReadOnlyList<ParagraphResult> CheckDocument(IReadOnlyList<string> paragraphs, CheckerOptions? options = null, CancellationToken cancellationToken = default) =>
        CheckDocument(paragraphs, options, cancellationToken, null);

    /// <summary>
    /// Checks consecutive paragraphs that carry skip and dialogue ranges, or are skipped whole.
    /// </summary>
    /// <param name="paragraphs">The paragraphs in order.</param>
    /// <param name="options">Options for this check. Null means the checker's own.</param>
    /// <param name="cancellationToken">Stops the check between paragraphs.</param>
    /// <returns>One result for each paragraph, in the same order.</returns>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public IReadOnlyList<ParagraphResult> CheckDocument(IReadOnlyList<DocumentParagraph> paragraphs, CheckerOptions? options = null, CancellationToken cancellationToken = default) =>
        CheckDocument(paragraphs, options, cancellationToken, null);

    /// <summary>Whether a single word is spelled correctly, as it would be judged in narration.</summary>
    public bool IsValidWord(string word, CheckerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(word);
        ResolvedOptions resolved = Resolve(options);
        return _spelling.IsValidWord(word, resolved.Spelling, StateFor(resolved));
    }

    /// <summary>Spelling suggestions for a single word, best first. Empty when nothing is close.</summary>
    public IReadOnlyList<string> Suggest(string word, CheckerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(word);
        ResolvedOptions resolved = Resolve(options);
        IReadOnlyList<string> all = _spelling.Suggest(word, resolved.Spelling, StateFor(resolved));
        int max = resolved.Spelling.MaxSuggestions > 0 ? resolved.Spelling.MaxSuggestions : 5;
        return all.Count <= max ? all : [.. all.Take(max)];
    }

    internal ParagraphResult CheckParagraph(string text, ParagraphContext? context, CheckCache? cache)
    {
        ArgumentNullException.ThrowIfNull(text);
        bool startsInDialogue = context?.StartsInDialogue ?? false;
        if (Tokenizer.IsSceneBreak(text))
        {
            return new ParagraphResult(NoIssues, startsInDialogue);
        }

        ResolvedOptions resolved = Resolve(context?.Options);
        DialogueResult dialogue = DialogueDetector.Detect(text, resolved.QuoteStyle, startsInDialogue, context?.SkipRanges, context?.DialogueRanges);
        return CheckCore(text, dialogue, context?.SkipRanges, resolved, StateFor(resolved), cache, context?.StartsMidSentence ?? false);
    }

    internal IReadOnlyList<ParagraphResult> CheckDocument(IReadOnlyList<string> paragraphs, CheckerOptions? options, CancellationToken cancel, CheckCache? cache)
    {
        ArgumentNullException.ThrowIfNull(paragraphs);
        ResolvedOptions resolved = Resolve(options);
        SpellingState state = StateFor(resolved);
        List<DialogueResult> dialogue = DialogueDetector.DetectDocument(paragraphs, resolved.QuoteStyle);
        var results = new ParagraphResult[paragraphs.Count];
        for (int i = 0; i < results.Length; i++)
        {
            cancel.ThrowIfCancellationRequested();
            results[i] = Tokenizer.IsSceneBreak(paragraphs[i])
                ? new ParagraphResult(NoIssues, dialogue[i].EndsInDialogue)
                : CheckCore(paragraphs[i], dialogue[i], null, resolved, state, cache, i > 0 && LeavesSentenceOpen(paragraphs[i - 1]));
        }

        return results;
    }

    internal IReadOnlyList<ParagraphResult> CheckDocument(IReadOnlyList<DocumentParagraph> paragraphs, CheckerOptions? options, CancellationToken cancel, CheckCache? cache)
    {
        ArgumentNullException.ThrowIfNull(paragraphs);
        ResolvedOptions resolved = Resolve(options);
        SpellingState state = StateFor(resolved);
        var texts = new string[paragraphs.Count];
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i] = paragraphs[i].Text;
        }

        IReadOnlyList<TextSpan>? SkipOf(int i) =>
            paragraphs[i].Skip ? [new TextSpan(0, texts[i].Length)] : paragraphs[i].SkipRanges;

        List<DialogueResult> dialogue = DialogueDetector.DetectDocument(texts, resolved.QuoteStyle, SkipOf, i => paragraphs[i].Skip ? null : paragraphs[i].DialogueRanges);
        var results = new ParagraphResult[texts.Length];
        for (int i = 0; i < results.Length; i++)
        {
            cancel.ThrowIfCancellationRequested();
            results[i] = paragraphs[i].Skip || Tokenizer.IsSceneBreak(texts[i])
                ? new ParagraphResult(NoIssues, dialogue[i].EndsInDialogue)
                : CheckCore(texts[i], dialogue[i], paragraphs[i].SkipRanges, resolved, state, cache, i > 0 && !paragraphs[i - 1].Skip && LeavesSentenceOpen(texts[i - 1]));
        }

        return results;
    }

    /// <summary>Numbers of the word states still in use, so a cache can drop results made with older ones.</summary>
    internal (object Version, HashSet<long> Live) LiveStates()
    {
        WordState words = _words;
        var live = new HashSet<long> { words.All.Id };
        foreach (View view in words.Views.Values)
        {
            live.Add(view.State.Id);
        }

        return (words, live);
    }

    internal object WordsVersion => _words;

    /// <summary>
    /// Whether a paragraph stops without ending its sentence: a line of verse, a heading, or prose broken off
    /// by an inserted letter or picture. The paragraph after it may rightly begin without a capital.
    /// </summary>
    internal static bool LeavesSentenceOpen(string text)
    {
        int at = text.Length - 1;
        while (at >= 0 && (char.IsWhiteSpace(text[at]) || CharClass.IsDoubleQuote(text[at]) || CharClass.IsSingleQuote(text[at]) || text[at] is ')' or ']' or '*' or '_'))
        {
            at--;
        }

        return at >= 0 && !Tokenizer.IsSceneBreak(text) && text[at] is not ('.' or '!' or '?' or CharClass.Ellipsis);
    }

    // The result depends only on what is passed here, which is what makes it safe to cache by these values.
    private ParagraphResult CheckCore(string text, DialogueResult dialogue, IReadOnlyList<TextSpan>? skipRanges, ResolvedOptions resolved, SpellingState state, CheckCache? cache, bool continues)
    {
        if (!resolved.AnySpelling && !resolved.AnyGrammar)
        {
            return new ParagraphResult(NoIssues, dialogue.EndsInDialogue, LeavesSentenceOpen(text));
        }

        CheckCache.Key? key = null;
        if (cache is not null)
        {
            key = new CheckCache.Key(text, state.Id, resolved.Fingerprint, dialogue, skipRanges, continues);
            if (cache.TryGet(key, out ParagraphResult? cached))
            {
                return cached;
            }
        }

        List<Token> tokens = Tokenizer.Tokenize(text, skipRanges);
        List<Issue>? list = null;
        if (resolved.AnySpelling)
        {
            SpellingResult found = _spelling.CheckParagraph(text, resolved.Spelling, new SpellingContext { SkipRanges = skipRanges, Dialogue = dialogue, Tokens = tokens }, state);
            foreach (SpellingIssue issue in found.Issues)
            {
                if (resolved.KindEnabled[(int)issue.Kind])
                {
                    (list ??= []).Add(ToIssue(issue, resolved.Spelling.Dialect));
                }
            }
        }

        if (resolved.AnyGrammar)
        {
            int before = list?.Count ?? 0;
            list ??= [];
            _grammar.Check(text, tokens, dialogue, state.Custom, resolved.GrammarEnabled, resolved.Spelling.Dialogue, resolved.QuoteStyle, list, continues);
            if (list.Count > before && list.Count > 1)
            {
                // Issues are reported in text order whichever part found them.
                list.Sort(static (a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : string.CompareOrdinal(a.RuleId, b.RuleId));
            }
        }

        IReadOnlyList<Issue> issues = list is { Count: > 0 } ? list.ToArray() : NoIssues;
        var result = new ParagraphResult(issues, dialogue.EndsInDialogue, LeavesSentenceOpen(text));
        cache?.Add(key!, result);
        return result;
    }

    private static Issue ToIssue(SpellingIssue issue, Dialect dialect)
    {
        var suggestions = new Suggestion[issue.Suggestions.Count];
        for (int i = 0; i < suggestions.Length; i++)
        {
            SpellingSuggestion s = issue.Suggestions[i];
            suggestions[i] = new Suggestion(s.Start, s.Length, s.Text);
        }

        string word = Quote(issue.Word);
        string? first = suggestions.Length > 0 && suggestions[0].Start == issue.Start && suggestions[0].Length == issue.Length ? Quote(suggestions[0].Text) : null;
        switch (issue.Kind)
        {
            case SpellingKind.UnknownName:
                return new Issue(issue.Start, issue.Length, RuleCatalog.UnknownName, IssueKind.UnknownName, issue.Word,
                    $"{word} is not a known word. It looks like a name.",
                    "Capitalized words that are unlike any known word are usually names. Add the name to a word list to accept it.",
                    suggestions);
            case SpellingKind.OtherDialect:
                return new Issue(issue.Start, issue.Length, RuleCatalog.OtherDialect, IssueKind.OtherDialectSpelling, issue.Word,
                    first is null ? $"{word} is not the {DialectName(dialect)} spelling." : $"{word} is spelled {first} in {DialectName(dialect)}.",
                    "The word is spelled correctly for another dialect of English.",
                    suggestions);
            case SpellingKind.Capitalization:
                return new Issue(issue.Start, issue.Length, RuleCatalog.Capitalization, IssueKind.Capitalization, issue.Word,
                    first is null ? $"{word} needs a capital letter." : $"{word} should be written {first}.",
                    "Names are written with a capital letter.",
                    suggestions);
            default:
                return new Issue(issue.Start, issue.Length, RuleCatalog.Misspelling, IssueKind.Misspelling, issue.Word,
                    $"{word} is not a known word.",
                    "The word is not in the dictionary or in your word lists. If it is intended, add it to a word list.",
                    suggestions);
        }
    }

    private static string Quote(string text) => string.Concat(CharClass.LeftDoubleQuote.ToString(), text, CharClass.RightDoubleQuote.ToString());

    private static string DialectName(Dialect dialect) => dialect switch
    {
        Dialect.British => "British English",
        Dialect.BritishIze => "British English with -ize spellings",
        Dialect.Canadian => "Canadian English",
        Dialect.Australian => "Australian English",
        _ => "American English",
    };

    private ResolvedOptions Resolve(CheckerOptions? options) =>
        _resolved.GetValue(options ?? Options, o => new ResolvedOptions(o, _grammar.Rules));

    private SpellingState StateFor(ResolvedOptions resolved)
    {
        WordState words = _words;
        if (resolved.SetNames is null)
        {
            return words.All;
        }

        return words.Views.GetOrAdd(resolved.SetsKey!, static (_, arg) =>
        {
            CustomWords subset = arg.words.Master.Subset(arg.names);
            return new View(ReferenceEquals(subset, arg.words.Master) ? arg.words.All : new SpellingState(subset), arg.names);
        }, (words, names: resolved.SetNames)).State;
    }

    private void Update(Func<CustomWords, CustomWords> change)
    {
        while (true)
        {
            WordState before = _words;
            CustomWords custom = change(before.Master);
            if (ReferenceEquals(custom, before.Master))
            {
                return;
            }

            var after = new WordState(custom, before);
            if (Interlocked.CompareExchange(ref _words, after, before) == before)
            {
                return;
            }
        }
    }

    private sealed record View(SpellingState State, string[] Names);

    /// <summary>
    /// The word sets at one moment, with the spelling state for all of them and for each selection of sets a
    /// check has asked for. A selection whose sets did not change keeps its state (and so its cached results)
    /// when another set changes.
    /// </summary>
    private sealed class WordState
    {
        public WordState(CustomWords master, WordState? previous)
        {
            Master = master;
            All = new SpellingState(master);
            if (previous is null)
            {
                return;
            }

            foreach (KeyValuePair<string, View> entry in previous.Views)
            {
                bool unchanged = true;
                foreach (string name in entry.Value.Names)
                {
                    unchanged &= master.HasSameSet(previous.Master, name);
                }

                if (unchanged)
                {
                    Views[entry.Key] = entry.Value;
                }
            }
        }

        public CustomWords Master { get; }

        public SpellingState All { get; }

        public ConcurrentDictionary<string, View> Views { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>Options worked out once into the form the checking code uses.</summary>
    private sealed class ResolvedOptions
    {
        public ResolvedOptions(CheckerOptions options, IReadOnlyList<GrammarRule> grammarRules)
        {
            KindEnabled = new bool[4];
            KindEnabled[(int)SpellingKind.Misspelling] = options.IsEnabled(RuleCatalog.Misspelling);
            KindEnabled[(int)SpellingKind.UnknownName] = options.IsEnabled(RuleCatalog.UnknownName);
            KindEnabled[(int)SpellingKind.OtherDialect] = options.IsEnabled(RuleCatalog.OtherDialect);
            KindEnabled[(int)SpellingKind.Capitalization] = options.IsEnabled(RuleCatalog.Capitalization);
            AnySpelling = Array.IndexOf(KindEnabled, true) >= 0;
            QuoteStyle = options.EffectiveQuoteStyle;
            Spelling = new SpellingOptions
            {
                Dialect = options.Dialect,
                QuoteStyle = QuoteStyle,
                Dialogue = options.DialogueStrictness,
                FlagOtherDialects = KindEnabled[(int)SpellingKind.OtherDialect],
                MaxSuggestions = Math.Max(0, options.MaxSuggestions),
            };

            GrammarEnabled = [.. grammarRules.Select(rule => options.IsEnabled(rule.Info))];
            AnyGrammar = Array.IndexOf(GrammarEnabled, true) >= 0;
            var mask = new System.Text.StringBuilder(KindEnabled.Length + GrammarEnabled.Length);
            foreach (bool on in (ReadOnlySpan<bool>)[.. KindEnabled, .. GrammarEnabled])
            {
                mask.Append(on ? '1' : '0');
            }

            // Everything that changes a paragraph's result, apart from the text, the speech found in it and
            // the word sets, which a cache key carries separately.
            Fingerprint = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{(int)options.Dialect}.{(int)QuoteStyle}.{(int)options.DialogueStrictness}.{Spelling.MaxSuggestions}.{mask}");
            if (options.WordSets is not null)
            {
                SetNames = [.. options.WordSets.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
                SetsKey = string.Join('\n', SetNames);
            }
        }

        public SpellingOptions Spelling { get; }

        public QuoteStyle QuoteStyle { get; }

        public bool[] KindEnabled { get; }

        public bool AnySpelling { get; }

        public bool[] GrammarEnabled { get; }

        public bool AnyGrammar { get; }

        public string Fingerprint { get; }

        public string[]? SetNames { get; }

        public string? SetsKey { get; }
    }
}
