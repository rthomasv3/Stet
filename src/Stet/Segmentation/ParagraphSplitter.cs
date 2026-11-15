namespace Stet.Segmentation;

/// <summary>Splits a block of text into paragraphs.</summary>
internal static class ParagraphSplitter
{
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;

    /// <summary>
    /// Returns the range of each paragraph in <paramref name="text"/>. A paragraph is a line; blank lines and
    /// surrounding whitespace are left out. Positions refer to the original text.
    /// </summary>
    public static List<TextSpan> Split(string text)
    {
        var paragraphs = new List<TextSpan>();
        int lineStart = 0;
        for (int i = 0; i <= text.Length; i++)
        {
            if (i < text.Length && text[i] is not ('\n' or '\r' or LineSeparator or ParagraphSeparator))
            {
                continue;
            }

            int start = lineStart;
            int end = i;
            while (start < end && char.IsWhiteSpace(text[start]))
            {
                start++;
            }

            while (end > start && char.IsWhiteSpace(text[end - 1]))
            {
                end--;
            }

            if (end > start)
            {
                paragraphs.Add(TextSpan.FromBounds(start, end));
            }

            lineStart = i + 1;
        }

        return paragraphs;
    }
}
