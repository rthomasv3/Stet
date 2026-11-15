using Stet.Segmentation;

namespace Stet.Tests.Segmentation;

public class DialogueDetectorTests
{
    private static string[] Speech(string text, QuoteStyle style, bool starts = false) =>
        DialogueDetector.Detect(text, style, starts).Spans.Select(s => text.Substring(s.Start, s.Length)).ToArray();

    [Theory]
    [InlineData("“Hello,” she said. “Come in.”", new[] { "“Hello,”", "“Come in.”" })]
    [InlineData("\"Hello,\" she said. \"Come in.\"", new[] { "\"Hello,\"", "\"Come in.\"" })]
    [InlineData("She said, “Hello.”", new[] { "“Hello.”" })]
    [InlineData("No speech here, only Tom’s dog.", new string[0])]
    [InlineData("“He said ‘no’ to me,” she said.", new[] { "“He said ‘no’ to me,”" })]
    [InlineData("“Don’t,” he said. “It’s the boys’ dog.”", new[] { "“Don’t,”", "“It’s the boys’ dog.”" })]
    [InlineData("\"Stop--\" He froze.", new[] { "\"Stop--\"" })]
    public void FindsDoubleQuotedSpeech(string text, string[] expected)
    {
        Assert.Equal(expected, Speech(text, QuoteStyle.Double));
    }

    [Theory]
    [InlineData("‘Hello,’ she said. ‘Come in.’", new[] { "‘Hello,’", "‘Come in.’" })]
    [InlineData("'Hello,' she said. 'Come in.'", new[] { "'Hello,'", "'Come in.'" })]
    [InlineData("‘Don’t,’ he said. ‘It’s Tom’s.’", new[] { "‘Don’t,’", "‘It’s Tom’s.’" })]
    [InlineData("'Don't,' he said. 'It's Tom's.'", new[] { "'Don't,'", "'It's Tom's.'" })]
    [InlineData("‘He said “no” to me,’ she said.", new[] { "‘He said “no” to me,’" })]
    [InlineData("No speech, only Tom’s dog and the boys’ cat.", new string[0])]
    [InlineData("He called it ‘magic’ and left.", new[] { "‘magic’" })]
    [InlineData("He called them ‘heroes’ and left.", new[] { "‘heroes’" })]
    [InlineData("The ‘i’ has the sound of long ‘e’ in her name.", new[] { "‘i’", "‘e’" })]
    [InlineData("They called it ‘runnin’’ back then.", new[] { "‘runnin’" })]
    public void FindsSingleQuotedSpeech(string text, string[] expected)
    {
        Assert.Equal(expected, Speech(text, QuoteStyle.Single));
    }

    [Theory]
    [InlineData("‘I was runnin’ all night,’ he said.", new[] { "‘I was runnin’ all night,’" })]
    [InlineData("‘We took the boys’ boat out past the point,’ he said.", new[] { "‘We took the boys’ boat out past the point,’" })]
    [InlineData("‘Give ’em a cup o’ tea,’ she said.", new[] { "‘Give ’em a cup o’ tea,’" })]
    [InlineData("‘’Tis late,’ he said.", new[] { "‘’Tis late,’" })]
    [InlineData("'Give 'em nothin',' she said.", new[] { "'Give 'em nothin','" })]
    [InlineData("‘Back in the ’90s,’ he said.", new[] { "‘Back in the ’90s,’" })]
    [InlineData("‘I’m comin’’", new[] { "‘I’m comin’’" })]
    public void ApostrophesInsideSingleQuotedSpeechDoNotCloseIt(string text, string[] expected)
    {
        Assert.Equal(expected, Speech(text, QuoteStyle.Single));
    }

    [Fact]
    public void DoubleStyleIgnoresSingleQuotes()
    {
        Assert.Empty(Speech("He called it ‘magic’ and left.", QuoteStyle.Double));
    }

    [Fact]
    public void QuoteLeftOpenRunsToTheEnd()
    {
        DialogueResult result = DialogueDetector.Detect("“It began long ago. We were young.", QuoteStyle.Double);

        Assert.True(result.EndsInDialogue);
        Assert.Equal(0, result.UnmatchedQuotes);
        Assert.Equal(new TextSpan(0, 34), Assert.Single(result.Spans));
    }

    [Fact]
    public void ContinuedSpeechReopensInTheNextParagraph()
    {
        DialogueResult result = DialogueDetector.Detect("“And then we grew old.” He sighed.", QuoteStyle.Double, startsInDialogue: true);

        Assert.False(result.EndsInDialogue);
        Assert.Equal(0, result.UnmatchedQuotes);
        Assert.True(result.IsDialogue(5));
        Assert.False(result.IsDialogue(27));
    }

    [Fact]
    public void ContinuationWithoutAReopeningMarkIsNarration()
    {
        DialogueResult result = DialogueDetector.Detect("He walked on in silence.", QuoteStyle.Double, startsInDialogue: true);

        Assert.Empty(result.Spans);
        Assert.Equal(1, result.UnmatchedQuotes);
    }

    [Theory]
    [InlineData("He said hello,” and left.")]
    [InlineData("He said hello,\" and left.")]
    public void StrayClosingMarkIsCountedAndTextStaysNarration(string text)
    {
        DialogueResult result = DialogueDetector.Detect(text, QuoteStyle.Double);

        Assert.Empty(result.Spans);
        Assert.Equal(1, result.UnmatchedQuotes);
    }

    [Fact]
    public void SecondOpeningMarkDropsTheFirst()
    {
        string text = "“Hello, she said. “Come in.”";
        DialogueResult result = DialogueDetector.Detect(text, QuoteStyle.Double);

        Assert.Equal(1, result.UnmatchedQuotes);
        Assert.Equal("“Come in.”", text.Substring(result.Spans.Single().Start, result.Spans.Single().Length));
    }

    [Fact]
    public void SameMarksNestedInsideSpeechStayInside()
    {
        string text = "“He says, “I want to see him.” I have heard him,” said Wemmick.";

        Assert.Equal(["“He says, “I want to see him.” I have heard him,”"], Speech(text, QuoteStyle.Double));
        Assert.Equal(0, DialogueDetector.Detect(text, QuoteStyle.Double).UnmatchedQuotes);
    }

    [Fact]
    public void SkipRangesHideTheirQuoteMarks()
    {
        string text = "The note read “gone” and he said, “Fine.”";
        DialogueResult result = DialogueDetector.Detect(text, QuoteStyle.Double, skipRanges: [new TextSpan(14, 6)]);

        Assert.Equal("“Fine.”", text.Substring(result.Spans.Single().Start, result.Spans.Single().Length));
    }

    [Fact]
    public void CallerRangesCountAsDialogue()
    {
        DialogueResult result = DialogueDetector.Detect("Why me? he thought. “Why?”", QuoteStyle.Double, dialogueRanges: [new TextSpan(0, 7)]);

        Assert.Equal(2, result.Spans.Count);
        Assert.True(result.IsDialogue(3));
        Assert.False(result.IsDialogue(10));
    }

    [Fact]
    public void DocumentCarriesAnOpenQuoteOnlyWhenTheNextParagraphReopensIt()
    {
        string[] paragraphs =
        [
            "“It began long ago.",
            "“We were young then.”",
            "He stopped. “I forget the rest.",
            "Nobody spoke.",
        ];

        List<DialogueResult> results = DialogueDetector.DetectDocument(paragraphs, QuoteStyle.Double);

        Assert.True(results[0].EndsInDialogue);
        Assert.Equal(0, results[0].UnmatchedQuotes);
        Assert.False(results[1].EndsInDialogue);
        Assert.Single(results[1].Spans);

        // The third paragraph's quote never closes and the fourth does not reopen it: a slip, so narration.
        Assert.False(results[2].EndsInDialogue);
        Assert.Empty(results[2].Spans);
        Assert.Equal(1, results[2].UnmatchedQuotes);
        Assert.Empty(results[3].Spans);
        Assert.Equal(0, results[3].UnmatchedQuotes);
    }

    [Fact]
    public void SpeechBrokenAcrossParagraphsWithoutAReopeningMarkIsStillSpeech()
    {
        string[] paragraphs = ["He said, “The law is the law, and I take care", "of law and order here.” He sat down.", "Nobody spoke."];

        List<DialogueResult> results = DialogueDetector.DetectDocument(paragraphs, QuoteStyle.Double);

        Assert.Equal(0, results.Sum(r => r.UnmatchedQuotes));
        Assert.True(results[0].EndsInDialogue);
        Assert.Equal(new TextSpan(0, 23), Assert.Single(results[1].Spans));
        Assert.False(results[1].EndsInDialogue);
        Assert.Empty(results[2].Spans);
    }

    [Fact]
    public void SongLinesInsideSpeechAreSpeech()
    {
        string[] paragraphs = ["‘The wind is old,", "The road is long,", "And the boys’ boots are thin.’", "She stopped singing."];

        List<DialogueResult> results = DialogueDetector.DetectDocument(paragraphs, QuoteStyle.Single);

        Assert.Equal(0, results.Sum(r => r.UnmatchedQuotes));
        Assert.Equal(new TextSpan(0, 17), Assert.Single(results[1].Spans));
        Assert.True(results[2].IsDialogue(5));
        Assert.False(results[2].EndsInDialogue);
        Assert.Empty(results[3].Spans);
    }

    [Fact]
    public void ContinuationThatClosesFirstCarriesOnThenScansTheRest()
    {
        string text = "of it.” He rose. “Come.”";
        DialogueResult result = DialogueDetector.Detect(text, QuoteStyle.Double, startsInDialogue: true);

        Assert.Equal(["of it.”", "“Come.”"], result.Spans.Select(s => text.Substring(s.Start, s.Length)));
        Assert.Equal(0, result.UnmatchedQuotes);
    }

    [Fact]
    public void GuessesTheQuoteStyleFromParagraphOpenings()
    {
        Assert.Equal(QuoteStyle.Double, DialogueDetector.GuessStyle(["“Hi,” she said.", "He nodded.", "\"Yes.\""]));
        Assert.Equal(QuoteStyle.Single, DialogueDetector.GuessStyle(["‘Hi,’ she said.", "He nodded.", "'Yes.'"]));
        Assert.Equal(QuoteStyle.Double, DialogueDetector.GuessStyle(["No speech at all."]));
    }
}
