using FictionChecker.Segmentation;

namespace FictionChecker.Tests.Segmentation;

public class TokenizerTests
{
    private static string[] Words(string text) =>
        Tokenizer.Tokenize(text).Where(t => t.Kind == TokenKind.Word).Select(t => t.TextIn(text).ToString()).ToArray();

    private static string[] Kinds(string text) =>
        Tokenizer.Tokenize(text).Select(t => $"{t.Kind}:{t.TextIn(text)}").ToArray();

    [Theory]
    [InlineData("The cat sat.", new[] { "The", "cat", "sat" })]
    [InlineData("Don't stop; it's o'clock.", new[] { "Don't", "stop", "it's", "o'clock" })]
    [InlineData("Don’t stop; it’s Kaelith’s.", new[] { "Don’t", "stop", "it’s", "Kaelith’s" })]
    [InlineData("Haydée met Mercédès.", new[] { "Haydée", "met", "Mercédès" })]
    [InlineData("a well-known half-elven mother-in-law", new[] { "a", "well-known", "half-elven", "mother-in-law" })]
    [InlineData("W-w-what? I-I don't know.", new[] { "W-w-what", "I-I", "don't", "know" })]
    [InlineData("word—another–third", new[] { "word", "another", "third" })]
    [InlineData("word--another", new[] { "word", "another" })]
    [InlineData("and/or yes…no", new[] { "and", "or", "yes", "no" })]
    [InlineData("(Hello) [there] “friend”", new[] { "Hello", "there", "friend" })]
    public void SplitsWords(string text, string[] expected)
    {
        Assert.Equal(expected, Words(text));
    }

    [Fact]
    public void CombiningAccentsStayInTheWord()
    {
        string text = "Haydée smiled";

        Assert.Equal(["Haydée", "smiled"], Words(text));
    }

    [Fact]
    public void PositionsAreUtf16Units()
    {
        string text = "😀 Hi there";
        List<Token> tokens = Tokenizer.Tokenize(text);

        Assert.Equal(new Token(0, 2, TokenKind.Punctuation), tokens[0]);
        Assert.Equal(new Token(3, 2, TokenKind.Word), tokens[1]);
        Assert.Equal(new Token(6, 5, TokenKind.Word), tokens[2]);
    }

    [Theory]
    [InlineData("21st")]
    [InlineData("1920s")]
    [InlineData("3.5")]
    [InlineData("1,000")]
    [InlineData("R2D2")]
    [InlineData("42")]
    public void AnythingWithADigitIsANumber(string text)
    {
        Assert.Equal([$"Number:{text}"], Kinds(text));
    }

    [Fact]
    public void NumberEndingASentenceLeavesTheFullStop()
    {
        Assert.Equal(["Word:in", "Number:1920", "Punctuation:."], Kinds("in 1920."));
    }

    [Theory]
    [InlineData("See https://example.com/a?b=1.", "Url:https://example.com/a?b=1")]
    [InlineData("Go to www.example.com, now", "Url:www.example.com")]
    [InlineData("Write to j.smith@example.co.uk.", "Email:j.smith@example.co.uk")]
    public void FindsUrlsAndEmails(string text, string expected)
    {
        Assert.Contains(expected, Kinds(text));
    }

    [Fact]
    public void AtSignAloneIsNotAnEmail()
    {
        Assert.DoesNotContain(Tokenizer.Tokenize("meet @ noon, a@b"), t => t.Kind == TokenKind.Email);
    }

    [Theory]
    [InlineData("J.R.R. Tolkien", new[] { "Initial:J", "Initial:R", "Initial:R" })]
    [InlineData("J. R. R. Tolkien", new[] { "Initial:J", "Initial:R", "Initial:R" })]
    [InlineData("Mr. T. Smith", new[] { "Initial:T" })]
    public void FindsInitials(string text, string[] expected)
    {
        Assert.Equal(expected, Kinds(text).Where(k => k.StartsWith("Initial", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("So did I. Then we left.")]
    [InlineData("Plan A. It failed.")]
    [InlineData("He got a B.")]
    public void WordsThatLookLikeInitialsStayWords(string text)
    {
        Assert.DoesNotContain(Tokenizer.Tokenize(text), t => t.Kind == TokenKind.Initial);
    }

    [Fact]
    public void EllipsisAndDoubleHyphenAreOneMarkEach()
    {
        Assert.Equal(["Word:Well", "Punctuation:...", "Word:no", "Punctuation:--", "Word:yes"], Kinds("Well... no--yes"));
    }

    [Fact]
    public void FlagsApostrophesAtWordEdges()
    {
        string text = "’Tis nothin’ to the boys’ dog, ‘friend’.";
        var flags = Tokenizer.Tokenize(text).Where(t => t.Kind == TokenKind.Word).ToDictionary(t => t.TextIn(text).ToString(), t => t.Flags);

        Assert.Equal(TokenFlags.LeadingApostrophe, flags["Tis"]);
        Assert.Equal(TokenFlags.TrailingApostrophe, flags["nothin"]);
        Assert.Equal(TokenFlags.TrailingApostrophe, flags["boys"]);
        Assert.Equal(TokenFlags.LeadingApostrophe | TokenFlags.TrailingApostrophe, flags["friend"]);
        Assert.Equal(TokenFlags.None, flags["dog"]);
    }

    [Fact]
    public void FlagsHyphenatedWordsAndSkippedRanges()
    {
        string text = "a well-known lettre here";
        List<Token> tokens = Tokenizer.Tokenize(text, [new TextSpan(13, 6)]);

        Assert.True(tokens[1].Has(TokenFlags.Hyphenated));
        Assert.True(tokens[2].Has(TokenFlags.Skipped));
        Assert.False(tokens[3].Has(TokenFlags.Skipped));
    }

    [Theory]
    [InlineData("***", true)]
    [InlineData("* * *", true)]
    [InlineData("#", true)]
    [InlineData("~", true)]
    [InlineData("———", true)]
    [InlineData("", true)]
    [InlineData("❦", true)]
    [InlineData("Chapter 1", false)]
    [InlineData("—Hello", false)]
    public void RecognizesSceneBreaks(string text, bool expected)
    {
        Assert.Equal(expected, Tokenizer.IsSceneBreak(text));
    }

    [Fact]
    public void SplitsParagraphsWithOriginalPositions()
    {
        string text = "First one.\r\n\r\n  Second one.  \n***\n";
        List<TextSpan> paragraphs = ParagraphSplitter.Split(text);

        Assert.Equal(["First one.", "Second one.", "***"], paragraphs.Select(p => text.Substring(p.Start, p.Length)));
        Assert.Equal(16, paragraphs[1].Start);
    }
}
