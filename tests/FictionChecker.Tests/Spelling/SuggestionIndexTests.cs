using FictionChecker.Spelling;

namespace FictionChecker.Tests.Spelling;

public class SuggestionIndexTests
{
    private static Dictionary<string, int> Find(string[] words, string target, int max)
    {
        Array.Sort(words, StringComparer.Ordinal);
        var found = new Dictionary<string, int>();
        new SuggestionIndex(words).Find(target, max, (index, distance) => found[words[index]] = distance);
        return found;
    }

    [Fact]
    public void FindsEachKindOfEdit()
    {
        Dictionary<string, int> found = Find(["cat", "cart", "at", "cot", "act", "dog", "cats", "coat", "scat"], "cat", 1);

        Assert.Equal(
            new Dictionary<string, int> { ["cat"] = 0, ["cart"] = 1, ["at"] = 1, ["cot"] = 1, ["act"] = 1, ["cats"] = 1, ["coat"] = 1, ["scat"] = 1 },
            found);
    }

    [Fact]
    public void RespectsTheDistanceLimit()
    {
        string[] words = ["sword", "words", "world", "swore", "sworn", "ward", "sorted"];

        Assert.Equal(["sword", "swore", "sworn"], Find(words, "sword", 1).Keys.Order());
        Assert.Equal(["sword", "swore", "sworn", "ward", "words", "world"], Find(words, "sword", 2).Keys.Order());
    }

    [Fact]
    public void IgnoresCaseOfTheWords()
    {
        Assert.Equal(1, Find(["London", "lemon"], "lodon", 1)["London"]);
    }

    [Fact]
    public void AgreesWithAPlainComparisonOnTheRealLexicon()
    {
        Lexicon lexicon = Lexicon.LoadEmbedded();
        string[] all = lexicon.Words.Select(w => w.Key).ToArray();
        foreach (string target in new[] { "teh", "recieve", "qwick", "shortswrod", "a", "zzzzzz", "definately" })
        {
            var fast = new HashSet<string>();
            lexicon.Search.Find(target, 2, (index, _) => fast.Add(lexicon.WordAt(index)));

            var slow = all.Where(w => w.Length <= SuggestionIndex.MaxLength && Distance(w.ToLowerInvariant(), target) <= 2).ToHashSet();

            Assert.Equal(slow.Order(StringComparer.Ordinal), fast.Order(StringComparer.Ordinal));
        }
    }

    // Reference edit distance with neighbour swaps (optimal string alignment).
    private static int Distance(string a, string b)
    {
        int[,] d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= b.Length; j++)
        {
            d[0, j] = j;
        }

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                {
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                }
            }
        }

        return d[a.Length, b.Length];
    }
}
