using FictionChecker.Segmentation;
using FictionChecker.Spelling;

namespace FictionChecker.Grammar;

/// <summary>A whole paragraph as paragraph-level rules see it: its text, tokens and where the speech is.</summary>
internal sealed class Paragraph(string text, List<Token> tokens, DialogueResult? dialogue, QuoteStyle quoteStyle, CustomWords custom)
{
    public string Text { get; } = text;

    public List<Token> Tokens { get; } = tokens;

    public DialogueResult? Dialogue { get; } = dialogue;

    /// <summary>Which quote marks delimit speech.</summary>
    public QuoteStyle QuoteStyle { get; } = quoteStyle;

    /// <summary>The writer's own words.</summary>
    public CustomWords Custom { get; } = custom;

    /// <summary>Whether a token is a word the caller did not ask to skip.</summary>
    public bool IsWord(int i) => (uint)i < (uint)Tokens.Count && Tokens[i].Kind == TokenKind.Word && !Tokens[i].Has(TokenFlags.Skipped);

    /// <summary>A token in lowercase with straight apostrophes.</summary>
    public string Lower(int i)
    {
        Span<char> buffer = stackalloc char[Math.Min(Tokens[i].Length, 64)];
        return Tagger.Normalize(Tokens[i].TextIn(Text)[..buffer.Length], buffer).ToString().ToLowerInvariant();
    }

    /// <summary>Whether a token is a single punctuation character, this one.</summary>
    public bool IsMark(int i, char mark) =>
        (uint)i < (uint)Tokens.Count && Tokens[i].Kind == TokenKind.Punctuation && Tokens[i].Length == 1 && Text[Tokens[i].Start] == mark;

    /// <summary>Whether only spaces lie between the end of one position and the start of a token.</summary>
    public bool OnlySpacesBetween(int end, int start) =>
        start > end && Text.AsSpan(end, start - end).IndexOfAnyExcept(' ', (char)0xA0, '\t') < 0;

    /// <summary>The first token starting at or after a position, or the token count.</summary>
    public int TokenAtOrAfter(int position)
    {
        int low = 0;
        int high = Tokens.Count;
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (Tokens[mid].Start < position)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }
}
