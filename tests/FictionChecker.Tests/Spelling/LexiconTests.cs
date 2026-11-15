using FictionChecker.DataBuilder;
using FictionChecker.Spelling;

namespace FictionChecker.Tests.Spelling;

public class LexiconTests
{
    private static readonly Lexicon Embedded = Lexicon.LoadEmbedded();

    private static WordInfo Get(string word)
    {
        Assert.True(Embedded.TryGet(word, out WordInfo info), $"'{word}' should be in the lexicon");
        return info;
    }

    [Fact]
    public void WriterAndReaderAgree()
    {
        LexiconEntry[] entries =
        [
            new("colour", 0b11110, false, 3_000_000),
            new("color", 0b00001, false, 6_000_000),
            new("Haydée", 0b11111, false, 0),
            new("damn", 0b11111, true, 500_000, LargeOnly: true),
            new("elven", 0b11111, false, 100_000, Added: true),
            new("a", 0b11111, false, 5_000_000_000),
        ];

        Lexicon lexicon = Lexicon.Read(LexiconWriter.Write(entries, 5));

        Assert.Equal(6, lexicon.Count);
        Assert.True(lexicon.TryGet("colour", out WordInfo colour));
        Assert.False(colour.IsIn(Dialect.American));
        Assert.True(colour.IsIn(Dialect.British) && colour.IsIn(Dialect.BritishIze) && colour.IsIn(Dialect.Canadian) && colour.IsIn(Dialect.Australian));
        Assert.True(lexicon.TryGet("color", out WordInfo color));
        Assert.True(color.IsIn(Dialect.American));
        Assert.False(color.IsInEveryDialect);
        Assert.True(color.Frequency > colour.Frequency);
        Assert.True(lexicon.TryGet("Haydée", out WordInfo name));
        Assert.Equal(0, name.Frequency);
        Assert.True(lexicon.TryGet("damn", out WordInfo damn));
        Assert.True(damn.NeverSuggest && damn.IsRare && !damn.IsAdded);
        Assert.True(lexicon.TryGet("elven", out WordInfo elven));
        Assert.True(elven.IsAdded && !elven.IsRare && !elven.NeverSuggest);
        Assert.True(lexicon.TryGet("a", out WordInfo a));
        Assert.Equal(247, a.Frequency);
        Assert.False(lexicon.TryGet("haydée", out _));
        Assert.False(lexicon.TryGet("colou", out _));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(199, 0)]
    [InlineData(200, 1)]
    [InlineData(400, 11)]
    [InlineData(800, 21)]
    [InlineData(long.MaxValue, 255)]
    public void FrequencyIsALogScale(long count, byte expected)
    {
        Assert.Equal(expected, LexiconWriter.FrequencyByte(count));
    }

    [Fact]
    public void RejectsDataItCannotRead()
    {
        Assert.Throws<InvalidDataException>(() => Lexicon.Read("nope"u8));
        byte[] newer = LexiconWriter.Write([], 5);
        newer[4] = 99;
        Assert.Throws<InvalidDataException>(() => Lexicon.Read(newer));
    }

    [Fact]
    public void ExpanderAppliesSuffixesPrefixesAndConditions()
    {
        var expander = new HunspellExpander();
        expander.ReadAffixes("""
            NOSUGGEST !
            ONLYINCOMPOUND c
            PFX U Y 1
            PFX U 0 un .
            SFX S Y 2
            SFX S y ies [^aeiou]y
            SFX S 0 s [^y]
            SFX G N 1
            SFX G e ing e
            """);

        var words = expander.Expand("5\ntidy/US\nmake/G\ndamn/!S\n1th/c\nday/S\n").ToDictionary(e => e.Word, e => e.NoSuggest);

        Assert.Equal(
            ["damn", "damns", "day", "make", "making", "tidies", "tidy", "untidies", "untidy"],
            words.Keys.Order(StringComparer.Ordinal));
        Assert.True(words["damn"] && words["damns"]);
        Assert.False(words["tidy"]);
    }

    [Fact]
    public void EmbeddedLexiconHasEveryDialect()
    {
        Assert.InRange(Embedded.Count, 140_000, 170_000);

        Assert.True(Get("the").IsInEveryDialect);
        Assert.True(Get("color").IsIn(Dialect.American));
        Assert.False(Get("color").IsIn(Dialect.British));
        Assert.True(Get("colour").IsIn(Dialect.British) && Get("colour").IsIn(Dialect.BritishIze) && Get("colour").IsIn(Dialect.Canadian) && Get("colour").IsIn(Dialect.Australian));
        Assert.False(Get("colour").IsIn(Dialect.American));
        Assert.True(Get("organise").IsIn(Dialect.British));
        Assert.False(Get("organise").IsIn(Dialect.BritishIze));
        Assert.True(Get("organize").IsIn(Dialect.BritishIze) && Get("organize").IsIn(Dialect.American));
        Assert.False(Get("organize").IsIn(Dialect.British));
    }

    [Fact]
    public void EmbeddedLexiconKeepsCaseAndContractions()
    {
        Assert.True(Embedded.TryGet("London", out _));
        Assert.False(Embedded.TryGet("london", out _));
        Assert.True(Embedded.TryGet("don't", out _));
        Assert.True(Embedded.TryGet("o'clock", out _));

        // Possessives are a rule in the checker, not entries.
        Assert.False(Embedded.TryGet("London's", out _));
    }

    [Fact]
    public void FrequenciesFollowFictionUsage()
    {
        Assert.True(Get("the").Frequency > Get("cat").Frequency);
        Assert.True(Get("cat").Frequency > Get("catamaran").Frequency);
        Assert.True(Get("don't").Frequency > Get("catamaran").Frequency);
        Assert.True(Get("their").Frequency > 150);
    }

    [Theory]
    [InlineData("shorn")]
    [InlineData("hewn")]
    [InlineData("rainforest")]
    public void RarerWordsFromTheLargeSizeAreMarked(string word)
    {
        Assert.True(Get(word).IsRare);
        Assert.False(Get("house").IsRare);
    }

    [Theory]
    [InlineData("shortsword")]
    [InlineData("elven")]
    [InlineData("dwarves")]
    [InlineData("spellbook")]
    [InlineData("homeworld")]
    [InlineData("snuck")]
    [InlineData("hmph")]
    public void OwnListsAreMergedForEveryDialect(string word)
    {
        WordInfo info = Get(word);

        Assert.True(info.IsAdded);
        Assert.True(info.IsInEveryDialect);
    }

    [Fact]
    public void LoadsQuickly()
    {
        Lexicon.LoadEmbedded(); // warm up
        var clock = System.Diagnostics.Stopwatch.StartNew();
        Lexicon.LoadEmbedded();
        clock.Stop();

        // The whole checker has 500 ms to start; the word list gets a generous share even on a slow machine.
        Assert.True(clock.ElapsedMilliseconds < 250, $"Loading took {clock.ElapsedMilliseconds} ms");
    }
}
