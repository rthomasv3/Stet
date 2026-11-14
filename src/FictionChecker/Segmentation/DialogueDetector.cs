namespace FictionChecker.Segmentation;

/// <summary>Which quote marks a text uses for speech.</summary>
internal enum QuoteStyle
{
    /// <summary>Double quotes for speech (usual in US publishing): “Hello,” she said.</summary>
    Double,

    /// <summary>Single quotes for speech (usual in UK publishing): ‘Hello,’ she said.</summary>
    Single,
}

/// <summary>Where the speech in a paragraph is.</summary>
/// <param name="Spans">Each stretch of speech, from its opening quote mark through its closing one.</param>
/// <param name="EndsInDialogue">The last quote was still open at the end of the paragraph.</param>
/// <param name="UnmatchedQuotes">Quote marks that could not be paired; text around them is treated as narration.</param>
internal sealed record DialogueResult(IReadOnlyList<TextSpan> Spans, bool EndsInDialogue, int UnmatchedQuotes)
{
    /// <summary>Whether a position is inside speech.</summary>
    public bool IsDialogue(int position) => TextSpan.AnyContains(Spans, position);
}

/// <summary>
/// Finds speech by its quote marks. Heuristic: when marks cannot be paired, the text is left as narration,
/// which means it is checked normally.
/// </summary>
internal static class DialogueDetector
{
    /// <summary>Finds the speech in one paragraph.</summary>
    /// <param name="text">The paragraph.</param>
    /// <param name="style">Which marks delimit speech.</param>
    /// <param name="startsInDialogue">
    /// The previous paragraph ended with its quote still open. By convention this paragraph then starts with a
    /// fresh opening mark; if it does not, the earlier open mark was a slip and this paragraph is narration.
    /// </param>
    /// <param name="skipRanges">Ranges to ignore; quote marks inside them do not count.</param>
    /// <param name="dialogueRanges">Ranges the caller wants treated as speech whatever the marks say.</param>
    /// <param name="openAtEndIsDialogue">
    /// Whether a quote still open at the end counts as speech running into the next paragraph (true), or as an
    /// unmatched mark (false, used when the next paragraph is known not to continue it).
    /// </param>
    public static DialogueResult Detect(
        string text,
        QuoteStyle style,
        bool startsInDialogue = false,
        IReadOnlyList<TextSpan>? skipRanges = null,
        IReadOnlyList<TextSpan>? dialogueRanges = null,
        bool openAtEndIsDialogue = true)
    {
        var spans = new List<TextSpan>();
        int unmatched = 0;
        int open = -1;

        if (startsInDialogue)
        {
            int first = 0;
            while (first < text.Length && char.IsWhiteSpace(text[first]))
            {
                first++;
            }

            bool reopens = first < text.Length && (style == QuoteStyle.Double
                ? text[first] is '"' or CharClass.LeftDoubleQuote
                : text[first] is '\'' or CharClass.LeftSingleQuote);
            if (!reopens)
            {
                unmatched++;
            }
        }

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            Mark mark = style == QuoteStyle.Double ? ClassifyDouble(text, i, open >= 0) : ClassifySingle(text, i, open);
            if (mark == Mark.None || TextSpan.AnyContains(skipRanges, i))
            {
                continue;
            }

            if (mark == Mark.Open)
            {
                if (open >= 0)
                {
                    unmatched++; // the earlier mark never closed; what followed it stays narration
                }

                open = i;
            }
            else if (open >= 0)
            {
                spans.Add(TextSpan.FromBounds(open, i + 1));
                open = -1;
            }
            else
            {
                unmatched++;
            }
        }

        bool endsOpen = open >= 0;
        if (endsOpen && openAtEndIsDialogue)
        {
            spans.Add(TextSpan.FromBounds(open, text.Length));
        }
        else if (endsOpen)
        {
            unmatched++;
            endsOpen = false;
        }

        if (dialogueRanges is { Count: > 0 })
        {
            spans.AddRange(dialogueRanges);
            spans.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        return new DialogueResult(spans, endsOpen, unmatched);
    }

    /// <summary>
    /// Finds the speech in consecutive paragraphs, carrying an open quote from one to the next. A quote left
    /// open is only believed when the next paragraph reopens it.
    /// </summary>
    public static List<DialogueResult> DetectDocument(IReadOnlyList<string> paragraphs, QuoteStyle style)
    {
        var results = new List<DialogueResult>(paragraphs.Count);
        bool carry = false;
        for (int p = 0; p < paragraphs.Count; p++)
        {
            DialogueResult result = Detect(paragraphs[p], style, carry);
            if (result.EndsInDialogue && p + 1 < paragraphs.Count && !StartsWithOpeningQuote(paragraphs[p + 1], style))
            {
                result = Detect(paragraphs[p], style, carry, openAtEndIsDialogue: false);
            }

            results.Add(result);
            carry = result.EndsInDialogue;
        }

        return results;
    }

    /// <summary>
    /// Guesses which marks a text uses for speech, from the marks that open its paragraphs.
    /// </summary>
    public static QuoteStyle GuessStyle(IEnumerable<string> paragraphs)
    {
        int doubles = 0;
        int singles = 0;
        foreach (string paragraph in paragraphs)
        {
            if (StartsWithOpeningQuote(paragraph, QuoteStyle.Double))
            {
                doubles++;
            }
            else if (StartsWithOpeningQuote(paragraph, QuoteStyle.Single))
            {
                singles++;
            }
        }

        return singles > doubles ? QuoteStyle.Single : QuoteStyle.Double;
    }

    private static bool StartsWithOpeningQuote(string text, QuoteStyle style)
    {
        int first = 0;
        while (first < text.Length && char.IsWhiteSpace(text[first]))
        {
            first++;
        }

        if (first >= text.Length)
        {
            return false;
        }

        return style == QuoteStyle.Double ? text[first] is '"' or CharClass.LeftDoubleQuote : text[first] is '\'' or CharClass.LeftSingleQuote;
    }

    private static Mark ClassifyDouble(string text, int i, bool isOpen)
    {
        switch (text[i])
        {
            case CharClass.LeftDoubleQuote:
                return Mark.Open;
            case CharClass.RightDoubleQuote:
                return Mark.Close;
            case '"':
                bool openPosition = LooksLikeOpening(text, i);
                bool closePosition = LooksLikeClosing(text, i);
                if (isOpen)
                {
                    return closePosition || !openPosition ? Mark.Close : Mark.Open;
                }

                return openPosition || !closePosition ? Mark.Open : Mark.Close;
            default:
                return Mark.None;
        }
    }

    // Single quotes share their marks with apostrophes, so each one is judged by what surrounds it.
    private static Mark ClassifySingle(string text, int i, int open)
    {
        char c = text[i];
        if (!CharClass.IsSingleQuote(c))
        {
            return Mark.None;
        }

        bool letterBefore = CharClass.IsWordChar(text, i - 1);
        bool letterAfter = CharClass.IsWordChar(text, i + 1);
        bool isOpen = open >= 0;

        if (letterBefore && letterAfter)
        {
            return Mark.None; // don't, Kaelith's
        }

        if (letterAfter)
        {
            // Start of a word: an opening quote, or a dropped start ('tis, 'em).
            if (c == CharClass.LeftSingleQuote)
            {
                return Mark.Open;
            }

            bool knownElision = WordLists.LeadingApostropheWords.Contains(WordAfter(text, i + 1));
            if (c == CharClass.RightSingleQuote)
            {
                return Mark.None; // a closing-shaped mark cannot open speech: ’tis, ’ave
            }

            if (isOpen)
            {
                return knownElision || !LooksLikeOpening(text, i) ? Mark.None : Mark.Open;
            }

            return LooksLikeOpening(text, i) ? Mark.Open : Mark.None;
        }

        if (letterBefore)
        {
            // End of a word: a closing quote, or a dropped ending (runnin', o') or a plural possessive (boys').
            if (c == CharClass.LeftSingleQuote || !isOpen)
            {
                return Mark.None;
            }

            if (OnlyClosersFollow(text, i + 1))
            {
                return Mark.Close;
            }

            string word = WordBefore(text, i);
            bool droppedEnding = WordLists.TrailingApostropheWords.Contains(word) || (word.Length >= 4 && word.EndsWith("in", StringComparison.Ordinal));
            if (droppedEnding)
            {
                return Mark.None;
            }

            if (word.EndsWith('s') && i + 2 < text.Length && text[i + 1] == ' ' && char.IsLower(text[i + 2]))
            {
                // "the boys' toys" deep inside speech is a possessive; a short quoted phrase is just closing.
                return WordsBetween(text, open, i) > 3 ? Mark.None : Mark.Close;
            }

            return Mark.Close;
        }

        // Punctuation or space on both sides.
        if (c == CharClass.LeftSingleQuote)
        {
            return Mark.Open;
        }

        bool spaceBefore = i == 0 || char.IsWhiteSpace(text[i - 1]);
        if (isOpen && !spaceBefore)
        {
            return Mark.Close; // ,' .' !' —'
        }

        if (c == '\'' && LooksLikeOpening(text, i))
        {
            return Mark.Open;
        }

        return isOpen || spaceBefore ? Mark.None : Mark.Close;
    }

    private static bool LooksLikeOpening(string text, int i)
    {
        bool before = i == 0 || char.IsWhiteSpace(text[i - 1]) || text[i - 1] is '(' or '[' or CharClass.EmDash or CharClass.EnDash or '-' or CharClass.LeftDoubleQuote or CharClass.LeftSingleQuote or '"' or '\'';
        bool after = i + 1 < text.Length && !char.IsWhiteSpace(text[i + 1]);
        return before && after;
    }

    private static bool LooksLikeClosing(string text, int i)
    {
        bool before = i > 0 && !char.IsWhiteSpace(text[i - 1]);
        bool after = i + 1 >= text.Length || !CharClass.IsWordChar(text, i + 1);
        return before && after;
    }

    private static bool OnlyClosersFollow(string text, int from)
    {
        for (int i = from; i < text.Length; i++)
        {
            if (!char.IsWhiteSpace(text[i]) && text[i] is not ('"' or CharClass.RightDoubleQuote or ')' or ']'))
            {
                return false;
            }
        }

        return true;
    }

    private static string WordAfter(string text, int start)
    {
        int end = start;
        while (end < text.Length && (CharClass.IsWordChar(text, end) || (CharClass.IsApostrophe(text[end]) && CharClass.IsWordChar(text, end + 1))))
        {
            end++;
        }

        return text[start..end].ToLowerInvariant().Replace(CharClass.RightSingleQuote, '\'');
    }

    private static string WordBefore(string text, int end)
    {
        int start = end;
        while (start > 0 && (CharClass.IsWordChar(text, start - 1) || (CharClass.IsApostrophe(text[start - 1]) && CharClass.IsWordChar(text, start - 2))))
        {
            start--;
        }

        return text[start..end].ToLowerInvariant().Replace(CharClass.RightSingleQuote, '\'');
    }

    private static int WordsBetween(string text, int from, int to)
    {
        int words = 0;
        bool inWord = false;
        for (int i = from; i < to; i++)
        {
            bool wordChar = CharClass.IsWordChar(text, i);
            if (wordChar && !inWord)
            {
                words++;
            }

            inWord = wordChar;
        }

        return words;
    }

    private enum Mark
    {
        None,
        Open,
        Close,
    }
}
