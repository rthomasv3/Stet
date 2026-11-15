using Stet.Segmentation;

namespace Stet.Grammar.Rules;

/// <summary>A space before a comma or a full stop, or two commas in a row.</summary>
internal sealed class PunctuationSpacingRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.PunctuationSpacing,
        "Punctuation spacing",
        "Flags a space between a word and the comma or full stop after it, and two commas in a row. Spaced-out dots (\". . .\") are left alone.",
        RuleCategory.Mechanics,
        true,
        false,
        ["He opened the door , and went in.", "He looked up,, then looked away.", "She waited, , and then left.", "The well was dry . They moved on.", "He said nothing ."],
        ["He opened the door, and went in.", "He waited . . . , then spoke.", "“Well —, I suppose so.”", "He waited . . . and then spoke.", "She paused . . .", "He carried a .45 on his hip.", "Save it as notes .txt if you like."]);

    public override RuleInfo Info => Description;

    public override void CheckParagraph(Paragraph paragraph, List<Finding> findings)
    {
        string text = paragraph.Text;
        for (int k = 1; k < paragraph.Tokens.Count; k++)
        {
            if (paragraph.IsMark(k, '.'))
            {
                FullStop(paragraph, k, findings);
                continue;
            }

            if (!paragraph.IsMark(k, ','))
            {
                continue;
            }

            Token comma = paragraph.Tokens[k];
            Token before = paragraph.Tokens[k - 1];
            if (comma.Has(TokenFlags.Skipped) || before.Has(TokenFlags.Skipped))
            {
                continue;
            }

            int gap = comma.Start - before.End;
            bool onlySpaces = gap == 0 || paragraph.OnlySpacesBetween(before.End, comma.Start);
            if (paragraph.IsMark(k - 1, ',') && onlySpaces)
            {
                // Two commas: drop the second and any space between.
                findings.Add(new Finding(before.End, comma.End - before.End, "Two commas in a row.", "One comma is enough here.", [Replace(before.End, comma.End - before.End, string.Empty)]));
            }
            else if (gap > 0 && onlySpaces && before.Kind is TokenKind.Word or TokenKind.Number)
            {
                findings.Add(new Finding(before.End, gap + 1, "Space before a comma.", "A comma follows the word before it with no space between.", [Replace(before.End, gap + 1, ",")]));
            }
        }
    }

    // "dry . They": a lone full stop set off from the word before it. Not one of a row of dots, and not the
    // start of something written with a leading dot (".45", ".txt").
    private static void FullStop(Paragraph paragraph, int k, List<Finding> findings)
    {
        string text = paragraph.Text;
        Token stop = paragraph.Tokens[k];
        Token before = paragraph.Tokens[k - 1];
        int gap = stop.Start - before.End;
        if (gap == 0 || before.Kind != TokenKind.Word || stop.Has(TokenFlags.Skipped) || before.Has(TokenFlags.Skipped) || !paragraph.OnlySpacesBetween(before.End, stop.Start))
        {
            return;
        }

        if (stop.End < text.Length && !char.IsWhiteSpace(text[stop.End]) && !CharClass.IsDoubleQuote(text[stop.End]) && !CharClass.IsSingleQuote(text[stop.End]) && text[stop.End] is not (')' or ']'))
        {
            return;
        }

        if (paragraph.IsMark(k + 1, '.') || (k + 1 < paragraph.Tokens.Count && text[paragraph.Tokens[k + 1].Start] is '.' or CharClass.Ellipsis))
        {
            return;
        }

        findings.Add(new Finding(before.End, gap + 1, "Space before a full stop.", "A full stop follows the word before it with no space between.", [Replace(before.End, gap + 1, ".")]));
    }
}
