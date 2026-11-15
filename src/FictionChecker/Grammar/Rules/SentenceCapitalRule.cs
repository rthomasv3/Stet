using FictionChecker.Segmentation;

namespace FictionChecker.Grammar.Rules;

/// <summary>A sentence that starts with a lowercase letter: "He left. she stayed."</summary>
internal sealed class SentenceCapitalRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.SentenceCapital,
        "Sentence capitals",
        "Flags a sentence that starts with a lowercase letter: at the start of a paragraph, or after a full stop. Abbreviations (\"etc.\", \"Mr.\"), trailing-off dots and dialogue tags (“Why?” she asked) are not sentence starts.",
        RuleCategory.Mechanics,
        true,
        false,
        ["He left. she stayed.", "the door was open.", "“the door is open,” she said.", "“Go.” he turned away."],
        ["He left. She stayed.", "“Why?” she asked.", "He waited... and waited.", "They sold nails, rope, etc. and little else.", "It cost approx. five crowns.", "’tis late.", "eBay was down."]);

    public override RuleInfo Info => Description;

    public override void CheckParagraph(Paragraph paragraph, List<Finding> findings)
    {
        string text = paragraph.Text;
        HashSet<int>? tagStarts = null;
        for (int k = 0; k < paragraph.Tokens.Count; k++)
        {
            Token token = paragraph.Tokens[k];
            if (!paragraph.IsWord(k) || !char.IsLower(text[token.Start]) || token.Has(TokenFlags.LeadingApostrophe))
            {
                continue;
            }

            // Look back past opening quote marks and brackets to what really comes before.
            int j = k - 1;
            while (j >= 0 && IsOpener(paragraph, j))
            {
                j--;
            }

            bool starts;
            if (j < 0)
            {
                // First word of the paragraph. A paragraph that carries on a speech from the one before, with
                // no quote mark of its own, may be carrying on a sentence too.
                starts = !(k == 0 && paragraph.Dialogue is not null && paragraph.Dialogue.IsDialogue(token.Start));
            }
            else
            {
                bool afterQuote = false;
                if (IsCloser(paragraph, j) && j > 0 && paragraph.Tokens[j - 1].End == paragraph.Tokens[j].Start)
                {
                    afterQuote = true;
                    j--;
                }

                starts = EndsSentence(paragraph, j) && paragraph.Tokens[j].End < token.Start && text.AsSpan(paragraph.Tokens[j].End, token.Start - paragraph.Tokens[j].End).Contains(' ');
                if (starts && afterQuote)
                {
                    // “Hello.” she said: the slip is the full stop, which another rule reports.
                    tagStarts ??= [.. DialogueTags.Find(paragraph).Select(s => s.First)];
                    starts = !tagStarts.Contains(k);
                }
            }

            if (!starts || !IsOrdinaryWord(paragraph, k))
            {
                continue;
            }

            string written = token.TextIn(text).ToString();
            string fix = char.ToUpperInvariant(written[0]) + written[1..];
            findings.Add(new Finding(
                token.Start,
                token.Length,
                $"A sentence starts with a capital letter: {CharClassQuote.Open}{fix}{CharClassQuote.Close}.",
                "This word starts a sentence.",
                [Replace(token.Start, token.Length, fix)]));
        }
    }

    // A single full stop straight after a word that is not an abbreviation.
    private static bool EndsSentence(Paragraph paragraph, int j)
    {
        if (!paragraph.IsMark(j, '.') || j == 0)
        {
            return false;
        }

        Token before = paragraph.Tokens[j - 1];
        if (before.Kind != TokenKind.Word || before.End != paragraph.Tokens[j].Start || before.Length < 2)
        {
            return false; // "e.g.", an initial, a number, or dots standing apart
        }

        string word = paragraph.Lower(j - 1);
        return !WordLists.TitleAbbreviations.Contains(word) && !WordLists.OtherAbbreviations.Contains(word) && !word.Contains('.', StringComparison.Ordinal);
    }

    // Lowercase on purpose: "iPhone", "eBay", a custom word added in lowercase-with-capitals, a lone letter.
    private static bool IsOrdinaryWord(Paragraph paragraph, int k)
    {
        Token token = paragraph.Tokens[k];
        ReadOnlySpan<char> written = token.TextIn(paragraph.Text);
        if (written.Length < 2 && !(written.Length == 1 && written[0] == 'a'))
        {
            return false;
        }

        foreach (char c in written)
        {
            if (char.IsUpper(c) || char.IsDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsOpener(Paragraph paragraph, int j)
    {
        Token token = paragraph.Tokens[j];
        if (token.Kind != TokenKind.Punctuation || token.Length != 1)
        {
            return false;
        }

        char c = paragraph.Text[token.Start];
        if (c is CharClass.LeftDoubleQuote or CharClass.LeftSingleQuote or '(' or '[')
        {
            return true;
        }

        // A straight quote opens when a space (or nothing) comes before it.
        return c is '"' or '\'' && (token.Start == 0 || char.IsWhiteSpace(paragraph.Text[token.Start - 1]));
    }

    private static bool IsCloser(Paragraph paragraph, int j)
    {
        Token token = paragraph.Tokens[j];
        if (token.Kind != TokenKind.Punctuation || token.Length != 1)
        {
            return false;
        }

        char c = paragraph.Text[token.Start];
        return c is CharClass.RightDoubleQuote or CharClass.RightSingleQuote or '"' or '\'';
    }
}
