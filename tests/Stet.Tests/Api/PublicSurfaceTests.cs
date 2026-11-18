namespace Stet.Tests.Api;

public class PublicSurfaceTests
{
    // Adding to the public API is a commitment, so a type becoming public has to be a deliberate change here.
    [Fact]
    public void Only_the_agreed_types_are_public()
    {
        string[] expected =
        [
            "Stet.CheckCache",
            "Stet.Checker",
            "Stet.CheckerOptions",
            "Stet.Dialect",
            "Stet.DialogueStrictness",
            "Stet.DocumentParagraph",
            "Stet.Issue",
            "Stet.IssueKind",
            "Stet.LibraryInfo",
            "Stet.ParagraphContext",
            "Stet.ParagraphResult",
            "Stet.QuoteStyle",
            "Stet.RuleCategory",
            "Stet.RuleIds",
            "Stet.RuleInfo",
            "Stet.Suggestion",
            "Stet.TextSpan",
            "Stet.WordScope",
        ];

        string[] actual = [.. typeof(Checker).Assembly.GetExportedTypes().Select(t => t.FullName!).Order(StringComparer.Ordinal)];

        Assert.Equal(expected, actual);
    }
}
