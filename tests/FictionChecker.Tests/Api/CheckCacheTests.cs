namespace FictionChecker.Tests.Api;

public class CheckCacheTests
{
    private const string Bad = "She could not beleive it.";

    [Fact]
    public void An_unchanged_paragraph_returns_the_remembered_result()
    {
        var cache = new CheckCache(new Checker());

        ParagraphResult first = cache.CheckParagraph(Bad);
        ParagraphResult second = cache.CheckParagraph(new string(Bad.AsSpan()));

        Assert.Same(first, second);
        Assert.Equal(1, cache.Count);
        Assert.Single(first.Issues);
    }

    [Fact]
    public void Results_match_the_checker_whatever_is_remembered()
    {
        var checker = new Checker();
        var cache = new CheckCache(checker);
        string[] document = ["“We ain’t going back.", "“Not now, and nothin’ doing.”", "He had nothin’ to say.", "A smal thing."];

        for (int round = 0; round < 2; round++)
        {
            IReadOnlyList<ParagraphResult> expected = checker.CheckDocument(document);
            IReadOnlyList<ParagraphResult> actual = cache.CheckDocument(document);
            Assert.Equal(expected.Select(Describe), actual.Select(Describe));
        }

        Assert.Equal(4, cache.Count);
    }

    [Fact]
    public void Only_changed_paragraphs_are_checked_again()
    {
        var cache = new CheckCache(new Checker());
        string[] before = ["One smal thing.", "Two smal things.", "Three smal things."];
        IReadOnlyList<ParagraphResult> first = cache.CheckDocument(before);

        IReadOnlyList<ParagraphResult> second = cache.CheckDocument(["One smal thing.", "Two small things.", "Three smal things."]);

        Assert.Same(first[0], second[0]);
        Assert.NotSame(first[1], second[1]);
        Assert.Empty(second[1].Issues);
        Assert.Same(first[2], second[2]);
    }

    [Fact]
    public void The_same_text_in_and_out_of_speech_is_kept_apart()
    {
        var cache = new CheckCache(new Checker());

        ParagraphResult narration = cache.CheckParagraph("Nothin’ for it.");
        ParagraphResult speech = cache.CheckParagraph("Nothin’ for it.", new ParagraphContext { DialogueRanges = [new TextSpan(0, 15)] });

        Assert.Single(narration.Issues);
        Assert.Empty(speech.Issues);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void Different_options_are_kept_apart()
    {
        var cache = new CheckCache(new Checker());
        var british = new ParagraphContext { Options = CheckerOptions.Default with { Dialect = Dialect.British } };

        Assert.Single(cache.CheckParagraph("The colour had faded.").Issues);
        Assert.Empty(cache.CheckParagraph("The colour had faded.", british).Issues);
        Assert.Single(cache.CheckParagraph("The colour had faded.").Issues);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void Changing_a_word_set_drops_the_results_made_with_it()
    {
        var checker = new Checker();
        var cache = new CheckCache(checker);
        Assert.Single(cache.CheckParagraph("Kaelith drew her blade.").Issues);

        checker.SetWords("world", ["Kaelith"]);

        Assert.Empty(cache.CheckParagraph("Kaelith drew her blade.").Issues);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Changing_one_word_set_keeps_results_made_with_other_sets_only()
    {
        var checker = new Checker();
        checker.SetWords("world-a", ["Kaelith"]);
        checker.SetWords("world-b", ["Ysolde"]);
        var cache = new CheckCache(checker);
        var onlyA = new ParagraphContext { Options = CheckerOptions.Default with { WordSets = ["world-a"] } };
        ParagraphResult first = cache.CheckParagraph("Kaelith met Ysolde.", onlyA);
        ParagraphResult all = cache.CheckParagraph("Kaelith met Ysolde.");

        checker.AddWords("world-b", ["Zorvath"]);

        Assert.Same(first, cache.CheckParagraph("Kaelith met Ysolde.", onlyA));
        Assert.NotSame(all, cache.CheckParagraph("Kaelith met Ysolde."));
        Assert.Equal(2, cache.Count);

        checker.AddWords("world-a", ["Zorvath"]);
        Assert.NotSame(first, cache.CheckParagraph("Kaelith met Ysolde.", onlyA));
    }

    [Fact]
    public void The_least_recently_used_paragraph_is_dropped_at_capacity()
    {
        var cache = new CheckCache(new Checker(), capacity: 2);
        ParagraphResult one = cache.CheckParagraph("One smal thing.");
        cache.CheckParagraph("Two smal things.");
        Assert.Same(one, cache.CheckParagraph("One smal thing."));

        cache.CheckParagraph("Three smal things.");

        Assert.Equal(2, cache.Count);
        Assert.Same(one, cache.CheckParagraph("One smal thing."));
        cache.Clear();
        Assert.Equal(0, cache.Count);
        Assert.NotSame(one, cache.CheckParagraph("One smal thing."));
    }

    [Fact]
    public void Capacity_must_be_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CheckCache(new Checker(), 0));
    }

    private static string Describe(ParagraphResult result) =>
        string.Join(";", result.Issues.Select(i => $"{i.Start}+{i.Length}:{i.RuleId}:{string.Join(",", i.Suggestions.Select(s => s.Text))}")) + "|" + result.EndsInDialogue;
}
