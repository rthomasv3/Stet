using FictionChecker.Eval.Text;

namespace FictionChecker.Eval.Planting;

/// <summary>
/// Plants a single-slip typo in a word: a dropped, added, wrong or swapped letter, or a keyboard-neighbour hit.
/// Only plain lowercase words of five letters or more are used, which keeps accidental real words rare.
/// </summary>
public sealed class TypoPlanter : IErrorPlanter
{
    private const string Letters = "abcdefghijklmnopqrstuvwxyz";

    private static readonly string[] KeyboardRows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"];

    /// <inheritdoc />
    public string Category => "typo";

    /// <inheritdoc />
    public PlantedError? TryPlant(string paragraph, Rng rng)
    {
        List<Token> candidates = Words.Tokenize(paragraph)
            .Where(t => t.Length >= 5 && t.Text.All(c => c is >= 'a' and <= 'z'))
            .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        Token word = rng.Pick(candidates);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            (string typo, string kind) = Mutate(word.Text, rng);
            if (typo != word.Text)
            {
                string text = string.Concat(paragraph.AsSpan(0, word.Start), typo, paragraph.AsSpan(word.End));
                return new PlantedError(text, word.Start, typo.Length, word.Start, word.Length, kind);
            }
        }

        return null;
    }

    private static (string Typo, string Kind) Mutate(string word, Rng rng)
    {
        // The first letter is left alone: most real typos are inside the word.
        int i = 1 + rng.Next(word.Length - 1);
        switch (rng.Next(5))
        {
            case 0:
                return (word.Remove(i, 1), "delete");
            case 1:
                return (word.Insert(i, Letters[rng.Next(26)].ToString()), "insert");
            case 2:
                return (Replace(word, i, Letters[rng.Next(26)]), "substitute");
            case 3:
                if (i == word.Length - 1)
                {
                    i--;
                }

                char[] chars = word.ToCharArray();
                (chars[i], chars[i + 1]) = (chars[i + 1], chars[i]);
                return (new string(chars), "transpose");
            default:
                return (Replace(word, i, Neighbour(word[i], rng)), "keyboard");
        }
    }

    private static string Replace(string word, int index, char c) =>
        string.Concat(word.AsSpan(0, index), c.ToString(), word.AsSpan(index + 1));

    private static char Neighbour(char c, Rng rng)
    {
        foreach (string row in KeyboardRows)
        {
            int at = row.IndexOf(c, StringComparison.Ordinal);
            if (at < 0)
            {
                continue;
            }

            if (at == 0)
            {
                return row[1];
            }

            if (at == row.Length - 1)
            {
                return row[at - 1];
            }

            return rng.Next(2) == 0 ? row[at - 1] : row[at + 1];
        }

        return c;
    }
}
