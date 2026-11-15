namespace FictionChecker.Grammar.Rules;

/// <summary>Two or more spaces in a row between words.</summary>
internal sealed class DoubledSpaceRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.DoubledSpace,
        "Doubled spaces",
        "Flags two or more spaces in a row inside a sentence. Extra space after the end of a sentence is left alone, since some writers type two spaces there by habit.",
        RuleCategory.Mechanics,
        true,
        false,
        ["She opened  the door.", "He ran,   then stopped."],
        ["She opened the door.", "She opened the door.  Then she stopped.", "“Go!”  He ran."]);

    public override RuleInfo Info => Description;

    public override void CheckParagraph(Paragraph paragraph, List<Finding> findings)
    {
        string text = paragraph.Text;
        for (int k = 0; k + 1 < paragraph.Tokens.Count; k++)
        {
            var left = paragraph.Tokens[k];
            var right = paragraph.Tokens[k + 1];
            int gap = right.Start - left.End;
            if (gap < 2 || left.Has(Segmentation.TokenFlags.Skipped) || right.Has(Segmentation.TokenFlags.Skipped)
                || text.AsSpan(left.End, gap).ContainsAnyExcept(' '))
            {
                continue;
            }

            if (EndsSentence(text, left.End - 1))
            {
                continue;
            }

            findings.Add(new Finding(left.End, gap, "Extra space.", "There is more than one space here.", [Replace(left.End, gap, " ")]));
        }
    }

    // Whether the character at a position closes a sentence: an end mark, or a closing quote or bracket after one.
    private static bool EndsSentence(string text, int at)
    {
        while (at > 0 && (Segmentation.CharClass.IsDoubleQuote(text[at]) || Segmentation.CharClass.IsSingleQuote(text[at]) || text[at] is ')' or ']'))
        {
            at--;
        }

        return text[at] is '.' or '!' or '?' or ':' or Segmentation.CharClass.Ellipsis;
    }
}
