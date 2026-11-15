using Stet.Segmentation;

namespace Stet.Grammar.Rules;

/// <summary>A sentence that starts with a lowercase letter: "He left. she stayed."</summary>
internal sealed class SentenceCapitalRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.SentenceCapital,
        "Sentence capitals",
        "Flags a sentence that starts with a lowercase letter after a full stop. The first word of a paragraph is not checked, because lines of verse and paragraphs that carry on after a break often start in lowercase. Abbreviations (\"etc.\", \"Mr.\"), trailing-off dots and dialogue tags (“Why?” she asked) are not sentence starts.",
        RuleCategory.Mechanics,
        true,
        false,
        ["He left. she stayed.", "the door was open.", "The lords gathered at the end. the singing began.", "“Go.” he turned away.", "The road was long. it wound through the hills."],
        ["He left. She stayed.", "“Why?” she asked.", "He waited... and waited.", "They sold nails, rope, etc. and little else.", "It cost approx. five crowns.", "The Louis XV. clock struck.", "See chap. ii. for more.", "Ten per cent. of the men were sick.", "She wrote: no. not now. not ever.", "“you are a long walker. you find me here,” she said."]);

    // Only abbreviations that are not also everyday words: "long.", "art." and "par." far more often end a
    // sentence ("The road was long.") than stand for longitude, article or paragraph.
    private static readonly HashSet<string> MoreAbbreviations = new(StringComparer.Ordinal)
    {
        "chap", "ch", "sec", "op", "pg", "pgs", "deg", "lat", "cit", "ibid", "al", "nat", "amer", "ser", "ex", "gen", "div", "pt", "ca", "c", "v",
    };

    public override RuleInfo Info => Description;

    public override void CheckParagraph(Paragraph paragraph, List<Finding> findings)
    {
        string text = paragraph.Text;
        HashSet<int>? tagStarts = null;
        int firstFinding = findings.Count;
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
                // First word of the paragraph. It starts a sentence unless the paragraph before stopped part way
                // through one (a line of verse, or prose carried on after an inserted letter).
                starts = !paragraph.ContinuesPrevious;
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

            if (!starts || !IsOrdinaryWord(paragraph, k) || SpeechStartsLowercase(paragraph, token.Start))
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

        // One missing capital is a slip. Two or more in a paragraph is a style: a character or a letter
        // written without capitals. Leave those alone.
        if (findings.Count - firstFinding >= 2)
        {
            findings.RemoveRange(firstFinding, findings.Count - firstFinding);
        }
    }

    // Whether the word lies in speech that itself opens with a lowercase letter: a voice written without
    // capitals on purpose.
    private static bool SpeechStartsLowercase(Paragraph paragraph, int position)
    {
        if (paragraph.Dialogue is null)
        {
            return false;
        }

        foreach (TextSpan span in paragraph.Dialogue.Spans)
        {
            if (span.Contains(position))
            {
                int first = paragraph.TokenAtOrAfter(span.Start);
                while (first < paragraph.Tokens.Count && paragraph.Tokens[first].Kind == TokenKind.Punctuation)
                {
                    first++;
                }

                return first < paragraph.Tokens.Count && paragraph.Tokens[first].Start < span.End && char.IsLower(paragraph.Text[paragraph.Tokens[first].Start]);
            }
        }

        return false;
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
        if (word == "cent")
        {
            // "ten per cent. of them" in older books; otherwise an ordinary word ("It cost one cent.").
            return !(j >= 2 && paragraph.IsWord(j - 2) && paragraph.Lower(j - 2) == "per");
        }

        return !WordLists.TitleAbbreviations.Contains(word) && !WordLists.OtherAbbreviations.Contains(word) && !MoreAbbreviations.Contains(word)
            && !word.Contains('.', StringComparison.Ordinal) && !IsRomanNumeral(word);
    }

    // "Louis XV. clock", "chap. ii., verse 25": older books put a full stop after a numeral.
    private static bool IsRomanNumeral(string word) => word.AsSpan().IndexOfAnyExcept("ivxlcdm") < 0;

    // Lowercase on purpose: "iPhone", "eBay", a custom word added in lowercase-with-capitals, a lone letter.
    private static bool IsOrdinaryWord(Paragraph paragraph, int k)
    {
        Token token = paragraph.Tokens[k];
        ReadOnlySpan<char> written = token.TextIn(paragraph.Text);
        if ((written.Length < 2 && !(written.Length == 1 && written[0] == 'a')) || IsRomanNumeral(written.ToString()))
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
