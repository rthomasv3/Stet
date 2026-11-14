using System.Globalization;

namespace FictionChecker.Segmentation;

/// <summary>Character tests shared by the splitters.</summary>
internal static class CharClass
{
    public const char LeftSingleQuote = (char)0x2018;
    public const char RightSingleQuote = (char)0x2019;
    public const char LeftDoubleQuote = (char)0x201C;
    public const char RightDoubleQuote = (char)0x201D;
    public const char EmDash = (char)0x2014;
    public const char EnDash = (char)0x2013;
    public const char HorizontalBar = (char)0x2015;
    public const char Ellipsis = (char)0x2026;
    public const char Interrobang = (char)0x203D;
    public const char Hyphen = (char)0x2010;
    public const char NonBreakingHyphen = (char)0x2011;

    /// <summary>Straight or curly apostrophe (the curly one doubles as a closing single quote).</summary>
    public static bool IsApostrophe(char c) => c is '\'' or RightSingleQuote;

    /// <summary>Any single quote mark: straight, opening curly or closing curly.</summary>
    public static bool IsSingleQuote(char c) => c is '\'' or LeftSingleQuote or RightSingleQuote;

    /// <summary>Any double quote mark: straight, opening curly or closing curly.</summary>
    public static bool IsDoubleQuote(char c) => c is '"' or LeftDoubleQuote or RightDoubleQuote;

    /// <summary>A hyphen that joins parts of a word (not a dash).</summary>
    public static bool IsHyphen(char c) => c is '-' or Hyphen or NonBreakingHyphen;

    /// <summary>An em dash, en dash or horizontal bar.</summary>
    public static bool IsDash(char c) => c is EmDash or EnDash or HorizontalBar;

    /// <summary>
    /// Length in UTF-16 units of the letter at <paramref name="index"/>, counting any combining accents that
    /// follow it, or 0 when there is no letter there.
    /// </summary>
    public static int LetterLength(string text, int index)
    {
        if (index >= text.Length)
        {
            return 0;
        }

        int length;
        if (char.IsLetter(text[index]))
        {
            length = 1;
        }
        else if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLetter(text, index))
        {
            length = 2;
        }
        else
        {
            return 0;
        }

        while (index + length < text.Length && IsCombiningMark(text[index + length]))
        {
            length++;
        }

        return length;
    }

    /// <summary>Whether the character at <paramref name="index"/> starts a letter or is a digit.</summary>
    public static bool IsWordChar(string text, int index) =>
        index >= 0 && index < text.Length && (char.IsDigit(text[index]) || LetterLength(text, index) > 0 || IsCombiningMark(text[index]) || char.IsLowSurrogate(text[index]));

    private static bool IsCombiningMark(char c) =>
        char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;
}
