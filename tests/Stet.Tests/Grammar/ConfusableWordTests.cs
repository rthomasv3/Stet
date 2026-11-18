using Stet.Grammar;
using Stet.Spelling;

namespace Stet.Tests.Grammar;

public class ConfusableWordTests
{
    private static readonly Checker Shared = new();

    private static Issue? Confusable(string text, ParagraphContext? context = null) =>
        Shared.CheckParagraph(text, context).Issues.FirstOrDefault(i => i.Category == RuleCategory.ConfusableWords);

    [Fact]
    public void The_data_covers_every_word_of_every_family()
    {
        ConfusableData data = ConfusableData.LoadEmbedded(Lexicon.LoadEmbedded());

        Assert.Equal(ConfusableSets.Families.Sum(f => f.Groups.Length), data.GroupCount);
        Assert.True(data.EntryCount > 100_000);
        foreach (string word in ConfusableSets.Families.SelectMany(f => f.Groups).SelectMany(g => g))
        {
            Assert.True(data.TryFind(word, out int group, out int member), word);
            Assert.Equal(word, data.Members(group)[member]);
            Assert.True(data.Counts(group)[member] > 0, word);
        }
    }

    [Fact]
    public void Each_family_has_its_own_rule_with_a_stable_id()
    {
        var ids = Shared.Rules.Where(r => r.Category == RuleCategory.ConfusableWords).Select(r => r.Id).ToList();

        Assert.Equal(ConfusableSets.Families.Select(f => f.RuleId), ids);
        Assert.Contains(RuleIds.ConfusableTheirThere, ids);
        Assert.All(ids, id => Assert.StartsWith("confusable.", id, StringComparison.Ordinal));
    }

    [Fact]
    public void A_flag_names_the_word_meant_and_offers_it_as_the_fix()
    {
        Issue issue = Confusable("She was taller then her brother.")!;

        Assert.Equal("then", issue.Text);
        Assert.Equal(15, issue.Start);
        Assert.Equal(RuleIds.ConfusableThenThan, issue.RuleId);
        Assert.Equal(IssueKind.Grammar, issue.Kind);
        Assert.Contains("than", issue.Message, StringComparison.Ordinal);
        Assert.Contains("compares", issue.Explanation, StringComparison.Ordinal);
        Suggestion fix = Assert.Single(issue.Suggestions);
        Assert.Equal(("than", issue.Start, issue.Length), (fix.Text, fix.Start, fix.Length));
    }

    [Theory]
    [InlineData("Their was nobody in the house.", "There")]
    [InlineData("THEIR WAS NOBODY IN THE HOUSE.", "THERE")]
    [InlineData("I think its going to rain.", "it's")]
    [InlineData("I think its going to rain, don’t you?", "it’s")]
    [InlineData("I hope your going to tell me.", "you're")]
    [InlineData("It was to late to go back.", "too")]
    [InlineData("She got out off the car.", "of")]
    [InlineData("He turned of the light.", "off")]
    [InlineData("I don't want to loose you.", "lose")]
    public void The_fix_keeps_the_writers_capitals_and_apostrophes(string text, string fix)
    {
        Assert.Equal(fix, Confusable(text)!.Suggestions[0].Text);
    }

    [Theory]
    [InlineData("They took their coats and went over there.")]
    [InlineData("It was too late to go to the house, and she was too tired to care.")]
    [InlineData("He took off his hat and thought of his mother.")]
    [InlineData("He cut the end off the rope.")]
    [InlineData("The hot broth took the edge off the cold.")]
    [InlineData("Is there going to be trouble?")]
    [InlineData("He began to long for home.")]
    [InlineData("She loosed the arrow, then drew another to loose at the gate.")]
    [InlineData("You too have power over me.")]
    [InlineData("It's been a long day, and the dog wants its dinner.")]
    [InlineData("Then he left, sooner than she had hoped.")]
    public void Correct_uses_are_left_alone(string text)
    {
        Assert.Null(Confusable(text));
    }

    [Theory]
    [InlineData("She walked along the Peak road to the village.")]
    [InlineData("He met Principal Hale at the gate.")]
    [InlineData("Than Kosis looked up in surprise.")]
    public void Names_and_titles_are_left_alone(string text)
    {
        Assert.Null(Confusable(text));
    }

    [Fact]
    public void A_swap_in_speech_is_flagged_unless_speech_is_not_checked()
    {
        const string Text = "“Their was nobody in the house,” she said.";
        var off = new ParagraphContext { Options = CheckerOptions.Default with { DialogueStrictness = DialogueStrictness.Off } };

        Assert.NotNull(Confusable(Text));
        Assert.Null(Confusable(Text, off));
    }

    [Fact]
    public void A_family_can_be_switched_off_alone_or_with_the_category()
    {
        const string Text = "Their was nobody in the house, and she was taller then her brother.";
        var oneOff = new ParagraphContext { Options = CheckerOptions.Default.WithRule(RuleIds.ConfusableTheirThere, false) };
        var allOff = new ParagraphContext { Options = CheckerOptions.Default.WithCategory(RuleCategory.ConfusableWords, false) };

        Assert.Equal(2, Shared.CheckParagraph(Text).Issues.Count(i => i.Category == RuleCategory.ConfusableWords));
        Assert.Equal(RuleIds.ConfusableThenThan, Assert.Single(Shared.CheckParagraph(Text, oneOff).Issues).RuleId);
        Assert.Empty(Shared.CheckParagraph(Text, allOff).Issues);
    }
}
