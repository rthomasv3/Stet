using System.Collections.Concurrent;
using FictionChecker.Segmentation;

namespace FictionChecker.Spelling;

/// <summary>What the caller knows about a paragraph beyond its text.</summary>
internal sealed record SpellingContext
{
    /// <summary>The previous paragraph ended with its quote still open.</summary>
    public bool StartsInDialogue { get; init; }

    /// <summary>Ranges to leave unchecked (foreign words in italics, a deliberately misspelled letter).</summary>
    public IReadOnlyList<TextSpan>? SkipRanges { get; init; }

    /// <summary>Ranges to treat as dialogue whatever the quote marks say (italic thoughts).</summary>
    public IReadOnlyList<TextSpan>? DialogueRanges { get; init; }

    /// <summary>Dialogue already found for this paragraph (used when checking a whole document).</summary>
    public DialogueResult? Dialogue { get; init; }

    /// <summary>The paragraph's tokens, when the caller already has them.</summary>
    public List<Token>? Tokens { get; init; }

    /// <summary>No extra information.</summary>
    public static SpellingContext None { get; } = new();
}

/// <summary>
/// The spelling checker: finds misspellings, other-dialect spellings and wrong capitals in a paragraph, and
/// suggests fixes. Safe to use from several threads at once.
/// </summary>
internal sealed class SpellChecker
{
    private const int SuggestionCacheLimit = 4000;

    private readonly Lexicon _lexicon;
    private readonly WordClassifier _classifier;
    private readonly Suggester _suggester;
    private volatile SpellingState _state = new(CustomWords.Empty);

    /// <summary>Creates a checker over loaded data.</summary>
    public SpellChecker(Lexicon lexicon, BuiltInLists lists)
    {
        _lexicon = lexicon;
        _classifier = new WordClassifier(lexicon, lists);
        _suggester = new Suggester(lexicon, lists, _classifier);
    }

    /// <summary>Creates a checker from the data embedded in the assembly.</summary>
    public static SpellChecker CreateDefault() => new(Lexicon.LoadEmbedded(), BuiltInLists.LoadEmbedded());

    /// <summary>The current custom words.</summary>
    public CustomWords Custom => _state.Custom;

    /// <summary>Replaces a named word set in full.</summary>
    public void SetWords(string name, IEnumerable<string> words, WordScope scope = WordScope.Everywhere) =>
        Update(custom => custom.WithSet(name, words, scope));

    /// <summary>Adds words to a named set.</summary>
    public void AddWords(string name, IEnumerable<string> words) => Update(custom => custom.WithAdded(name, words));

    /// <summary>Removes words from a named set.</summary>
    public void RemoveWords(string name, IEnumerable<string> words) => Update(custom => custom.WithRemoved(name, words));

    /// <summary>Removes a named set.</summary>
    public void ClearWords(string name) => Update(custom => custom.WithoutSet(name));

    /// <summary>Whether a single word is correctly spelled (as narration).</summary>
    public bool IsValidWord(string word, SpellingOptions options, SpellingState? state = null)
    {
        state ??= _state;
        string text = word.Replace(CharClass.RightSingleQuote, '\'');
        var issues = new List<SpellingIssue>();
        CheckTokens(text, Tokenizer.Tokenize(text), null, options, state, issues, withSuggestions: false);
        return issues.Count == 0;
    }

    /// <summary>Suggestions for a single word, best first.</summary>
    public IReadOnlyList<string> Suggest(string word, SpellingOptions options, SpellingState? state = null)
    {
        state ??= _state;
        var context = new WordClassifier.Context(options.Dialect, state.Custom, false, false, options.FlagOtherDialects);
        return SuggestCached(word.Replace(CharClass.RightSingleQuote, '\''), context, options, state);
    }

    /// <summary>Checks one paragraph.</summary>
    public SpellingResult CheckParagraph(string text, SpellingOptions options, SpellingContext? context = null, SpellingState? state = null)
    {
        context ??= SpellingContext.None;
        if (Tokenizer.IsSceneBreak(text))
        {
            return new SpellingResult([], context.StartsInDialogue);
        }

        DialogueResult dialogue = context.Dialogue
            ?? DialogueDetector.Detect(text, options.EffectiveQuoteStyle, context.StartsInDialogue, context.SkipRanges, context.DialogueRanges);
        var issues = new List<SpellingIssue>();
        CheckTokens(text, context.Tokens ?? Tokenizer.Tokenize(text, context.SkipRanges), dialogue, options, state ?? _state, issues, withSuggestions: options.MaxSuggestions > 0);
        return new SpellingResult(issues, dialogue.EndsInDialogue);
    }

    /// <summary>
    /// Checks consecutive paragraphs, carrying an open quote from each to the next.
    /// </summary>
    public List<SpellingResult> CheckDocument(IReadOnlyList<string> paragraphs, SpellingOptions options, CancellationToken cancel = default)
    {
        List<DialogueResult> dialogue = DialogueDetector.DetectDocument(paragraphs, options.EffectiveQuoteStyle);
        var results = new List<SpellingResult>(paragraphs.Count);
        for (int i = 0; i < paragraphs.Count; i++)
        {
            cancel.ThrowIfCancellationRequested();
            results.Add(CheckParagraph(paragraphs[i], options, new SpellingContext { Dialogue = dialogue[i] }));
        }

        return results;
    }

    private void Update(Func<CustomWords, CustomWords> change)
    {
        while (true)
        {
            SpellingState before = _state;
            var after = new SpellingState(change(before.Custom));
            if (Interlocked.CompareExchange(ref _state, after, before) == before)
            {
                return;
            }
        }
    }

    private void CheckTokens(string text, List<Token> tokens, DialogueResult? dialogue, SpellingOptions options, SpellingState state, List<SpellingIssue> issues, bool withSuggestions)
    {
        Span<char> buffer = stackalloc char[64];
        for (int i = 0; i < tokens.Count; i++)
        {
            Token token = tokens[i];
            if (token.Kind != TokenKind.Word || token.Has(TokenFlags.Skipped) || token.Length > 64)
            {
                continue;
            }

            bool inDialogue = dialogue is not null && dialogue.IsDialogue(token.Start);
            if (inDialogue && options.Dialogue == DialogueStrictness.Off)
            {
                continue;
            }

            var context = new WordClassifier.Context(options.Dialect, state.Custom, inDialogue, options.Dialogue == DialogueStrictness.Lenient, options.FlagOtherDialects);

            // Curly apostrophes and unusual hyphens are folded so the dictionary sees one spelling.
            Span<char> word = buffer[..token.Length];
            token.TextIn(text).CopyTo(word);
            for (int k = 0; k < word.Length; k++)
            {
                if (CharClass.IsApostropheVariant(word[k]))
                {
                    word[k] = '\'';
                }
                else if (CharClass.IsHyphen(word[k]))
                {
                    word[k] = '-';
                }
            }

            if (!token.Has(TokenFlags.Hyphenated))
            {
                Report(text, tokens, i, token.Start, word, _classifier.Classify(word, token.Flags, context), context, options, state, issues, withSuggestions);
                continue;
            }

            // Hyphenated: fine if the whole is a word or every part is. Each bad part is reported on its own.
            if (_classifier.ClassifyForms(word, context).Status == WordStatus.Valid)
            {
                continue;
            }

            if (WordClassifier.IsStutter(word, out int finalStart))
            {
                ReadOnlySpan<char> final = word[finalStart..];
                Report(text, tokens, i, token.Start + finalStart, final, _classifier.Classify(final, token.Flags & TokenFlags.TrailingApostrophe, context), context, options, state, issues, withSuggestions);
                continue;
            }

            int partStart = 0;
            for (int k = 0; k <= word.Length; k++)
            {
                if (k < word.Length && word[k] != '-')
                {
                    continue;
                }

                ReadOnlySpan<char> part = word[partStart..k];
                TokenFlags flags = TokenFlags.None;
                if (partStart == 0)
                {
                    flags |= token.Flags & TokenFlags.LeadingApostrophe;
                }

                if (k == word.Length)
                {
                    flags |= token.Flags & TokenFlags.TrailingApostrophe;
                }

                if (part.Length > 0)
                {
                    Report(text, tokens, -1, token.Start + partStart, part, _classifier.ClassifyHyphenPart(part, flags, context, partStart == 0), context, options, state, issues, withSuggestions);
                }

                partStart = k + 1;
            }
        }
    }

    private void Report(
        string text,
        List<Token> tokens,
        int tokenIndex,
        int start,
        ReadOnlySpan<char> word,
        WordVerdict verdict,
        in WordClassifier.Context context,
        SpellingOptions options,
        SpellingState state,
        List<SpellingIssue> issues,
        bool withSuggestions)
    {
        if (verdict.Status == WordStatus.Valid)
        {
            return;
        }

        string written = word.ToString();
        var suggestions = new List<SpellingSuggestion>();
        SpellingKind kind = verdict.Status switch
        {
            WordStatus.OtherDialect => SpellingKind.OtherDialect,
            WordStatus.WrongCase => SpellingKind.Capitalization,
            _ => SpellingKind.Misspelling,
        };

        if (kind == SpellingKind.Misspelling && IsCapitalized(word) && tokenIndex >= 0
            && !LooksLikeSlipCached(written, context, IsSentenceStart(text, tokens, tokenIndex), state))
        {
            kind = SpellingKind.UnknownName;
        }

        if (kind == SpellingKind.Capitalization)
        {
            suggestions.Add(new SpellingSuggestion(start, word.Length, Restyle(text, start, verdict.Correction!)));
        }
        else if (withSuggestions)
        {
            if (kind == SpellingKind.Misspelling && tokenIndex >= 0)
            {
                AddJoins(text, tokens, tokenIndex, context, suggestions);
            }

            foreach (string candidate in SuggestCached(written, context, options, state))
            {
                if (suggestions.Count >= options.MaxSuggestions)
                {
                    break;
                }

                suggestions.Add(new SpellingSuggestion(start, word.Length, Restyle(text, start, candidate)));
            }
        }

        issues.Add(new SpellingIssue(start, word.Length, kind, text.Substring(start, word.Length), suggestions));
    }

    private static bool IsCapitalized(ReadOnlySpan<char> word)
    {
        if (word.Length < 2 || !char.IsUpper(word[0]))
        {
            return false;
        }

        // At least one lowercase letter: KAELITH in capitals could be any shouted word.
        foreach (char c in word[1..])
        {
            if (char.IsLower(c))
            {
                return true;
            }
        }

        return false;
    }

    // Whether a word opens a sentence or a quoted speech, where any word gets a capital.
    private static bool IsSentenceStart(string text, List<Token> tokens, int index)
    {
        for (int i = index - 1; i >= 0; i--)
        {
            Token token = tokens[i];
            if (token.Kind != TokenKind.Punctuation)
            {
                return false;
            }

            char c = text[token.Start];
            if (c is '.' or '!' or '?' or ':' or CharClass.Ellipsis or CharClass.LeftDoubleQuote or '"' or CharClass.EmDash)
            {
                return true;
            }

            if (c is not (CharClass.LeftSingleQuote or '\'' or '(' or '[' or CharClass.RightDoubleQuote or CharClass.RightSingleQuote))
            {
                return false;
            }
        }

        return true;
    }

    private bool LooksLikeSlipCached(string word, in WordClassifier.Context context, bool sentenceStart, SpellingState state)
    {
        var key = (word, context.Dialect, context.InDialogue, sentenceStart);
        if (state.Slips.TryGetValue(key, out bool cached))
        {
            return cached;
        }

        bool slip = _suggester.LooksLikeSlip(word, context, sentenceStart);
        if (state.Slips.Count >= SuggestionCacheLimit)
        {
            state.Slips.Clear();
        }

        state.Slips[key] = slip;
        return slip;
    }

    // "any where" → "anywhere": an unknown word that makes a word with its neighbour.
    private void AddJoins(string text, List<Token> tokens, int index, in WordClassifier.Context context, List<SpellingSuggestion> suggestions)
    {
        Token token = tokens[index];
        foreach (int other in (ReadOnlySpan<int>)[index + 1, index - 1])
        {
            if (other < 0 || other >= tokens.Count || tokens[other].Kind != TokenKind.Word)
            {
                continue;
            }

            Token first = other < index ? tokens[other] : token;
            Token second = other < index ? token : tokens[other];
            if (second.Start != first.End + 1 || text[first.End] != ' ' || first.Length + second.Length > 40)
            {
                continue;
            }

            string joined = string.Concat(first.TextIn(text), second.TextIn(text)).Replace(CharClass.RightSingleQuote, '\'');
            if (_lexicon.TryGet(joined.ToLowerInvariant(), out WordInfo info) && info.IsIn(context.Dialect) && !info.IsRare
                && _classifier.ClassifyForms(joined, context).Status == WordStatus.Valid)
            {
                suggestions.Add(new SpellingSuggestion(first.Start, second.End - first.Start, string.Concat(first.TextIn(text), second.TextIn(text))));
            }
        }
    }

    private IReadOnlyList<string> SuggestCached(string word, in WordClassifier.Context context, SpellingOptions options, SpellingState state)
    {
        // Suggestions depend on the dialect, the custom words (the state) and, for dialogue-only words, on
        // being inside dialogue.
        var key = (word, options.Dialect, context.InDialogue);
        if (state.Suggestions.TryGetValue(key, out List<string>? cached))
        {
            return cached;
        }

        List<string> fresh = _suggester.Suggest(word, context, Math.Max(options.MaxSuggestions, 5));
        if (state.Suggestions.Count >= SuggestionCacheLimit)
        {
            state.Suggestions.Clear();
        }

        state.Suggestions[key] = fresh;
        return fresh;
    }

    // Suggestions come back with straight apostrophes; give them the apostrophe the text uses.
    private static string Restyle(string text, int start, string suggestion)
    {
        if (!suggestion.Contains('\''))
        {
            return suggestion;
        }

        int end = Math.Min(text.Length, start + 64);
        bool curly = text.AsSpan(start, end - start).Contains(CharClass.RightSingleQuote) || (!text.Contains('\'') && text.Contains(CharClass.RightSingleQuote));
        return curly ? suggestion.Replace('\'', CharClass.RightSingleQuote) : suggestion;
    }
}

/// <summary>Custom words plus the suggestion caches that are only valid for them.</summary>
internal sealed class SpellingState(CustomWords custom)
{
    private static long s_lastId;

    /// <summary>A number unique to this state, for cache keys.</summary>
    public long Id { get; } = Interlocked.Increment(ref s_lastId);

    public CustomWords Custom { get; } = custom;

    public ConcurrentDictionary<(string Word, Dialect Dialect, bool InDialogue), List<string>> Suggestions { get; } = new();

    public ConcurrentDictionary<(string Word, Dialect Dialect, bool InDialogue, bool SentenceStart), bool> Slips { get; } = new();
}
