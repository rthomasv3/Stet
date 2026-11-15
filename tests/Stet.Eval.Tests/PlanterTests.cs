using Stet.Eval.Planting;
using Stet.Eval.Text;

namespace Stet.Eval.Tests;

public class PlanterTests
{
    private const string Sample = "“I think they were late,” she said, and he was sure of it. It was a long road to their house.";

    public static TheoryData<string> Categories => new(Planters.All.Select(p => p.Category));

    [Theory]
    [MemberData(nameof(Categories))]
    public void PlantsOneErrorInsideTheReportedRegion(string category)
    {
        IErrorPlanter planter = Planters.All.Single(p => p.Category == category);

        for (ulong seed = 0; seed < 50; seed++)
        {
            PlantedError? error = planter.TryPlant(Sample, new Rng(seed));

            Assert.NotNull(error);
            Assert.NotEqual(Sample, error.Text);
            Assert.InRange(error.Start, 0, error.Text.Length);
            Assert.InRange(error.Start + error.Length, error.Start + 1, error.Text.Length);

            // Everything outside the two regions is untouched.
            Assert.Equal(Sample[..error.OriginalStart], error.Text[..error.Start]);
            Assert.Equal(Sample[(error.OriginalStart + error.OriginalLength)..], error.Text[(error.Start + error.Length)..]);
        }
    }

    [Theory]
    [MemberData(nameof(Categories))]
    public void SameSeedGivesSameError(string category)
    {
        IErrorPlanter planter = Planters.All.Single(p => p.Category == category);

        Assert.Equal(planter.TryPlant(Sample, new Rng(7)), planter.TryPlant(Sample, new Rng(7)));
    }

    [Theory]
    [MemberData(nameof(Categories))]
    public void ReturnsNullWhenNothingFits(string category)
    {
        IErrorPlanter planter = Planters.All.Single(p => p.Category == category);

        Assert.Null(planter.TryPlant("No.", new Rng(1)));
    }

    [Fact]
    public void TypoChangesOneLongLowercaseWord()
    {
        PlantedError error = new TypoPlanter().TryPlant("The Captain shouted loudly.", new Rng(3))!;

        string[] before = "The Captain shouted loudly.".Split(' ');
        string[] after = error.Text.Split(' ');
        Assert.Equal(before.Length, after.Length);
        Assert.Equal("The", after[0]);
        Assert.Equal("Captain", after[1]);
        Assert.Single(Enumerable.Range(0, before.Length), i => before[i] != after[i]);
    }

    [Fact]
    public void ConfusableKeepsCapitalsAndCurlyApostrophes()
    {
        PlantedError error = new ConfusablePlanter().TryPlant("Its door was open, wasn’t it?", new Rng(1))!;

        Assert.Equal("It’s door was open, wasn’t it?", error.Text);
        Assert.Equal("its>it's", error.Kind);
    }

    [Fact]
    public void ArticleRegionCoversTheNextWord()
    {
        PlantedError error = new ArticlePlanter().TryPlant("She ate an apple.", new Rng(1))!;

        Assert.Equal("She ate a apple.", error.Text);
        Assert.Equal("a apple", error.Text.Substring(error.Start, error.Length));
        Assert.Equal("an apple", "She ate an apple.".Substring(error.OriginalStart, error.OriginalLength));
    }

    [Fact]
    public void RepeatedWordSkipsValidRepeats()
    {
        Assert.Null(new RepeatedWordPlanter().TryPlant("had that", new Rng(1)));

        PlantedError error = new RepeatedWordPlanter().TryPlant("The door opened.", new Rng(1))!;
        Assert.Contains(error.Text, new[] { "The The door opened.", "The door door opened." });
    }

    [Theory]
    [InlineData("They were tired.", "They was tired.")]
    [InlineData("I don’t know.", "I doesn’t know.")]
    [InlineData("Then he has gone.", "Then he have gone.")]
    public void AgreementSwapsTheVerb(string clean, string expected)
    {
        Assert.Equal(expected, new AgreementPlanter().TryPlant(clean, new Rng(1))!.Text);
    }

    [Theory]
    [InlineData("If he was here, we would know.")]
    [InlineData("You and he was never friends.")]
    [InlineData("I wish it was over.")]
    public void AgreementLeavesSubjunctiveAndJoinedSubjectsAlone(string clean)
    {
        Assert.Null(new AgreementPlanter().TryPlant(clean, new Rng(1)));
    }

    [Fact]
    public void DialoguePunctuationMakesEachKindOfBreak()
    {
        const string clean = "“Hello,” she said.";
        var planter = new DialoguePunctuationPlanter();

        var results = Enumerable.Range(0, 40).Select(seed => planter.TryPlant(clean, new Rng((ulong)seed))!.Text).ToHashSet();

        Assert.Equal(
            new HashSet<string> { "“Hello.” she said.", "“Hello” she said.", "“Hello,” She said." },
            results);
    }

    [Fact]
    public void DialoguePunctuationIgnoresActionBeats()
    {
        Assert.Null(new DialoguePunctuationPlanter().TryPlant("“Hello,” she smiled.", new Rng(1)));
        Assert.NotNull(new DialoguePunctuationPlanter().TryPlant("\"Hello,\" said Tom.", new Rng(1)));
    }
}
