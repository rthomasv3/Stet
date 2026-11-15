namespace FictionChecker.Tests.Api;

public class PublicSurfaceTests
{
    // The public API needs Robert's approval before 1.0, so a type becoming public is a deliberate change here.
    [Fact]
    public void Only_the_agreed_types_are_public()
    {
        string[] expected =
        [
            "FictionChecker.CheckCache",
            "FictionChecker.Checker",
            "FictionChecker.CheckerOptions",
            "FictionChecker.Dialect",
            "FictionChecker.DialogueStrictness",
            "FictionChecker.DocumentParagraph",
            "FictionChecker.Issue",
            "FictionChecker.IssueKind",
            "FictionChecker.LibraryInfo",
            "FictionChecker.ParagraphContext",
            "FictionChecker.ParagraphResult",
            "FictionChecker.QuoteStyle",
            "FictionChecker.RuleCategory",
            "FictionChecker.RuleIds",
            "FictionChecker.RuleInfo",
            "FictionChecker.Suggestion",
            "FictionChecker.TextSpan",
            "FictionChecker.WordScope",
        ];

        string[] actual = [.. typeof(Checker).Assembly.GetExportedTypes().Select(t => t.FullName!).Order(StringComparer.Ordinal)];

        Assert.Equal(expected, actual);
    }
}
