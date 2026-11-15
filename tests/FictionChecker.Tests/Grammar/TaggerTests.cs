using FictionChecker.Grammar;
using FictionChecker.Segmentation;
using FictionChecker.Spelling;

namespace FictionChecker.Tests.Grammar;

public class TaggerTests
{
    private static readonly Lexicon Words = Lexicon.LoadEmbedded();
    private static readonly WordData Data = WordData.LoadEmbedded(Words);
    private static readonly Tagger Tagger = new(Data);

    private static readonly Dictionary<string, WordType> Names = new(StringComparer.Ordinal)
    {
        ["N"] = WordType.Noun, ["V"] = WordType.Verb, ["J"] = WordType.Adjective, ["R"] = WordType.Adverb,
        ["P"] = WordType.Pronoun, ["D"] = WordType.Determiner, ["I"] = WordType.Adposition, ["M"] = WordType.Number,
        ["C"] = WordType.Conjunction, ["T"] = WordType.Particle, ["X"] = WordType.Other,
    };

    // Hand-tagged sentences written for this project. Each word is followed by its type, in the habits of the
    // Google Books tagging: "to" before a verb is a particle (T), possessives like "her" and "my" are pronouns
    // (P), and "that" introducing a clause is an adposition (I). Punctuation is left untagged.
    private static readonly string[] Tagged =
    [
        "The/D old/J man/N walked/V slowly/R down/I the/D road/N .",
        "She/P opened/V the/D door/N and/C looked/V out/T .",
        "I/P can/V see/V the/D can/N on/I the/D shelf/N .",
        "They/P will/V leave/V in/I the/D morning/N .",
        "He/P had/V left/V his/P keys/N in/I the/D car/N .",
        "The/D wind/N was/V cold/J and/C the/D sky/N was/V dark/J .",
        "We/P need/V to/T wind/V the/D clock/N .",
        "A/D light/N burned/V in/I the/D window/N .",
        "The/D box/N was/V light/J enough/R to/T carry/V .",
        "Her/P answer/N surprised/V him/P .",
        "Nobody/N would/V answer/V the/D question/N .",
        "The/D rain/N fell/V all/D night/N .",
        "It/P might/V rain/V before/I dark/N .",
        "He/P watched/V the/D boats/N sail/V away/R .",
        "The/D sail/N tore/V in/I the/D storm/N .",
        "She/P gave/V him/P a/D quick/J smile/N .",
        "They/P smile/V at/I each/D other/J .",
        "The/D captain/N ordered/V the/D men/N to/T row/V faster/R .",
        "My/P brother/N lives/V near/I the/D river/N .",
        "Many/J lives/N were/V lost/V that/D winter/N .",
        "He/P knew/V that/I she/P was/V right/J .",
        "Turn/V right/R at/I the/D corner/N .",
        "The/D children/N were/V playing/V in/I the/D garden/N .",
        "Running/V water/N could/V be/V heard/V below/R .",
        "She/P is/V very/R tired/J of/I waiting/V .",
        "The/D book/N that/D he/P wanted/V was/V gone/V .",
        "We/P walked/V for/I three/M hours/N without/I rest/N .",
        "You/P should/V rest/V now/R .",
        "The/D guard/N did/V not/R move/V .",
        "His/P hands/N were/V shaking/V badly/R .",
        "A/D small/J brown/J dog/N followed/V them/P home/R .",
        "Nothing/N in/I the/D house/N had/V changed/V .",
        "The/D change/N in/I her/P voice/N was/V sudden/J .",
        "He/P drank/V the/D water/N and/C sat/V down/T .",
        "Their/P horses/N were/V tired/J but/C they/P rode/V on/T .",
        "The/D fire/N had/V burned/V low/J .",
        "Fire/V when/R ready/J .",
        "She/P reached/V for/I the/D sword/N at/I her/P side/N .",
        "The/D queen/N smiled/V and/C said/V nothing/N .",
        "Snow/N covered/V the/D fields/N like/I a/D white/J sheet/N .",
        "I/P like/V the/D way/N you/P think/V .",
        "It/P was/V the/D last/J thing/N he/P expected/V .",
        "The/D meeting/N would/V last/V an/D hour/N .",
        "Her/P eyes/N were/V a/D pale/J green/J .",
        "He/P set/V the/D lamp/N on/I the/D table/N and/C waited/V .",
    ];

    public static IEnumerable<object[]> Sentences => Tagged.Select(s => new object[] { s });

    private static (string Text, List<(string Word, WordType? Expected)> Words) Parse(string tagged)
    {
        var words = tagged.Split(' ').Select(part =>
        {
            int slash = part.LastIndexOf('/');
            return slash < 0 ? (part, (WordType?)null) : (part[..slash], Names[part[(slash + 1)..]]);
        }).ToList();
        return (string.Join(" ", words.Select(w => w.Item1)), words);
    }

    private static WordType[] Tag(string text, CustomWords? custom = null)
    {
        List<Token> tokens = Tokenizer.Tokenize(text);
        var types = new WordType[tokens.Count];
        Tagger.Tag(text, tokens.ToArray(), custom ?? CustomWords.Empty, types, new int[tokens.Count]);
        return types;
    }

    [Fact]
    public void Tags_at_least_nineteen_words_in_twenty_of_the_hand_tagged_sentences()
    {
        int total = 0;
        int right = 0;
        var wrong = new List<string>();
        foreach (string sentence in Tagged)
        {
            (string text, List<(string Word, WordType? Expected)> words) = Parse(sentence);
            WordType[] types = Tag(text);
            Assert.Equal(words.Count, types.Length);
            for (int i = 0; i < words.Count; i++)
            {
                if (words[i].Expected is not { } expected)
                {
                    continue;
                }

                total++;
                if (types[i] == expected)
                {
                    right++;
                }
                else
                {
                    wrong.Add($"{words[i].Word}: {types[i]} not {expected} in \"{text}\"");
                }
            }
        }

        Assert.True(right >= total * 0.95, $"{right} of {total} right. Wrong:\n" + string.Join("\n", wrong));
    }

    [Fact]
    public void Punctuation_and_numbers_get_their_own_types()
    {
        WordType[] types = Tag("He paid 40 crowns, no more.");

        Assert.Equal(WordType.Number, types[2]);
        Assert.Equal(WordType.Punctuation, types[4]);
        Assert.Equal(WordType.Punctuation, types[^1]);
    }

    [Fact]
    public void A_custom_word_is_a_name_even_when_it_is_also_a_verb()
    {
        CustomWords world = CustomWords.Empty.WithSet("world", ["Will", "Kaelith"], WordScope.Everywhere);

        Assert.Equal(WordType.Noun, Tag("Will had declined the offer.", world)[0]);
        Assert.Equal(WordType.Noun, Tag("Kaelith had declined the offer.", world)[0]);
        Assert.Equal(WordType.Verb, Tag("Will you come with us?")[0]);
    }

    [Fact]
    public void A_capitalized_word_inside_a_sentence_is_a_name()
    {
        WordType[] types = Tag("Then she asked Rose and Mark to wait.");

        Assert.Equal(WordType.Noun, types[3]);
        Assert.Equal(WordType.Noun, types[5]);
    }

    [Fact]
    public void Contractions_are_tagged_by_their_parts()
    {
        WordType[] types = Tag("I'm sure they don't know what she's found.");

        Assert.Equal(WordType.Pronoun, types[0]);
        Assert.Equal(WordType.Adjective, types[1]);
        Assert.Equal(WordType.Verb, types[3]);
        Assert.Equal(WordType.Verb, types[4]);
        Assert.Equal(WordType.Verb, types[7]);
    }

    [Fact]
    public void Unknown_words_are_guessed_from_their_endings()
    {
        WordType[] types = Tag("The zorbling crept quorfly past the vexed mirthons.");

        Assert.Equal(WordType.Adverb, types[3]);
        Assert.Equal(WordType.Noun, types[7]);
    }

    [Fact]
    public void An_empty_sentence_is_fine()
    {
        Tagger.Tag("", [], CustomWords.Empty, [], []);
    }
}
