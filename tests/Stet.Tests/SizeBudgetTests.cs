using Stet.Spelling;

namespace Stet.Tests;

/// <summary>
/// The size budgets set for the project. A failure here means a part grew past its budget: either shrink
/// it or change the budget on purpose.
/// </summary>
public class SizeBudgetTests
{
    private const long Megabyte = 1024 * 1024;

    [Fact]
    public void SpellingWordsFitTheirBudget()
    {
        long size = Lexicon.EmbeddedSize();

        Assert.InRange(size, 1, 2 * Megabyte);
    }

    [Fact]
    public void BuiltInListsFitTheirBudget()
    {
        long size = BuiltInLists.EmbeddedSize();

        Assert.InRange(size, 1, Megabyte / 2);
    }

    [Fact]
    public void WordInfoAndTaggerDataFitTheirBudget()
    {
        long size = Stet.Grammar.WordData.EmbeddedSize();

        Assert.InRange(size, 1, 3 * Megabyte / 2);
    }

    [Fact]
    public void ConfusableWordDataFitsItsBudget()
    {
        long size = Stet.Grammar.ConfusableData.EmbeddedSize();

        Assert.InRange(size, 1, 5 * Megabyte);
    }

    [Fact]
    public void LibraryCodeAndDataFitTheHardCap()
    {
        long size = new FileInfo(typeof(LibraryInfo).Assembly.Location).Length;

        Assert.InRange(size, 1, 30 * Megabyte);
    }
}
