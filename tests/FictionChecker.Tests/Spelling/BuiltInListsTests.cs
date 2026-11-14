using FictionChecker.Spelling;

namespace FictionChecker.Tests.Spelling;

public class BuiltInListsTests
{
    private static readonly Lexicon Words = Lexicon.LoadEmbedded();
    private static readonly BuiltInLists Lists = BuiltInLists.LoadEmbedded();

    private static bool IsWord(string word) =>
        Words.TryGet(word, out _) || Words.TryGet(char.ToUpperInvariant(word[0]) + word[1..], out _);

    [Fact]
    public void ListsAreLoaded()
    {
        Assert.Contains("gonna", (IEnumerable<string>)Lists.Informal);
        Assert.Contains("ain't", (IEnumerable<string>)Lists.Informal);
        Assert.Contains("hath", (IEnumerable<string>)Lists.Archaic);
        Assert.Equal("receive", Lists.Misspellings["recieve"]);
        Assert.Equal("a lot", Lists.Misspellings["alot"]);
        Assert.InRange(Lists.Misspellings.Count, 300, 2000);
    }

    [Fact]
    public void NoMisspellingIsACommonWord()
    {
        // Rare real words are allowed (calender, wether): in fiction they are nearly always slips.
        Assert.All(Lists.Misspellings.Keys, wrong => Assert.False(Words.TryGet(wrong, out WordInfo info) && !info.IsRare, $"'{wrong}' is a common word"));
    }

    [Fact]
    public void EveryCorrectionIsMadeOfRealWords()
    {
        Assert.All(Lists.Misspellings.Values, right => Assert.All(right.Split(' '), part => Assert.True(IsWord(part), $"'{part}' is not in the lexicon")));
    }

    [Fact]
    public void ArchaicWordsAreNotAlreadyOrdinaryWords()
    {
        Assert.All(Lists.Archaic, word => Assert.False(Words.TryGet(word, out _), $"'{word}' is already in the lexicon"));
    }

    [Fact]
    public void ListsAreLowercaseWithStraightApostrophes()
    {
        IEnumerable<string> all = Lists.Informal.Concat(Lists.Archaic).Concat(Lists.Misspellings.Keys).Concat(Lists.Misspellings.Values);

        Assert.All(all, entry => Assert.True(entry == entry.ToLowerInvariant() && !entry.Contains('’'), entry));
    }
}
