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
    /// <param name="wholeParagraphContinues">
    /// The paragraph has no marks of its own and sits inside a longer speech (used by <see cref="DetectDocument"/>).
    /// </param>
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
        bool openAtEndIsDialogue = true,
        bool wholeParagraphContinues = false)
    {
        if (wholeParagraphContinues)
        {
            // A line of a song or letter inside a longer speech: no marks of its own, speech on both sides.
            return new DialogueResult([new TextSpan(0, text.Length)], true, 0);
        }

        int carried = 0;
        int continuedUntil = -1;
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
                // No reopening mark. If the first mark is a closing one, the speech simply ran on across a
                // paragraph break; otherwise the earlier open mark was a slip.
                continuedUntil = FirstMarkIsClose(text, style, skipRanges);
                if (continuedUntil < 0)
                {
                    carried = 1;
                }
            }
        }

        if (continuedUntil >= 0)
        {
            // Scan the rest on its own; the part up to the closing mark is speech.
            skipRanges = [.. skipRanges ?? [], new TextSpan(0, continuedUntil + 1)];
        }

        // First assume a second opening mark is a quote within the speech (some books nest the same marks).
        // If that leaves the paragraph unbalanced, the extra mark was more likely a missing closing mark, so
        // scan again treating each opening mark as a fresh start.
        Scan scan = ScanMarks(text, style, skipRanges, allowNesting: true);
        if (scan.OpenAt >= 0 && scan.Nested)
        {
            scan = ScanMarks(text, style, skipRanges, allowNesting: false);
        }

        List<TextSpan> spans = scan.Spans;
        if (continuedUntil >= 0)
        {
            spans.Insert(0, new TextSpan(0, continuedUntil + 1));
        }

        int unmatched = scan.Unmatched + carried;
        bool endsOpen = scan.OpenAt >= 0;
        if (endsOpen && openAtEndIsDialogue)
        {
            spans.Add(TextSpan.FromBounds(scan.OpenAt, text.Length));
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
    /// open is only believed when what follows continues it: the next paragraph reopens it, or closes it before
    /// opening another, possibly after a few lines with no marks at all (a song or letter).
    /// </summary>
    public static List<DialogueResult> DetectDocument(IReadOnlyList<string> paragraphs, QuoteStyle style)
    {
        var results = new List<DialogueResult>(paragraphs.Count);
        bool carry = false;
        int markless = 0; // paragraphs still to be taken whole as speech
        for (int p = 0; p < paragraphs.Count; p++)
        {
            if (markless > 0)
            {
                markless--;
                results.Add(Detect(paragraphs[p], style, wholeParagraphContinues: true));
                carry = true;
                continue;
            }

            DialogueResult result = Detect(paragraphs[p], style, carry);
            if (result.EndsInDialogue && p + 1 < paragraphs.Count)
            {
                int linesBeforeClose = ContinuationLength(paragraphs, p + 1, style);
                if (linesBeforeClose < 0)
                {
                    result = Detect(paragraphs[p], style, carry, openAtEndIsDialogue: false);
                }
                else
                {
                    markless = linesBeforeClose;
                }
            }

            results.Add(result);
            carry = result.EndsInDialogue;
        }

        return results;
    }

    // How the paragraphs from <paramref name="start"/> carry on a speech left open: 0 when the next one reopens
    // or closes it, n when n paragraphs without any marks come first (lines of a song or letter), and -1 when
    // nothing within reach continues it.
    private static int ContinuationLength(IReadOnlyList<string> paragraphs, int start, QuoteStyle style)
    {
        const int MaxLines = 8;
        for (int i = start; i < paragraphs.Count && i - start <= MaxLines; i++)
        {
            string text = paragraphs[i];
            if (i == start && StartsWithOpeningQuote(text, style))
            {
                return 0;
            }

            switch (FirstMark(text, style, null, out _))
            {
                case Mark.Close:
                    return i - start;
                case Mark.Open:
                    return -1;
            }
        }

        return -1;
    }

    private static int FirstMarkIsClose(string text, QuoteStyle style, IReadOnlyList<TextSpan>? skipRanges) =>
        FirstMark(text, style, skipRanges, out int at) == Mark.Close ? at : -1;

    // The first quote mark of a paragraph, read as if a speech were already open.
    private static Mark FirstMark(string text, QuoteStyle style, IReadOnlyList<TextSpan>? skipRanges, out int at)
    {
        for (int i = 0; i < text.Length; i++)
        {
            Mark mark = style == QuoteStyle.Double ? ClassifyDouble(text, i, isOpen: true) : ClassifySingle(text, i, isOpen: true, openedAt: -1);
            if (mark != Mark.None && !TextSpan.AnyContains(skipRanges, i))
            {
                at = i;
                return mark;
            }
        }

        at = -1;
        return Mark.None;
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

    private static Scan ScanMarks(string text, QuoteStyle style, IReadOnlyList<TextSpan>? skipRanges, bool allowNesting)
    {
        var spans = new List<TextSpan>();
        int unmatched = 0;
        int open = -1;
        int depth = 0;
        bool nested = false;

        for (int i = 0; i < text.Length; i++)
        {
            Mark mark = style == QuoteStyle.Double ? ClassifyDouble(text, i, open >= 0) : ClassifySingle(text, i, open >= 0, open);
            if (mark == Mark.None || TextSpan.AnyContains(skipRanges, i))
            {
                continue;
            }

            if (mark == Mark.Open)
            {
                if (open < 0)
                {
                    open = i;
                    depth = 1;
                }
                else if (allowNesting)
                {
                    depth++;
                    nested = true;
                }
                else
                {
                    unmatched++; // the earlier mark never closed; what followed it stays narration
                    open = i;
                }
            }
            else if (open >= 0)
            {
                if (--depth == 0)
                {
                    spans.Add(TextSpan.FromBounds(open, i + 1));
                    open = -1;
                }
            }
            else
            {
                unmatched++;
            }
        }

        return new Scan(spans, unmatched, open, nested);
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
    private static Mark ClassifySingle(string text, int i, bool isOpen, int openedAt)
    {
        char c = text[i];
        if (!CharClass.IsSingleQuote(c))
        {
            return Mark.None;
        }

        bool letterBefore = CharClass.IsWordChar(text, i - 1);
        bool letterAfter = CharClass.IsWordChar(text, i + 1);

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
            if (openedAt >= 0 && openedAt == i - word.Length - 1)
            {
                return Mark.Close; // a single quoted word or letter: the ‘i’ in a name
            }

            bool droppedEnding = WordLists.TrailingApostropheWords.Contains(word) || (word.Length >= 4 && word.EndsWith("in", StringComparison.Ordinal));
            if (droppedEnding)
            {
                return Mark.None;
            }

            if (word.EndsWith('s') && i + 2 < text.Length && text[i + 1] == ' ' && char.IsLower(text[i + 2]))
            {
                // "the boys' toys" deep inside speech is a possessive; a short quoted phrase is just closing.
                // A speech opened in an earlier paragraph (openedAt < 0) is long by definition.
                return openedAt < 0 || WordsBetween(text, openedAt, i) > 3 ? Mark.None : Mark.Close;
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

    private readonly record struct Scan(List<TextSpan> Spans, int Unmatched, int OpenAt, bool Nested);

    private enum Mark
    {
        None,
        Open,
        Close,
    }
}
