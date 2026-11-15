using Stet.Eval.Text;

namespace Stet.Eval.Planting;

/// <summary>Doubles a word ("the the").</summary>
public sealed class RepeatedWordPlanter : IErrorPlanter
{
    // Doubling these can be correct English ("had had", "that that"), so they are not planted.
    private static readonly HashSet<string> ValidRepeats = new(StringComparer.OrdinalIgnoreCase) { "had", "that", "is", "do", "her", "it", "you" };

    /// <inheritdoc />
    public string Category => "repeated-word";

    /// <inheritdoc />
    public PlantedError? TryPlant(string paragraph, Rng rng)
    {
        List<Token> tokens = Words.Tokenize(paragraph);
        var candidates = new List<int>();
        for (int i = 0; i + 1 < tokens.Count; i++)
        {
            Token token = tokens[i];
            bool spaceAfter = paragraph[token.End] == ' ' && tokens[i + 1].Start == token.End + 1;
            bool alreadyRepeated = string.Equals(tokens[i + 1].Text, token.Text, StringComparison.OrdinalIgnoreCase)
                || (i > 0 && string.Equals(tokens[i - 1].Text, token.Text, StringComparison.OrdinalIgnoreCase));
            if (spaceAfter && !alreadyRepeated && token.Length >= 2 && !ValidRepeats.Contains(token.Text))
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        Token word = tokens[rng.Pick(candidates)];
        string copy = word.Text;
        string text = string.Concat(paragraph.AsSpan(0, word.End), " ", copy, paragraph.AsSpan(word.End));
        return new PlantedError(text, word.Start, word.Length + 1 + copy.Length, word.Start, word.Length, "double");
    }
}
