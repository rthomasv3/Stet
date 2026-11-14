namespace FictionChecker.Eval.Text;

/// <summary>A word in a paragraph, with its position in UTF-16 units.</summary>
public readonly record struct Token(int Start, int Length, string Text)
{
    /// <summary>Position just past the word.</summary>
    public int End => Start + Length;
}

/// <summary>
/// A simple word splitter for the harness (counting words, planting errors). It is independent of the
/// library's own splitter on purpose, so a mistake there cannot hide itself in the measurements.
/// </summary>
public static class Words
{
    /// <summary>Splits text into words: letters, with apostrophes and hyphens allowed between letters.</summary>
    public static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < text.Length)
        {
            if (!char.IsLetter(text[i]))
            {
                i++;
                continue;
            }

            int start = i;
            while (i < text.Length)
            {
                if (char.IsLetter(text[i]))
                {
                    i++;
                }
                else if (IsJoiner(text[i]) && i + 1 < text.Length && char.IsLetter(text[i + 1]))
                {
                    i += 2;
                }
                else
                {
                    break;
                }
            }

            tokens.Add(new Token(start, i - start, text[start..i]));
        }

        return tokens;
    }

    /// <summary>Counts the words in a paragraph.</summary>
    public static int Count(string text)
    {
        int count = 0;
        bool inWord = false;
        foreach (char c in text)
        {
            bool letter = char.IsLetter(c) || char.IsDigit(c);
            if (letter && !inWord)
            {
                count++;
            }

            if (letter)
            {
                inWord = true;
            }
            else if (!IsJoiner(c))
            {
                inWord = false;
            }
        }

        return count;
    }

    /// <summary>Whether a character is a straight or curly apostrophe.</summary>
    public static bool IsApostrophe(char c) => c is '\'' or '’';

    private static bool IsJoiner(char c) => IsApostrophe(c) || c == '-';
}
