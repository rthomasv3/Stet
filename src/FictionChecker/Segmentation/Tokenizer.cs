namespace FictionChecker.Segmentation;

/// <summary>Splits a paragraph into words, numbers and punctuation with their positions.</summary>
internal static class Tokenizer
{
    /// <summary>Tokenizes a paragraph.</summary>
    /// <param name="text">The paragraph.</param>
    /// <param name="skipRanges">Ranges the caller wants left alone; tokens starting inside one are flagged <see cref="TokenFlags.Skipped"/>.</param>
    public static List<Token> Tokenize(string text, IReadOnlyList<TextSpan>? skipRanges = null)
    {
        var tokens = new List<Token>(text.Length / 5);
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            Token token;
            int length;
            if ((length = UrlLength(text, i)) > 0)
            {
                token = new Token(i, length, TokenKind.Url);
            }
            else if ((length = EmailLength(text, i)) > 0)
            {
                token = new Token(i, length, TokenKind.Email);
            }
            else if (CharClass.IsWordChar(text, i) && !char.IsLowSurrogate(c))
            {
                token = ReadWord(text, i);
            }
            else
            {
                token = new Token(i, PunctuationLength(text, i), TokenKind.Punctuation);
            }

            if (TextSpan.AnyContains(skipRanges, token.Start))
            {
                token = token with { Flags = token.Flags | TokenFlags.Skipped };
            }

            tokens.Add(token);
            i = token.End;
        }

        return tokens;
    }

    /// <summary>
    /// Whether a paragraph is a scene break or ornament rather than prose: no letters or digits at all
    /// (<c>***</c>, <c># # #</c>, <c>~</c>, a row of dashes), or empty.
    /// </summary>
    public static bool IsSceneBreak(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsLetterOrDigit(text[i]) || char.IsHighSurrogate(text[i]) && char.IsLetterOrDigit(text, i))
            {
                return false;
            }
        }

        return true;
    }

    private static Token ReadWord(string text, int start)
    {
        int i = start;
        bool digits = false;
        bool letters = false;
        bool hyphen = false;
        while (i < text.Length)
        {
            int letter = CharClass.LetterLength(text, i);
            if (letter > 0)
            {
                letters = true;
                i += letter;
            }
            else if (char.IsDigit(text[i]))
            {
                digits = true;
                i++;
            }
            else if (i + 1 < text.Length && CharClass.IsWordChar(text, i + 1) && IsJoiner(text, i, digits && !letters))
            {
                hyphen |= CharClass.IsHyphen(text[i]);
                i++;
            }
            else
            {
                break;
            }
        }

        int length = i - start;
        if (digits)
        {
            return new Token(start, length, TokenKind.Number);
        }

        TokenFlags flags = hyphen ? TokenFlags.Hyphenated : TokenFlags.None;
        if (start > 0 && CharClass.IsSingleQuote(text[start - 1]) && !CharClass.IsWordChar(text, start - 2))
        {
            flags |= TokenFlags.LeadingApostrophe;
        }

        if (i < text.Length && CharClass.IsApostrophe(text[i]))
        {
            flags |= TokenFlags.TrailingApostrophe;
        }

        return new Token(start, length, IsInitial(text, start, length) ? TokenKind.Initial : TokenKind.Word, flags);
    }

    // Apostrophes and hyphens join letters; full stops and commas join digits (3.5, 1,000).
    private static bool IsJoiner(string text, int index, bool inNumber)
    {
        char c = text[index];
        if (CharClass.IsApostropheVariant(c) || CharClass.IsHyphen(c))
        {
            return true; // only reached between two letters, where even a stray opening quote is an apostrophe
        }

        return inNumber && c is '.' or ',' && inNumber && char.IsDigit(text[index + 1]);
    }

    // A lone capital followed by a full stop is an initial ("J. Smith", "J.R.R."), except the words "I" and "A"
    // unless another initial follows directly.
    private static bool IsInitial(string text, int start, int length)
    {
        if (length != 1 || !char.IsUpper(text[start]) || start + 1 >= text.Length || text[start + 1] != '.')
        {
            return false;
        }

        if (start > 0 && text[start - 1] == '.' && start > 1 && char.IsUpper(text[start - 2]))
        {
            return true; // the later letters of "J.R.R."
        }

        bool anotherFollows = start + 3 < text.Length && char.IsUpper(text[start + 2]) && text[start + 3] == '.'
            || start + 4 < text.Length && text[start + 2] == ' ' && char.IsUpper(text[start + 3]) && text[start + 4] == '.';
        if (text[start] is 'I' or 'A')
        {
            return anotherFollows;
        }

        // "J. Smith": a space and a capitalized word follow. At the very end of a paragraph it is just a letter.
        return anotherFollows || (start + 3 < text.Length && text[start + 2] == ' ' && char.IsUpper(text[start + 3]));
    }

    private static int PunctuationLength(string text, int start)
    {
        char c = text[start];
        if (c == '.' || CharClass.IsHyphen(c))
        {
            // "..." is one ellipsis and "--" is one dash.
            int i = start + 1;
            while (i < text.Length && text[i] == c)
            {
                i++;
            }

            return i - start;
        }

        return char.IsHighSurrogate(c) && start + 1 < text.Length && char.IsLowSurrogate(text[start + 1]) ? 2 : 1;
    }

    private static int UrlLength(string text, int start)
    {
        bool boundary = start == 0 || !CharClass.IsWordChar(text, start - 1);
        if (!boundary)
        {
            return 0;
        }

        ReadOnlySpan<char> rest = text.AsSpan(start);
        bool url = rest.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || rest.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || (rest.StartsWith("www.", StringComparison.OrdinalIgnoreCase) && rest.Length > 4 && char.IsLetterOrDigit(rest[4]));
        if (!url)
        {
            return 0;
        }

        int end = start;
        while (end < text.Length && !char.IsWhiteSpace(text[end]) && !CharClass.IsDoubleQuote(text[end]) && text[end] is not ('<' or '>'))
        {
            end++;
        }

        // Punctuation that ends the sentence is not part of the address.
        while (end > start && text[end - 1] is '.' or ',' or ';' or ':' or '!' or '?' or ')' or '\'' or CharClass.RightSingleQuote)
        {
            end--;
        }

        return end - start;
    }

    private static int EmailLength(string text, int start)
    {
        if (!char.IsAsciiLetterOrDigit(text[start]) || (start > 0 && CharClass.IsWordChar(text, start - 1)))
        {
            return 0;
        }

        int i = start;
        while (i < text.Length && (char.IsAsciiLetterOrDigit(text[i]) || text[i] is '.' or '_' or '%' or '+' or '-'))
        {
            i++;
        }

        if (i >= text.Length || text[i] != '@')
        {
            return 0;
        }

        int domainStart = ++i;
        int lastDot = -1;
        while (i < text.Length && (char.IsAsciiLetterOrDigit(text[i]) || text[i] is '.' or '-'))
        {
            if (text[i] == '.')
            {
                lastDot = i;
            }

            i++;
        }

        while (i > domainStart && text[i - 1] is '.' or '-')
        {
            i--;
            if (lastDot == i)
            {
                lastDot = text.LastIndexOf('.', i - 1, i - domainStart);
            }
        }

        bool validDomain = lastDot > domainStart && lastDot < i - 1;
        return validDomain ? i - start : 0;
    }
}
