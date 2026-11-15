using Stet.Segmentation;

namespace Stet.Grammar.Rules;

/// <summary>The word "I" written as a lowercase "i".</summary>
internal sealed class LowercaseIRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.LowercaseI,
        "Lowercase I",
        "Flags the word \"I\" written without its capital, alone or in \"i'm\", \"i'll\", \"i've\" and \"i'd\". Numbering (\"i.\", \"(i)\", \"chapter i\") is left alone, and so is a paragraph that does it more than once, which is a voice written without capitals on purpose.",
        RuleCategory.Mechanics,
        true,
        false,
        ["Then i saw the smoke.", "She knew i'm not one to complain.", "“Am i late?” he asked."],
        ["Then I saw the smoke.", "See chapter i for more.", "There were two reasons: (i) the cold, and (ii) the dark.", "He wrote: i am here. i am waiting.", "That is, i.e. the usual way.", "Heels i’ the air!"]);

    private static readonly string[] Numbered = ["chapter", "part", "book", "act", "scene", "volume", "vol", "section", "canto", "page", "figure", "appendix"];

    public override RuleInfo Info => Description;

    public override void CheckParagraph(Paragraph paragraph, List<Finding> findings)
    {
        string text = paragraph.Text;
        int first = findings.Count;
        for (int k = 0; k < paragraph.Tokens.Count; k++)
        {
            Token token = paragraph.Tokens[k];
            if (!paragraph.IsWord(k) || text[token.Start] != 'i' || token.Length > 4)
            {
                continue;
            }

            string lower = paragraph.Lower(k);
            if (lower is not ("i" or "i'm" or "i'll" or "i've" or "i'd"))
            {
                continue;
            }

            if (lower == "i")
            {
                // A roman numeral or a list letter: "i.", "(i)", "i)", "chapter i", "i-ii".
                char before = token.Start > 0 ? text[token.Start - 1] : ' ';
                char after = token.End < text.Length ? text[token.End] : ' ';
                // Or a clipped "in": "i' the old hall".
                if (after is '.' or ')' or ':' or '-' || before is '(' or '-' || CharClass.IsApostropheVariant(after) || CharClass.IsApostropheVariant(before) || CharClass.IsDash(after) || CharClass.IsDash(before)
                    || (k > 0 && paragraph.IsWord(k - 1) && Array.IndexOf(Numbered, paragraph.Lower(k - 1)) >= 0))
                {
                    continue;
                }
            }

            string fix = "I" + text.Substring(token.Start + 1, token.Length - 1);
            findings.Add(new Finding(
                token.Start,
                token.Length,
                $"{CharClassQuote.Open}{fix}{CharClassQuote.Close} takes a capital.",
                "The word \"I\" is always written with a capital letter.",
                [Replace(token.Start, token.Length, fix)]));
        }

        // Once is a slip; more than once is a way of writing.
        if (findings.Count - first >= 2)
        {
            findings.RemoveRange(first, findings.Count - first);
        }
    }
}
