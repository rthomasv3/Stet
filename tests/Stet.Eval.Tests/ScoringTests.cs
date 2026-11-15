using Stet.Eval.Checkers;
using Stet.Eval.Corpora;
using Stet.Eval.Planting;
using Stet.Eval.Scoring;

namespace Stet.Eval.Tests;

public class ScoringTests
{
    private static readonly EvalSettings Settings = new() { CleanWordsPerBook = 1000, PlantedPerCategory = 5 };

    private static Corpus TinyCorpus()
    {
        string[] dev =
        [
            "The the cat sat on a mat while they were out, and it was a long quiet day for it.",
            "“We have to go,” she said, and he was glad of their help with the heavy boxes.",
            "It was an awful sight to see, but they were brave about their troubles and went on.",
        ];
        string[] test =
        [
            "“They were here,” said Tom, who had had a long day and wanted to sit by the fire.",
            "She has an idea that their friends were waiting somewhere beyond the old stone bridge.",
        ];
        return new Corpus("tiny", [new Book("a", Split.Dev, "", "US", dev), new Book("b", Split.Test, "", "GB", test)]);
    }

    [Fact]
    public void WorkSetIsTheSameEveryTime()
    {
        WorkSet first = WorkSet.Build([TinyCorpus()], Settings, Planters.All);
        WorkSet second = WorkSet.Build([TinyCorpus()], Settings, Planters.All);

        Assert.Equal(first.Planted, second.Planted);
        Assert.NotEmpty(first.Planted);
        Assert.All(first.Planted, p => Assert.NotEqual(p.Clean, p.Error.Text));
    }

    [Fact]
    public void PlantedErrorsStayOnTheirSideOfTheSplit()
    {
        Corpus corpus = TinyCorpus();
        WorkSet work = WorkSet.Build([corpus], Settings, Planters.All);

        Assert.All(work.Planted, p => Assert.Contains(p.Clean, corpus.Books.Single(b => b.Split == p.Split).Paragraphs));
    }

    [Fact]
    public void NoneCheckerScoresZeroEverywhere()
    {
        WorkSet work = WorkSet.Build([TinyCorpus()], Settings, Planters.All);

        EvalReport report = new Scorer(Settings, Labels.Empty).Run(new NoneChecker(), work, "test");

        Assert.All(report.Groups, g => Assert.Equal(0, g.Flags));
        Assert.All(report.Groups.SelectMany(g => g.Planted), p => Assert.Equal(0, p.Caught));
        Assert.Equal(["tiny/dev", "tiny/test", "all/dev", "all/test"], report.Groups.Select(g => $"{g.Corpus}/{g.Split}"));
    }

    [Fact]
    public void DemoCheckerCountsFalseAlarmsAndCatches()
    {
        WorkSet work = WorkSet.Build([TinyCorpus()], Settings, Planters.All);

        EvalReport report = new Scorer(Settings, Labels.Empty).Run(new DemoChecker(), work, "test");

        GroupResult dev = report.Find("tiny", "dev")!;
        Assert.Equal(1, dev.Flags); // "The the"
        Assert.Equal("demo.repeated-word", dev.Rules.Single().RuleId);
        Assert.True(dev.FlagsPer1000 > 0);

        GroupResult test = report.Find("tiny", "test")!;
        Assert.Equal(1, test.Flags); // "had had"

        PlantedResult repeated = dev.Planted.Single(p => p.Category == "repeated-word");
        Assert.True(repeated.Planted > 0);
        Assert.Equal(repeated.Planted, repeated.Caught);
        Assert.Equal(repeated.Caught, repeated.FirstFixRight);
        Assert.Equal(0, dev.Planted.Single(p => p.Category == "typo").Caught);
    }

    [Fact]
    public void ConfirmedRealErrorsAreNotCountedAsFalseAlarms()
    {
        Corpus corpus = TinyCorpus();
        WorkSet work = WorkSet.Build([corpus], Settings, Planters.All);
        string paragraph = corpus.Books[0].Paragraphs[0];
        var labels = new Labels([new Label(WorkSet.HashOf(paragraph), 0, 7, "demo.repeated-word", Labels.RealError)]);

        EvalReport report = new Scorer(Settings, labels).Run(new DemoChecker(), work, "test");

        GroupResult dev = report.Find("tiny", "dev")!;
        Assert.Equal(0, dev.Flags);
        Assert.Equal(1, dev.ConfirmedRealErrors);
    }

    [Fact]
    public void FlagAlreadyOnTheCleanTextIsNotACatch()
    {
        var item = new PlantedItem("c", Split.Dev, "b", "US", 0, "typo", "the the cat", new PlantedError("the the cta", 8, 3, 8, 3, "transpose"));
        EvalIssue[] onClean = [new EvalIssue(0, 11, "r", "", [])];
        EvalIssue[] onPlanted = [new EvalIssue(0, 11, "r", "", [])];

        Assert.False(Scorer.Judge(item, onPlanted, onClean).Caught);
        Assert.True(Scorer.Judge(item, onPlanted, []).Caught);
        Assert.False(Scorer.Judge(item, [new EvalIssue(0, 3, "r", "", [])], []).Caught);
    }

    [Fact]
    public void FixIsRightOnlyWhenItRestoresTheCleanText()
    {
        var item = new PlantedItem("c", Split.Dev, "b", "US", 0, "typo", "a cat sat", new PlantedError("a cta sat", 2, 3, 2, 3, "transpose"));
        var wrong = new EvalSuggestion([new Edit(2, 3, "cot")]);
        var right = new EvalSuggestion([new Edit(2, 3, "cat")]);

        Scorer.PlantOutcome second = Scorer.Judge(item, [new EvalIssue(2, 3, "r", "", [wrong, right])], []);
        Scorer.PlantOutcome first = Scorer.Judge(item, [new EvalIssue(2, 3, "r", "", [right, wrong])], []);

        Assert.Equal(new Scorer.PlantOutcome(true, false, true), second);
        Assert.Equal(new Scorer.PlantOutcome(true, true, true), first);
    }

    [Fact]
    public void UncheckedParagraphsAreLeftOutOfTheNumbers()
    {
        WorkSet work = WorkSet.Build([TinyCorpus()], Settings, Planters.All);
        string path = Path.GetTempFileName();
        try
        {
            string first = work.Clean[0].Paragraphs[0];
            File.WriteAllText(path, $$"""{"id":"{{WorkSet.HashOf(first)}}","issues":[{"start":0,"length":7,"ruleId":"X","category":"C","suggestions":[[{"start":3,"length":4,"text":""}]]}]}""" + "\n");

            EvalReport report = new Scorer(Settings, Labels.Empty).Run(new ExternalChecker("ext", path), work, "test");

            GroupResult dev = report.Find("tiny", "dev")!;
            Assert.Equal(3, dev.Paragraphs);
            Assert.Equal(2, dev.UncheckedParagraphs);
            Assert.Equal(1, dev.Flags);
            Assert.Equal(Text.Words.Count(first), dev.Words);
            Assert.All(dev.Planted, p => Assert.Equal(0, p.Checked));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SubsetKeepsCleanParagraphsOfItsPlantedErrors()
    {
        WorkSet work = WorkSet.Build([TinyCorpus()], Settings, Planters.All);

        var subset = work.Subset("tiny", cleanWordsPerBook: 1, plantedPerCategory: 1).ToList();
        var texts = subset.Select(s => s.Text).ToHashSet();

        Assert.Equal(subset.Count, texts.Count);
        Assert.True(subset.Count < work.AllTexts().Count());
        Assert.Equal(12, work.Planted.Count(p => texts.Contains(p.Error.Text) && texts.Contains(p.Clean)));
        Assert.Contains(subset, s => s.Dialect == "GB");
        Assert.Empty(work.Subset("other", 100, 100));
    }

    [Fact]
    public void FilteredCheckerDropsExcludedCategories()
    {
        var filtered = new FilteredChecker(new DemoChecker(), "demo-x", ["mechanics"]);

        Assert.Empty(filtered.CheckDocument(["the the cat"], new DocumentInfo("c", "b", "US"))[0]!);
        Assert.Single(new DemoChecker().CheckDocument(["the the cat"], new DocumentInfo("c", "b", "US"))[0]!);
    }

    [Fact]
    public void ReportSurvivesARoundTripAndShowsChanges()
    {
        WorkSet work = WorkSet.Build([TinyCorpus()], Settings, Planters.All);
        EvalReport none = new Scorer(Settings, Labels.Empty).Run(new NoneChecker(), work, "test");
        EvalReport demo = new Scorer(Settings, Labels.Empty).Run(new DemoChecker(), work, "test");
        string path = Path.GetTempFileName();
        try
        {
            ReportWriter.WriteJson(none, path);
            EvalReport previous = ReportWriter.TryRead(path)!;

            Assert.Equal(none.Groups.Select(g => g.Words), previous.Groups.Select(g => g.Words));

            string markdown = ReportWriter.ToMarkdown(demo, previous, previous);
            Assert.Contains("| repeated-word |", markdown);
            Assert.Contains("+100 pts", markdown);
            Assert.Contains("none caught", markdown);
            Assert.DoesNotContain("cat sat", markdown);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
