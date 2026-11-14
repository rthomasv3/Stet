using FictionChecker.Segmentation;

namespace FictionChecker.Tests.Segmentation;

public class SentenceSplitterTests
{
    private static string[] Sentences(string text) =>
        SentenceSplitter.Split(text, Tokenizer.Tokenize(text)).Select(s => text.Substring(s.Start, s.Length)).ToArray();

    [Fact]
    public void SplitsOnEndMarks()
    {
        Assert.Equal(["It rained.", "Was it cold?", "Yes!", "They went in."], Sentences("It rained. Was it cold? Yes! They went in."));
    }

    [Theory]
    [InlineData("Mr. Smith met Dr. Jones on St. James Street.")]
    [InlineData("Capt. Vorn and Lt. Hale saluted Mrs. Reed.")]
    [InlineData("She read J. R. R. Tolkien and C.S. Lewis.")]
    [InlineData("They walked approx. five miles, i.e. too far.")]
    [InlineData("He owned cows, pigs, etc. and sold them.")]
    [InlineData("It cost 3.5 crowns.")]
    [InlineData("They lived at No. 12 for years.")]
    public void AbbreviationsAndInitialsDoNotEndASentence(string text)
    {
        Assert.Equal([text], Sentences(text));
    }

    [Fact]
    public void AbbreviationBeforeACapitalCanEndASentence()
    {
        Assert.Equal(["He sold cows, pigs, etc.", "Then he left."], Sentences("He sold cows, pigs, etc. Then he left."));
    }

    [Theory]
    [InlineData("“Why?” she asked.")]
    [InlineData("“Stop!” he cried, and ran.")]
    [InlineData("\"Why?\" she asked.")]
    [InlineData("‘Why?’ she asked.")]
    [InlineData("“Why?” Tom asked.")]
    [InlineData("“Why?” Mr. Smith asked.")]
    [InlineData("“Why not?” Lady Anne demanded of him.")]
    [InlineData("“Well…” she said.")]
    public void SpeechAndItsTagAreOneSentence(string text)
    {
        Assert.Equal([text], Sentences(text));
    }

    [Theory]
    [InlineData("“Why?” She turned away.", new[] { "“Why?”", "She turned away." })]
    [InlineData("“Hello.” She smiled.", new[] { "“Hello.”", "She smiled." })]
    [InlineData("“Go!” Tom ran for the door.", new[] { "“Go!”", "Tom ran for the door." })]
    [InlineData("“I know.” “Do you?”", new[] { "“I know.”", "“Do you?”" })]
    [InlineData("He left. “Wait!” she called.", new[] { "He left.", "“Wait!” she called." })]
    public void ActionBeatsStartANewSentence(string text, string[] expected)
    {
        Assert.Equal(expected, Sentences(text));
    }

    [Theory]
    [InlineData("Well... maybe not.", new[] { "Well... maybe not." })]
    [InlineData("Well… maybe not.", new[] { "Well… maybe not." })]
    [InlineData("He waited... Nothing came.", new[] { "He waited...", "Nothing came." })]
    [InlineData("What?! No. Really?", new[] { "What?!", "No.", "Really?" })]
    public void HandlesEllipsesAndStackedMarks(string text, string[] expected)
    {
        Assert.Equal(expected, Sentences(text));
    }

    [Theory]
    [InlineData("“I was going to—”")]
    [InlineData("“Well…”")]
    [InlineData("Silence")]
    public void UnfinishedSpeechIsStillOneSentence(string text)
    {
        Assert.Equal([text], Sentences(text));
    }

    [Fact]
    public void FragmentsAreSentences()
    {
        Assert.Equal(["Silence.", "Then footsteps."], Sentences("Silence. Then footsteps."));
    }

    [Fact]
    public void SentenceInParenthesesKeepsItsBracket()
    {
        Assert.Equal(["He lied.", "(She knew.)", "It hurt."], Sentences("He lied. (She knew.) It hurt."));
    }

    [Fact]
    public void EmptyTextHasNoSentences()
    {
        Assert.Empty(Sentences("   "));
    }
}
