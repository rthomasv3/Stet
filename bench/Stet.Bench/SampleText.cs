using System.Text;

namespace Stet.Bench;

/// <summary>
/// Made-up prose for the benchmarks: no book text is kept in the repository. It has the things that cost time
/// in real fiction: speech, names that are not in the dictionary, and a few misspellings.
/// </summary>
public static class SampleText
{
    private static readonly string[] Subjects = ["the old ferryman", "she", "he", "the captain", "Kaelith", "Ysolde", "the stranger", "nobody", "the youngest of them", "Vorn"];
    private static readonly string[] Verbs = ["watched", "followed", "remembered", "carried", "crossed", "opened", "refused", "counted", "left", "found"];
    private static readonly string[] Objects = ["the river", "a lantern", "the narrow road", "her father's sword", "the last of the bread", "a letter", "the gate", "three grey horses", "the harbour lights", "an empty room"];
    private static readonly string[] Tails = ["before the rain came", "without a word", "as the bells rang", "and said nothing of it", "while the others slept", "for the second time that night", "until the lamp burned low", "though it cost him dearly"];
    private static readonly string[] Speech = ["We can't stay here", "I never asked for this", "You'll see it by morning", "Nothin' good comes of waiting", "Tell them I'm gonna be late", "Where did you leave it", "It isn't far now"];
    private static readonly string[] Slips = ["beleived", "recieved", "teh", "smal", "definately", "Drathmoor", "Ilvenna", "seperate"];

    /// <summary>The names used in the text, as a writer's word list would hold them.</summary>
    public static IReadOnlyList<string> Names { get; } = ["Kaelith", "Ysolde", "Vorn", "Drathmoor", "Ilvenna"];

    /// <summary>Paragraphs of about <paramref name="wordsEach"/> words, with <paramref name="totalWords"/> in all.</summary>
    public static List<string> Paragraphs(int totalWords, int wordsEach = 75, int seed = 7)
    {
        var random = new Random(seed);
        var paragraphs = new List<string>();
        int words = 0;
        while (words < totalWords)
        {
            var sb = new StringBuilder();
            int count = 0;
            while (count < wordsEach)
            {
                string sentence = Sentence(random);
                sb.Append(sentence).Append(' ');
                count += sentence.Count(c => c == ' ') + 1;
            }

            paragraphs.Add(sb.ToString().TrimEnd());
            words += count;
        }

        return paragraphs;
    }

    /// <summary>Distinct misspellings, each one or two letter changes away from a real word.</summary>
    public static string[] Typos(int count, int seed = 5)
    {
        string[] bases = ["remembered", "definitely", "lantern", "stranger", "followed", "harbour", "captain", "believe", "separate", "narrow", "youngest", "nothing", "morning", "waiting", "second", "burned"];
        var random = new Random(seed);
        var typos = new HashSet<string>(StringComparer.Ordinal);
        while (typos.Count < count)
        {
            char[] word = bases[random.Next(bases.Length)].ToCharArray();
            for (int edits = random.Next(1, 3); edits > 0; edits--)
            {
                word[random.Next(1, word.Length)] = (char)('a' + random.Next(26));
            }

            typos.Add(new string(word));
        }

        return [.. typos];
    }

    private static string Sentence(Random random)
    {
        string Pick(string[] from) => from[random.Next(from.Length)];
        string subject = Pick(Subjects);
        string body = $"{subject} {Pick(Verbs)} {Pick(Objects)} {Pick(Tails)}";
        if (random.Next(40) == 0)
        {
            body += " " + Pick(Slips);
        }

        body = char.ToUpperInvariant(body[0]) + body[1..];
        return random.Next(3) == 0 ? $"“{Pick(Speech)},” {subject} said. {body}." : body + ".";
    }
}
