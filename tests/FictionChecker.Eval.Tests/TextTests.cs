using FictionChecker.Eval.Checkers;
using FictionChecker.Eval.Text;

namespace FictionChecker.Eval.Tests;

public class TextTests
{
    [Fact]
    public void TokenizeKeepsApostrophesAndHyphensInsideWords()
    {
        List<Token> tokens = Words.Tokenize("Don’t half-open the ‘door’ — it’s Haydée’s.");

        Assert.Equal(["Don’t", "half-open", "the", "door", "it’s", "Haydée’s"], tokens.Select(t => t.Text));
        Assert.Equal(0, tokens[0].Start);
        Assert.Equal(6, tokens[1].Start);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("One two, three.", 3)]
    [InlineData("It’s a well-known 21st trick—really.", 6)]
    public void CountsWords(string text, int expected)
    {
        Assert.Equal(expected, Words.Count(text));
    }

    [Fact]
    public void RngIsFixedAcrossMachines()
    {
        var rng = new Rng(42);

        Assert.Equal(13679457532755275413UL, rng.NextUInt64());
        Assert.Equal(2949826092126892291UL, rng.NextUInt64());
    }

    [Fact]
    public void RngFromNamesDependsOnEveryName()
    {
        Assert.NotEqual(new Rng(1, "a", "bc").NextUInt64(), new Rng(1, "ab", "c").NextUInt64());
        Assert.Equal(new Rng(1, "a", "b").NextUInt64(), new Rng(1, "a", "b").NextUInt64());
    }

    [Fact]
    public void SuggestionAppliesEditsFromTheEnd()
    {
        var suggestion = new EvalSuggestion([new Edit(0, 1, "H"), new Edit(5, 1, ","), new Edit(8, 3, "she")]);

        Assert.Equal("Hello,” she said", suggestion.ApplyTo("hello.” She said"));
    }

    [Fact]
    public void SuggestionWithBadPositionsChangesNothing()
    {
        Assert.Equal("abc", new EvalSuggestion([new Edit(2, 5, "x")]).ApplyTo("abc"));
    }
}
