using FictionChecker.Segmentation;

namespace FictionChecker.Grammar.Rules;

/// <summary>A space before a comma, or two commas in a row.</summary>
internal sealed class CommaSpacingRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.CommaSpacing,
        "Comma spacing",
        "Flags a space between a word and the comma after it, and two commas in a row.",
        RuleCategory.Mechanics,
        true,
        false,
        ["He opened the door , and went in.", "He looked up,, then looked away.", "She waited, , and then left."],
        ["He opened the door, and went in.", "He waited . . . , then spoke.", "“Well —, I suppose so.”"]);

    public override RuleInfo Info => Description;

    public override void CheckParagraph(Paragraph paragraph, List<Finding> findings)
    {
        string text = paragraph.Text;
        for (int k = 1; k < paragraph.Tokens.Count; k++)
        {
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
}
