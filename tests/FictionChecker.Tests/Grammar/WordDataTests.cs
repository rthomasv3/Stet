using FictionChecker.Grammar;
using FictionChecker.Spelling;

namespace FictionChecker.Tests.Grammar;

public class WordDataTests
{
    private static readonly Lexicon Words = Lexicon.LoadEmbedded();
    private static readonly WordData Data = WordData.LoadEmbedded(Words);

    private static byte Cost(string word, WordType type)
    {
        Span<byte> costs = stackalloc byte[WordData.TypeCount];
        Assert.True(Data.TryGetTypeCosts(Words.IndexOf(word), costs), word);
        return costs[(int)type];
    }

    [Theory]
    [InlineData("the", "Determiner")]
    [InlineData("house", "Noun")]
    [InlineData("quickly", "Adverb")]
    [InlineData("beautiful", "Adjective")]
    [InlineData("they", "Pronoun")]
    [InlineData("and", "Conjunction")]
    [InlineData("of", "Adposition")]
    [InlineData("went", "Verb")]
    public void A_word_is_most_likely_its_usual_type(string word, string type)
    {
        WordType usual = Enum.Parse<WordType>(type);
        Assert.Equal(0, Cost(word, usual));
    }

    [Fact]
    public void A_word_with_two_uses_has_both_and_the_rarer_costs_more()
    {
        Assert.Equal(0, Cost("can", WordType.Verb));
        Assert.InRange(Cost("can", WordType.Noun), 4, WordData.Never - 1);
        Assert.Equal(WordData.Never, Cost("can", WordType.Adverb));
        Assert.InRange(Cost("walk", WordType.Noun), 0, 3);
        Assert.InRange(Cost("walk", WordType.Verb), 0, 3);
    }

    [Fact]
    public void A_determiner_is_far_likelier_before_a_noun_than_before_a_verb()
    {
        int noun = Data.TransitionCost((int)WordType.Determiner, (int)WordType.Noun);
        int verb = Data.TransitionCost((int)WordType.Determiner, (int)WordType.Verb);

        Assert.True(noun + (2 * WordData.CostUnit) < verb, $"noun {noun}, verb {verb}");
        Assert.True(Data.TransitionCost((int)WordType.Punctuation, WordData.EndState) < Data.TransitionCost((int)WordType.Determiner, WordData.EndState));
    }

    [Fact]
    public void Function_words_have_their_own_following_types()
    {
        short[] they = Data.CostsAfter("they")!;
        short[] her = Data.CostsAfter("her")!;

        Assert.True(they[(int)WordType.Verb] < they[(int)WordType.Noun]);
        Assert.True(her[(int)WordType.Noun] < they[(int)WordType.Noun]);
        Assert.Null(Data.CostsAfter("house"));
        Assert.InRange(Data.WordsWithCostsAfter, 150, 400);
    }

    [Theory]
    [InlineData("walked", "walk", "VerbPast, VerbPastParticiple")]
    [InlineData("walking", "walk", "VerbGerund")]
    [InlineData("hoping", "hope", "VerbGerund")]
    [InlineData("hopping", "hop", "VerbGerund")]
    [InlineData("stopped", "stop", "VerbPast, VerbPastParticiple")]
    [InlineData("called", "call", "VerbPast, VerbPastParticiple")]
    [InlineData("tried", "try", "VerbPast, VerbPastParticiple")]
    [InlineData("went", "go", "VerbPast")]
    [InlineData("gone", "go", "VerbPastParticiple")]
    [InlineData("written", "write", "VerbPastParticiple")]
    [InlineData("houses", "house", "NounPlural")]
    [InlineData("cities", "city", "NounPlural")]
    [InlineData("children", "child", "NounPlural")]
    [InlineData("wolves", "wolf", "NounPlural")]
    [InlineData("goes", "go", "VerbThirdPerson")]
    [InlineData("bigger", "big", "Comparative")]
    [InlineData("happiest", "happy", "Superlative")]
    [InlineData("better", "good", "Comparative")]
    public void A_form_is_linked_to_its_base_word(string form, string baseWord, string formNames)
    {
        WordForms forms = Enum.Parse<WordForms>(formNames);
        int index = Words.IndexOf(form);
        int expected = Words.IndexOf(baseWord);

        Assert.Contains(Data.BasesOf(index).ToArray(), link => link.Base == expected && (link.Forms & forms) == forms);
        Assert.True((Data.FormsOf(index) & forms) == forms);
    }

    [Fact]
    public void A_word_can_be_a_form_of_two_words()
    {
        var bases = Data.BasesOf(Words.IndexOf("leaves")).ToArray().Select(l => Words.WordAt(l.Base)).ToList();

        Assert.Contains("leaf", bases);
        Assert.Contains("leave", bases);
    }

    [Theory]
    [InlineData("thing")]
    [InlineData("morning")]
    [InlineData("heed")]
    [InlineData("news")]
    [InlineData("number")]
    [InlineData("during")]
    public void Look_alikes_are_not_forms(string word)
    {
        Assert.Equal(WordForms.None, Data.FormsOf(Words.IndexOf(word)) & ~WordForms.VerbBase);
    }

    [Fact]
    public void A_base_verb_knows_it_is_one_and_its_forms_can_be_found()
    {
        int walk = Words.IndexOf("walk");

        Assert.True(Data.FormsOf(walk).HasFlag(WordForms.VerbBase));
        Assert.Equal("walked", Words.WordAt(Data.FormOf(walk, WordForms.VerbPast)));
        Assert.Equal("walks", Words.WordAt(Data.FormOf(walk, WordForms.VerbThirdPerson)));
        Assert.Equal("went", Words.WordAt(Data.FormOf(Words.IndexOf("go"), WordForms.VerbPast)));
        Assert.Equal(-1, Data.FormOf(Words.IndexOf("the"), WordForms.VerbPast));
    }

    [Theory]
    [InlineData("hour", "Vowel")]
    [InlineData("honest", "Vowel")]
    [InlineData("heir", "Vowel")]
    [InlineData("unicorn", "Consonant")]
    [InlineData("university", "Consonant")]
    [InlineData("one", "Consonant")]
    [InlineData("European", "Consonant")]
    [InlineData("apple", "AsSpelled")]
    [InlineData("house", "AsSpelled")]
    [InlineData("umbrella", "AsSpelled")]
    public void Words_that_sound_unlike_their_spelling_are_known(string word, string soundName)
    {
        StartSound sound = Enum.Parse<StartSound>(soundName);
        Assert.Equal(sound, Data.SoundOf(Words.IndexOf(word)));
    }

    [Theory]
    [InlineData("furniture", "Measured")]
    [InlineData("advice", "Measured")]
    [InlineData("water", "Both")]
    [InlineData("information", "Measured")]
    [InlineData("chair", "Counted")]
    [InlineData("time", "Both")]
    public void Nouns_are_counted_or_measured(string word, string kindName)
    {
        Countability kind = Enum.Parse<Countability>(kindName);
        Assert.Equal(kind, Data.CountabilityOf(Words.IndexOf(word)));
    }

    [Fact]
    public void Data_built_for_another_word_list_is_refused()
    {
        Assert.Throws<InvalidDataException>(() => WordData.Read(Words, "FCWI"u8.ToArray()));
        Assert.Throws<InvalidDataException>(() => WordData.Read(Words, [.. "FCWI"u8, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]));
    }
}
