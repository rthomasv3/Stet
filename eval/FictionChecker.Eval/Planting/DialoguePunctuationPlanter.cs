using FictionChecker.Eval.Text;

namespace FictionChecker.Eval.Planting;

/// <summary>
/// Breaks the punctuation between a line of speech and its tag: <c>,” she said</c> becomes
/// <c>.” she said</c>, <c>,” She said</c> or <c>” she said</c>.
/// </summary>
public sealed class DialoguePunctuationPlanter : IErrorPlanter
{
    private static readonly HashSet<string> TagVerbs = new(StringComparer.Ordinal)
    {
        "said", "asked", "replied", "answered", "whispered", "muttered", "murmured", "shouted", "cried", "called",
        "added", "continued", "remarked", "exclaimed", "demanded", "growled", "snapped", "repeated", "observed",
        "inquired", "declared", "began", "returned", "says", "yelled", "hissed", "mumbled", "agreed", "explained",
    };

    private static readonly HashSet<string> Pronouns = new(StringComparer.Ordinal) { "he", "she", "they", "we", "it" };

    /// <inheritdoc />
    public string Category => "dialogue-punctuation";

    /// <inheritdoc />
    public PlantedError? TryPlant(string paragraph, Rng rng)
    {
        List<Token> tokens = Words.Tokenize(paragraph);
        var candidates = new List<(int Comma, int First, int Verb)>();
        for (int i = 0; i < tokens.Count; i++)
        {
            // Looking for: comma, closing double quote, space, then "said X" or "X said".
            int start = tokens[i].Start;
            if (start < 3 || paragraph[start - 1] != ' ' || paragraph[start - 2] is not ('"' or '”') || paragraph[start - 3] != ',')
            {
                continue;
            }

            if (TagVerbs.Contains(tokens[i].Text))
            {
                candidates.Add((start - 3, i, i));
            }
            else if (i + 1 < tokens.Count
                && tokens[i + 1].Start == tokens[i].End + 1
                && TagVerbs.Contains(tokens[i + 1].Text)
                && (Pronouns.Contains(tokens[i].Text) || char.IsUpper(tokens[i].Text[0])))
            {
                candidates.Add((start - 3, i, i + 1));
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        (int comma, int first, int verb) = rng.Pick(candidates);
        Token firstWord = tokens[first];
        int regionEnd = tokens[verb].End;
        bool canCapitalize = char.IsLower(firstWord.Text[0]);
        int choice = rng.Next(canCapitalize ? 3 : 2);

        string text;
        string kind;
        int shift = 0;
        if (choice == 0)
        {
            text = string.Concat(paragraph.AsSpan(0, comma), ".", paragraph.AsSpan(comma + 1));
            kind = "full-stop-before-tag";
        }
        else if (choice == 1)
        {
            text = paragraph.Remove(comma, 1);
            kind = "missing-comma";
            shift = -1;
        }
        else
        {
            string capital = char.ToUpperInvariant(firstWord.Text[0]) + firstWord.Text[1..];
            text = string.Concat(paragraph.AsSpan(0, firstWord.Start), capital, paragraph.AsSpan(firstWord.End));
            kind = "capitalized-tag";
        }

        // A flag anywhere from the punctuation to the tag verb counts.
        int regionStart = choice == 1 ? comma - 1 : comma;
        return new PlantedError(text, regionStart, regionEnd + shift - regionStart, regionStart, regionEnd - regionStart, kind);
    }
}
