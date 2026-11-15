using Stet.Grammar;

namespace Stet.Tests.Grammar;

public class FunctionWordsTests
{
    private static readonly FunctionWords Words = FunctionWords.LoadEmbedded();

    [Fact]
    public void The_embedded_list_loads()
    {
        Assert.InRange(Words.Count, 150, 400);
    }

    [Theory]
    [InlineData("i", "Pronoun, Subject, FirstPerson, Singular")]
    [InlineData("them", "Pronoun, Object, ThirdPerson, Plural")]
    [InlineData("her", "Pronoun, Object, Possessive")]
    [InlineData("was", "Be, Past, Singular")]
    [InlineData("has", "Have, Present, ThirdPerson, Singular")]
    [InlineData("could", "Modal")]
    [InlineData("the", "Determiner, Article")]
    [InlineData("of", "Preposition")]
    [InlineData("n't", "Clitic, Negative")]
    public void A_function_word_has_its_traits(string word, string traitNames)
    {
        WordTraits traits = Enum.Parse<WordTraits>(traitNames);
        Assert.True(Words.TraitsOf(word).HasFlag(traits));
    }

    [Fact]
    public void Other_words_have_no_traits()
    {
        Assert.Equal(WordTraits.None, Words.TraitsOf("house"));
        Assert.Equal(WordTraits.None, Words.TraitsOf("I"));
    }

    [Fact]
    public void An_unknown_trait_is_an_error()
    {
        Assert.Throws<InvalidDataException>(() => FunctionWords.Parse("the det shiny"));
    }
}
