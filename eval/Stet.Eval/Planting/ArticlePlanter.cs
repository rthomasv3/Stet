using Stet.Eval.Text;

namespace Stet.Eval.Planting;

/// <summary>Flips "a" and "an" before a word.</summary>
public sealed class ArticlePlanter : IErrorPlanter
{
    /// <inheritdoc />
    public string Category => "a-an";

    /// <inheritdoc />
    public PlantedError? TryPlant(string paragraph, Rng rng)
    {
        List<Token> tokens = Words.Tokenize(paragraph);
        var candidates = new List<int>();
        for (int i = 0; i + 1 < tokens.Count; i++)
        {
            string word = tokens[i].Text;
            bool article = word is "a" or "an" or "A" or "An";
            bool wordFollows = tokens[i + 1].Start == tokens[i].End + 1 && paragraph[tokens[i].End] == ' ';
            if (article && wordFollows)
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        int at = rng.Pick(candidates);
        Token token = tokens[at];
        Token next = tokens[at + 1];
        string flipped = token.Text switch
        {
            "a" => "an",
            "A" => "An",
            "an" => "a",
            _ => "A",
        };
        string text = string.Concat(paragraph.AsSpan(0, token.Start), flipped, paragraph.AsSpan(token.End));
        int shift = flipped.Length - token.Length;
        return new PlantedError(
            text,
            token.Start,
            next.End + shift - token.Start,
            token.Start,
            next.End - token.Start,
            $"{token.Text.ToLowerInvariant()}>{flipped.ToLowerInvariant()}");
    }
}
