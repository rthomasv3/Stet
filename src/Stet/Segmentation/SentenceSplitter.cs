namespace Stet.Segmentation;

/// <summary>Splits a paragraph into sentences.</summary>
internal static class SentenceSplitter
{
    /// <summary>
    /// Finds the sentences of a paragraph. Handles abbreviations (Mr., Dr., St.), initials, ellipses, and
    /// speech followed by its tag: <c>“Why?” she asked.</c> is one sentence.
    /// </summary>
    /// <param name="text">The paragraph.</param>
    /// <param name="tokens">Its tokens, from <see cref="Tokenizer.Tokenize"/>.</param>
    public static List<TextSpan> Split(string text, IReadOnlyList<Token> tokens)
    {
        var sentences = new List<TextSpan>();
        int sentenceStart = -1;
        int i = 0;
        while (i < tokens.Count)
        {
            Token token = tokens[i];
            if (sentenceStart < 0)
            {
                sentenceStart = token.Start;
            }

            if (!IsTerminator(text, token))
            {
                i++;
                continue;
            }

            // Take in the whole run of end marks (?!, ...?) and any closing quotes or brackets after it.
            int first = i;
            int last = i;
            while (last + 1 < tokens.Count && tokens[last + 1].Start == tokens[last].End && IsTerminator(text, tokens[last + 1]))
            {
                last++;
            }

            bool closedQuote = false;
            while (last + 1 < tokens.Count && tokens[last + 1].Start == tokens[last].End && IsCloser(text, tokens[last + 1]))
            {
                last++;
                closedQuote |= IsQuote(text[tokens[last].Start]);
            }

            if (IsBoundary(text, tokens, first, last, closedQuote))
            {
                sentences.Add(TextSpan.FromBounds(sentenceStart, tokens[last].End));
                sentenceStart = -1;
            }

            i = last + 1;
        }

        if (sentenceStart >= 0 && tokens.Count > 0)
        {
            sentences.Add(TextSpan.FromBounds(sentenceStart, tokens[^1].End));
        }

        return sentences;
    }

    private static bool IsBoundary(string text, IReadOnlyList<Token> tokens, int first, int last, bool closedQuote)
    {
        if (last + 1 >= tokens.Count)
        {
            return true;
        }

        Token next = tokens[last + 1];
        if (next.Start == tokens[last].End)
        {
            return false; // no space after the mark: "U.S.A", "3.5", ".com"
        }

        Token mark = tokens[first];
        bool fullStop = mark.Length == 1 && text[mark.Start] == '.' && first == last;
        bool ellipsis = text[mark.Start] == CharClass.Ellipsis || (text[mark.Start] == '.' && mark.Length > 1);
        bool nextIsCapital = StartsWithCapital(text, tokens, last + 1);

        if (fullStop && first > 0 && tokens[first - 1].End == mark.Start)
        {
            Token before = tokens[first - 1];
            if (before.Kind == TokenKind.Initial)
            {
                return false;
            }

            if (before.Kind == TokenKind.Word)
            {
                string word = Lower(text, before);
                if (WordLists.TitleAbbreviations.Contains(word))
                {
                    return false;
                }

                // "No. 5" is an abbreviation; "No." on its own is an answer.
                if (word is "no" or "nos" && next.Kind == TokenKind.Number)
                {
                    return false;
                }

                // "e.g." and "i.e.": a single lowercase letter before the full stop is not a sentence end.
                if (word.Length == 1 && char.IsLower(word[0]) && first >= 2)
                {
                    return false;
                }

                if (WordLists.OtherAbbreviations.Contains(word) && !nextIsCapital)
                {
                    return false;
                }
            }
        }

        if (!nextIsCapital)
        {
            // A lowercase word carries the sentence on: “Why?” she asked.   Well... maybe.   approx. five miles.
            // A missing capital after a real sentence end is left for a rule to find; guessing a boundary here
            // would turn every unknown abbreviation into a false sentence start.
            return false;
        }

        // “Why?” Tom asked.  A name and a speech verb after the closing quote belong to the same sentence.
        if (closedQuote && !fullStop && IsTagAhead(text, tokens, last + 1))
        {
            return false;
        }

        return true;
    }

    // Up to three capitalized words (Tom / Mr. Smith / Lady Anne Grey) followed by a speech verb.
    private static bool IsTagAhead(string text, IReadOnlyList<Token> tokens, int index)
    {
        int words = 0;
        for (int i = index; i < tokens.Count && words < 4; i++)
        {
            Token token = tokens[i];
            if (token.Kind == TokenKind.Punctuation)
            {
                if (text[token.Start] == '.' && i > index && WordLists.TitleAbbreviations.Contains(Lower(text, tokens[i - 1])))
                {
                    continue;
                }

                return false;
            }

            if (token.Kind is not (TokenKind.Word or TokenKind.Initial))
            {
                return false;
            }

            if (char.IsLower(text[token.Start]))
            {
                return WordLists.DialogueTagVerbs.Contains(Lower(text, token));
            }

            words++;
        }

        return false;
    }

    private static bool StartsWithCapital(string text, IReadOnlyList<Token> tokens, int index)
    {
        // Look past opening quotes and brackets to the first word.
        for (int i = index; i < tokens.Count; i++)
        {
            Token token = tokens[i];
            if (token.Kind == TokenKind.Punctuation)
            {
                char c = text[token.Start];
                if (IsQuote(c) || c is '(' or '[' or CharClass.EmDash or CharClass.EnDash || (c == '-' && token.Length > 1))
                {
                    continue;
                }

                return true; // other punctuation: treat as a fresh start
            }

            if (token.Kind == TokenKind.Number)
            {
                return true;
            }

            return !char.IsLower(text[token.Start]);
        }

        return true;
    }

    private static bool IsTerminator(string text, Token token)
    {
        if (token.Kind != TokenKind.Punctuation)
        {
            return false;
        }

        return text[token.Start] is '.' or '!' or '?' or CharClass.Ellipsis or CharClass.Interrobang;
    }

    private static bool IsCloser(string text, Token token)
    {
        if (token.Kind != TokenKind.Punctuation || token.Length != 1)
        {
            return false;
        }

        char c = text[token.Start];
        if (c is CharClass.RightDoubleQuote or CharClass.RightSingleQuote or ')' or ']')
        {
            return true;
        }

        // A straight quote closes only when it hugs the mark and is followed by a space or the end.
        return c is '"' or '\'' && (token.End >= text.Length || !CharClass.IsWordChar(text, token.End));
    }

    private static bool IsQuote(char c) => CharClass.IsDoubleQuote(c) || CharClass.IsSingleQuote(c);

    private static string Lower(string text, Token token) =>
        text.Substring(token.Start, token.Length).ToLowerInvariant().Replace(CharClass.RightSingleQuote, '\'');
}
