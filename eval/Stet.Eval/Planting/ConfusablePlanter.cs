using Stet.Eval.Text;

namespace Stet.Eval.Planting;

/// <summary>Swaps a commonly confused word for another of its set (their → there).</summary>
public sealed class ConfusablePlanter : IErrorPlanter
{
    // The V1 candidate list from the requirements.
    private static readonly string[][] Sets =
    [
        ["their", "there", "they're"], ["its", "it's"], ["your", "you're"], ["whose", "who's"],
        ["then", "than"], ["lose", "loose"], ["affect", "effect"], ["accept", "except"],
        ["to", "too"], ["of", "off"], ["lead", "led"], ["past", "passed"],
        ["breath", "breathe"], ["quite", "quiet"], ["peek", "peak", "pique"],
        ["wander", "wonder"], ["desert", "dessert"], ["brake", "break"],
        ["principal", "principle"], ["compliment", "complement"],
    ];

    private static readonly Dictionary<string, int> SetOf = BuildIndex();

    /// <inheritdoc />
    public string Category => "confusable";

    /// <inheritdoc />
    public PlantedError? TryPlant(string paragraph, Rng rng)
    {
        var bySet = new SortedDictionary<int, List<Token>>();
        foreach (Token token in Words.Tokenize(paragraph))
        {
            if (SetOf.TryGetValue(Casing.Normalize(token.Text), out int set))
            {
                if (!bySet.TryGetValue(set, out List<Token>? list))
                {
                    bySet[set] = list = [];
                }

                list.Add(token);
            }
        }

        if (bySet.Count == 0)
        {
            return null;
        }

        // Pick the set first, so the very common words (to, of) do not crowd out the rest.
        int chosenSet = rng.Pick(bySet.Keys.ToList());
        Token word = rng.Pick(bySet[chosenSet]);
        string normalized = Casing.Normalize(word.Text);
        string other = rng.Pick(Sets[chosenSet].Where(w => w != normalized).ToList());
        string replacement = Casing.MatchApostrophe(word.Text, Casing.Match(word.Text, other));
        if (!word.Text.Contains('’') && paragraph.Contains('’'))
        {
            replacement = replacement.Replace('\'', '’');
        }

        string text = string.Concat(paragraph.AsSpan(0, word.Start), replacement, paragraph.AsSpan(word.End));
        return new PlantedError(text, word.Start, replacement.Length, word.Start, word.Length, $"{normalized}>{other}");
    }

    private static Dictionary<string, int> BuildIndex()
    {
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < Sets.Length; i++)
        {
            foreach (string word in Sets[i])
            {
                index[word] = i;
            }
        }

        return index;
    }
}
