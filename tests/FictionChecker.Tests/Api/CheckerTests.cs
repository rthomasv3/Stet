namespace FictionChecker.Tests.Api;

public class CheckerTests
{
    private static readonly Checker Shared = new();

    [Fact]
    public void Finds_a_misspelling_with_position_message_and_fix()
    {
        ParagraphResult result = Shared.CheckParagraph("She could not beleive it.");

        Issue issue = Assert.Single(result.Issues);
        Assert.Equal(14, issue.Start);
        Assert.Equal(7, issue.Length);
        Assert.Equal("beleive", issue.Text);
        Assert.Equal(RuleIds.Misspelling, issue.RuleId);
        Assert.Equal(RuleCategory.Spelling, issue.Category);
        Assert.Equal(IssueKind.Misspelling, issue.Kind);
        Assert.Contains("beleive", issue.Message, StringComparison.Ordinal);
        Assert.NotEmpty(issue.Explanation);
        Assert.Equal("believe", issue.Suggestions[0].Text);
        Assert.Equal(14, issue.Suggestions[0].Start);
        Assert.Equal(7, issue.Suggestions[0].Length);
        Assert.False(result.EndsInDialogue);
    }

    [Fact]
    public void Clean_text_has_no_issues()
    {
        Assert.Empty(Shared.CheckParagraph("The rain had stopped by the time she reached the gate.").Issues);
    }

    [Fact]
    public void A_fix_can_cover_more_than_the_flagged_word()
    {
        ParagraphResult result = Shared.CheckParagraph("They walked to gether at last.");

        Issue issue = Assert.Single(result.Issues);
        Assert.Equal("gether", issue.Text);
        Assert.Contains(issue.Suggestions, s => s.Text == "together" && s.Start < issue.Start && s.Length == "to gether".Length);
    }

    [Fact]
    public void Each_kind_reports_its_own_rule()
    {
        ParagraphResult result = Shared.CheckParagraph("The colour of london pleased Vaelmoor.");

        Assert.Collection(
            result.Issues,
            i => { Assert.Equal(RuleIds.OtherDialect, i.RuleId); Assert.Equal(IssueKind.OtherDialectSpelling, i.Kind); Assert.Contains("American English", i.Message, StringComparison.Ordinal); },
            i => { Assert.Equal(RuleIds.Capitalization, i.RuleId); Assert.Equal("London", i.Suggestions[0].Text); },
            i => { Assert.Equal(RuleIds.UnknownName, i.RuleId); Assert.Equal(IssueKind.UnknownName, i.Kind); });
    }

    [Fact]
    public void Options_given_at_creation_are_used()
    {
        var british = new Checker(new CheckerOptions { Dialect = Dialect.British });

        Assert.Empty(british.CheckParagraph("The colour had faded.").Issues);
        Assert.Single(british.CheckParagraph("The color had faded.").Issues);
        Assert.Equal(QuoteStyle.Single, british.Options.EffectiveQuoteStyle);
    }

    [Fact]
    public void Options_can_be_overridden_for_one_check()
    {
        var context = new ParagraphContext { Options = Shared.Options with { Dialect = Dialect.British } };

        Assert.Empty(Shared.CheckParagraph("The colour had faded.", context).Issues);
        Assert.Single(Shared.CheckParagraph("The colour had faded.").Issues);
    }

    [Fact]
    public void A_rule_turned_off_reports_nothing()
    {
        const string Text = "The colour of london pleased Vaelmoor, she beleived.";
        foreach (RuleInfo rule in Shared.Rules)
        {
            CheckerOptions options = CheckerOptions.Default.WithRule(rule.Id, false);
            ParagraphResult result = Shared.CheckParagraph(Text, new ParagraphContext { Options = options });
            Assert.Equal(rule.Category == RuleCategory.Spelling ? 3 : 4, result.Issues.Count);
            Assert.DoesNotContain(result.Issues, i => i.RuleId == rule.Id);
        }
    }

    [Fact]
    public void A_category_turned_off_reports_nothing_unless_a_rule_is_turned_back_on()
    {
        const string Text = "The colour of london pleased Vaelmoor, she beleived.";
        CheckerOptions off = CheckerOptions.Default.WithCategory(RuleCategory.Spelling, false);
        Assert.Empty(Shared.CheckParagraph(Text, new ParagraphContext { Options = off }).Issues);

        CheckerOptions one = off.WithRule(RuleIds.Misspelling, true);
        Issue issue = Assert.Single(Shared.CheckParagraph(Text, new ParagraphContext { Options = one }).Issues);
        Assert.Equal("beleived", issue.Text);
    }

    [Fact]
    public void Unknown_rule_ids_are_ignored()
    {
        CheckerOptions options = CheckerOptions.Default.WithRule("grammar.from-a-later-version", false);

        Assert.Single(Shared.CheckParagraph("She could not beleive it.", new ParagraphContext { Options = options }).Issues);
    }

    [Fact]
    public void Max_suggestions_limits_and_zero_gives_none()
    {
        ParagraphResult two = Shared.CheckParagraph("A smal thing.", new ParagraphContext { Options = CheckerOptions.Default with { MaxSuggestions = 2 } });
        ParagraphResult none = Shared.CheckParagraph("A smal thing.", new ParagraphContext { Options = CheckerOptions.Default with { MaxSuggestions = 0 } });

        Assert.Equal(2, Assert.Single(two.Issues).Suggestions.Count);
        Assert.Empty(Assert.Single(none.Issues).Suggestions);
    }

    [Fact]
    public void Rules_are_listed_with_what_a_settings_screen_needs()
    {
        Assert.Equal(48, Shared.Rules.Count);
        Assert.Equal(27, Shared.Rules.Count(r => r.Category == RuleCategory.ConfusableWords));
        Assert.Equal(4, Shared.Rules.Count(r => r.Category == RuleCategory.Spelling));
        Assert.Equal(Shared.Rules.Count, Shared.Rules.Select(r => r.Id).Distinct().Count());
        Assert.All(Shared.Rules, rule =>
        {
            Assert.NotEmpty(rule.Name);
            Assert.NotEmpty(rule.Description);
            Assert.NotEmpty(rule.FlaggedExamples);
            Assert.NotEmpty(rule.AcceptedExamples);
            Assert.True(rule.EnabledByDefault);
        });
    }

    [Fact]
    public void Every_rule_flags_its_flagged_examples_and_accepts_the_others()
    {
        foreach (RuleInfo rule in Shared.Rules)
        {
            var on = new ParagraphContext { Options = CheckerOptions.Default.WithRule(rule.Id, true) };
            Assert.All(rule.FlaggedExamples, example => Assert.Contains(Shared.CheckParagraph(example, on).Issues, i => i.RuleId == rule.Id));
            Assert.All(rule.AcceptedExamples, example => Assert.DoesNotContain(Shared.CheckParagraph(example, on).Issues, i => i.RuleId == rule.Id));
        }
    }

    [Theory]
    [InlineData("She ate a apple.", RuleIds.Article, 8, 1, "an")]
    [InlineData("He could of gone home.", RuleIds.CouldOf, 9, 2, "have")]
    [InlineData("She opened  the door.", RuleIds.DoubledSpace, 10, 2, " ")]
    [InlineData("He left. she stayed.", RuleIds.SentenceCapital, 9, 3, "She")]
    [InlineData("I has no idea.", RuleIds.PronounVerb, 2, 3, "have")]
    [InlineData("They was late again.", RuleIds.DialectVerbForm, 5, 3, "were")]
    [InlineData("“Hello.” she said.", RuleIds.FullStopBeforeTag, 6, 1, ",")]
    [InlineData("“Hello,” She said.", RuleIds.CapitalizedTag, 9, 3, "she")]
    [InlineData("“Hello” she said.", RuleIds.MissingCommaBeforeTag, 6, 1, ",”")]
    [InlineData("“Hello”, she said.", RuleIds.CommaOutsideQuote, 6, 2, ",”")]
    public void Each_grammar_rule_reports_where_the_slip_is_and_how_to_fix_it(string text, string ruleId, int start, int length, string fix)
    {
        Issue issue = Assert.Single(Shared.CheckParagraph(text).Issues);

        Assert.Equal(ruleId, issue.RuleId);
        Assert.Equal((start, length), (issue.Start, issue.Length));
        Assert.Equal(fix, Assert.Single(issue.Suggestions).Text);
        Assert.Equal(IssueKind.Grammar, issue.Kind);
        Assert.NotEmpty(issue.Message);
        Assert.NotEmpty(issue.Explanation);
    }

    [Fact]
    public void Formal_rules_are_skipped_in_speech_unless_dialogue_checking_is_strict()
    {
        const string Text = "“They was here, and he could of stayed,” she said.";
        var strict = new ParagraphContext { Options = CheckerOptions.Default with { DialogueStrictness = DialogueStrictness.Strict } };

        Assert.Empty(Shared.CheckParagraph(Text).Issues);
        Assert.Equal([RuleIds.DialectVerbForm, RuleIds.CouldOf], Shared.CheckParagraph(Text, strict).Issues.Select(i => i.RuleId));
    }

    [Fact]
    public void Dialogue_punctuation_is_still_checked_when_dialogue_checking_is_off()
    {
        var off = new ParagraphContext { Options = CheckerOptions.Default with { DialogueStrictness = DialogueStrictness.Off } };

        Assert.Equal(RuleIds.FullStopBeforeTag, Assert.Single(Shared.CheckParagraph("“Helo.” she said.", off).Issues).RuleId);
    }

    [Fact]
    public void Dialogue_punctuation_is_checked_with_single_quote_marks_too()
    {
        var single = new ParagraphContext { Options = CheckerOptions.Default with { QuoteStyle = QuoteStyle.Single } };

        Issue issue = Assert.Single(Shared.CheckParagraph("‘Hello there’, she said.", single).Issues);
        Assert.Equal(RuleIds.CommaOutsideQuote, issue.RuleId);
        Assert.Equal(",’", issue.Suggestions[0].Text);
        Assert.Equal(RuleIds.MissingCommaBeforeTag, Assert.Single(Shared.CheckParagraph("‘Hello there’ she said.", single).Issues).RuleId);
    }

    [Fact]
    public void Unfinished_speech_is_not_flagged()
    {
        Assert.Empty(Shared.CheckParagraph("“I was going to—” she said.").Issues);
        Assert.Empty(Shared.CheckParagraph("“Well…” he said.").Issues);
        Assert.Empty(Shared.CheckParagraph("“Well...” he said.").Issues);
    }

    [Fact]
    public void A_grammar_issue_carries_its_rule_and_a_fix_for_the_whole_slip()
    {
        ParagraphResult result = Shared.CheckParagraph("She opened the the door.");

        Issue issue = Assert.Single(result.Issues);
        Assert.Equal(RuleIds.RepeatedWord, issue.RuleId);
        Assert.Equal(RuleCategory.Mechanics, issue.Category);
        Assert.Equal(IssueKind.Grammar, issue.Kind);
        Assert.Equal(15, issue.Start);
        Assert.Equal("the", issue.Text);
        Suggestion fix = Assert.Single(issue.Suggestions);
        Assert.Equal((11, 7, "the"), (fix.Start, fix.Length, fix.Text));
    }

    [Fact]
    public void Spelling_and_grammar_issues_come_in_text_order()
    {
        ParagraphResult result = Shared.CheckParagraph("She beleived the the door was smal.");

        Assert.Equal(["beleived", "the", "smal"], result.Issues.Select(i => i.Text));
    }

    [Fact]
    public void Grammar_rules_follow_rule_and_category_switches()
    {
        const string Text = "She opened the the door.";
        var ruleOff = new ParagraphContext { Options = CheckerOptions.Default.WithRule(RuleIds.RepeatedWord, false) };
        var categoryOff = new ParagraphContext { Options = CheckerOptions.Default.WithCategory(RuleCategory.Mechanics, false) };
        var spellingOff = new ParagraphContext { Options = CheckerOptions.Default.WithCategory(RuleCategory.Spelling, false) };

        Assert.Empty(Shared.CheckParagraph(Text, ruleOff).Issues);
        Assert.Empty(Shared.CheckParagraph(Text, categoryOff).Issues);
        Assert.Single(Shared.CheckParagraph(Text, spellingOff).Issues);
    }

    [Fact]
    public void Grammar_is_not_checked_in_speech_when_dialogue_checking_is_off()
    {
        const string Text = "“She opened the the door,” he said to to her.";
        var off = new ParagraphContext { Options = CheckerOptions.Default with { DialogueStrictness = DialogueStrictness.Off } };

        Assert.Equal(2, Shared.CheckParagraph(Text).Issues.Count);
        Assert.Equal(38, Assert.Single(Shared.CheckParagraph(Text, off).Issues).Start);
    }

    [Fact]
    public void Skipped_ranges_are_not_checked_by_grammar_rules()
    {
        const string Text = "She opened the the door.";

        Assert.Empty(Shared.CheckParagraph(Text, new ParagraphContext { SkipRanges = [new TextSpan(11, 7)] }).Issues);
    }

    [Fact]
    public void Word_sets_can_be_set_added_to_removed_from_and_cleared()
    {
        var checker = new Checker();
        Assert.Single(checker.CheckParagraph("Kaelith Vorn drew her blade.").Issues.Select(i => i.Kind).Distinct());

        checker.SetWords("world", ["Kaelith Vorn"]);
        Assert.Empty(checker.CheckParagraph("Kaelith Vorn drew her blade. It was Vorn's own.").Issues);
        Assert.Equal(["world"], checker.WordSetNames);

        checker.AddWords("world", ["Ysolde"]);
        Assert.Empty(checker.CheckParagraph("Ysolde watched Kaelith.").Issues);

        checker.RemoveWords("world", ["Vorn"]);
        Assert.Equal("Vorn", Assert.Single(checker.CheckParagraph("Kaelith Vorn drew her blade.").Issues).Text);

        checker.ClearWords("world");
        Assert.Equal(2, checker.CheckParagraph("Kaelith Vorn drew her blade.").Issues.Count);
        Assert.Empty(checker.WordSetNames);
    }

    [Fact]
    public void Custom_words_are_offered_as_suggestions()
    {
        var checker = new Checker();
        checker.SetWords("world", ["Kaelith"]);

        Assert.Equal("Kaelith", checker.CheckParagraph("Then Kaelth spoke.").Issues[0].Suggestions[0].Text);
        Assert.Equal("Kaelith", checker.Suggest("Kaelth")[0]);
    }

    [Fact]
    public void Dialogue_only_words_are_valid_only_in_speech()
    {
        var checker = new Checker();
        checker.SetWords("slang", ["wicious"], WordScope.DialogueOnly);

        Issue issue = Assert.Single(checker.CheckParagraph("“A wicious dog,” he said of the wicious dog.").Issues);
        Assert.Equal(32, issue.Start);
    }

    [Fact]
    public void A_check_can_name_the_word_sets_that_apply()
    {
        var checker = new Checker();
        checker.SetWords("world-a", ["Kaelith"]);
        checker.SetWords("world-b", ["Ysolde"]);
        const string Text = "Kaelith met Ysolde.";

        Assert.Empty(checker.CheckParagraph(Text).Issues);
        Assert.Equal("Ysolde", Assert.Single(checker.CheckParagraph(Text, Sets("world-a")).Issues).Text);
        Assert.Equal("Kaelith", Assert.Single(checker.CheckParagraph(Text, Sets("world-b", "no-such-set")).Issues).Text);
        Assert.Empty(checker.CheckParagraph(Text, Sets("world-b", "world-a")).Issues);
        Assert.Equal(2, checker.CheckParagraph(Text, Sets()).Issues.Count);

        checker.SetWords("world-a", ["Ysolde"]);
        Assert.Equal("Kaelith", Assert.Single(checker.CheckParagraph(Text, Sets("world-a")).Issues).Text);
    }

    [Fact]
    public void Dialogue_state_is_passed_from_paragraph_to_paragraph()
    {
        ParagraphResult first = Shared.CheckParagraph("“I ain’t goin’ back,");
        Assert.True(first.EndsInDialogue);
        Assert.Empty(first.Issues);

        var carried = new ParagraphContext { StartsInDialogue = true };
        ParagraphResult second = Shared.CheckParagraph("and nothin’ will make me.” He was sayin’ it twice.", carried);
        Assert.False(second.EndsInDialogue);
        Assert.Equal("sayin", Assert.Single(second.Issues).Text);
        Assert.Equal(2, Shared.CheckParagraph("and nothin’ will make me.” He was sayin’ it twice.").Issues.Count(i => i.Category == RuleCategory.Spelling));
    }

    [Fact]
    public void Dialogue_strictness_changes_what_speech_allows()
    {
        const string Text = "“I’m findin’ teh door,” he said.";
        ParagraphResult lenient = Shared.CheckParagraph(Text);
        ParagraphResult strict = Shared.CheckParagraph(Text, new ParagraphContext { Options = CheckerOptions.Default with { DialogueStrictness = DialogueStrictness.Strict } });
        ParagraphResult off = Shared.CheckParagraph(Text, new ParagraphContext { Options = CheckerOptions.Default with { DialogueStrictness = DialogueStrictness.Off } });

        Assert.Equal(["teh"], lenient.Issues.Select(i => i.Text));
        Assert.Equal(["findin", "teh"], strict.Issues.Select(i => i.Text));
        Assert.Empty(off.Issues);
    }

    [Fact]
    public void Skip_ranges_are_left_alone_and_dialogue_ranges_count_as_speech()
    {
        const string Text = "He read the word zzyx and thought, nothin’ for it.";
        Assert.Equal(2, Shared.CheckParagraph(Text).Issues.Count);

        var context = new ParagraphContext
        {
            SkipRanges = [new TextSpan(Text.IndexOf("zzyx", StringComparison.Ordinal), 4)],
            DialogueRanges = [TextSpan.FromBounds(Text.IndexOf("nothin", StringComparison.Ordinal), Text.Length)],
        };
        Assert.Empty(Shared.CheckParagraph(Text, context).Issues);
    }

    [Fact]
    public void A_document_carries_open_quotes_between_paragraphs()
    {
        IReadOnlyList<ParagraphResult> results = Shared.CheckDocument(
        [
            "“We ain’t going back.",
            "“Not now, and nothin’ doing.”",
            "He had nothin’ to say.",
            "***",
        ]);

        Assert.Equal(4, results.Count);
        Assert.True(results[0].EndsInDialogue);
        Assert.Empty(results[0].Issues);
        Assert.Empty(results[1].Issues);
        Assert.Equal("nothin", Assert.Single(results[2].Issues).Text);
        Assert.Empty(results[3].Issues);
    }

    [Fact]
    public void Document_paragraphs_can_be_skipped_or_carry_ranges()
    {
        IReadOnlyList<ParagraphResult> results = Shared.CheckDocument(
        [
            new DocumentParagraph("Chaptr One") { Skip = true },
            new DocumentParagraph("The sign read zzyx in red paint.") { SkipRanges = [new TextSpan(14, 4)] },
            new DocumentParagraph("Nothin’ for it, he thought.") { DialogueRanges = [new TextSpan(0, 15)] },
            new DocumentParagraph("A smal thing."),
        ]);

        Assert.Empty(results[0].Issues);
        Assert.Empty(results[1].Issues);
        Assert.Empty(results[2].Issues);
        Assert.Equal("smal", Assert.Single(results[3].Issues).Text);
    }

    [Fact]
    public void A_document_check_can_be_cancelled()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => Shared.CheckDocument(["One.", "Two."], null, source.Token));
    }

    [Fact]
    public void Single_words_can_be_checked_and_corrected()
    {
        Assert.True(Shared.IsValidWord("believe"));
        Assert.False(Shared.IsValidWord("beleive"));
        Assert.True(Shared.IsValidWord("don’t"));
        Assert.False(Shared.IsValidWord("colour"));
        Assert.True(Shared.IsValidWord("colour", CheckerOptions.Default with { Dialect = Dialect.British }));
        Assert.True(Shared.IsValidWord("colour", CheckerOptions.Default.WithRule(RuleIds.OtherDialect, false)));
        Assert.Equal("believe", Shared.Suggest("beleive")[0]);
        Assert.True(Shared.Suggest("beleive", CheckerOptions.Default with { MaxSuggestions = 1 }).Count <= 1);
    }

    [Fact]
    public void Options_compare_by_value()
    {
        CheckerOptions a = CheckerOptions.Default.WithRule(RuleIds.UnknownName, false) with { WordSets = ["a", "b"] };
        CheckerOptions b = CheckerOptions.Default.WithRule(RuleIds.UnknownName, false) with { WordSets = ["b", "a"] };

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, b.WithRule(RuleIds.UnknownName, true));
        Assert.NotEqual(a, b with { WordSets = null });
    }

    [Fact]
    public async Task Checks_and_word_changes_can_run_at_the_same_time()
    {
        var checker = new Checker();
        checker.SetWords("world", ["Kaelith"]);
        using var stop = new CancellationTokenSource();
        Task writer = Task.Run(() =>
        {
            for (int i = 0; i < 300; i++)
            {
                checker.SetWords("other", [$"Zorvath{i % 7}", "Ysolde"]);
            }

            stop.Cancel();
        });

        Task[] readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                // "Kaelith" is in a set that never changes, so it must never be flagged.
                Assert.DoesNotContain(checker.CheckParagraph("Kaelith met Ysolde on the beleived road.").Issues, i => i.Text == "Kaelith");
            }
        })).ToArray();

        await Task.WhenAll([writer, .. readers]);
        Assert.Equal("beleived", Assert.Single(checker.CheckParagraph("Kaelith met Ysolde on the beleived road.").Issues).Text);
    }

    private static ParagraphContext Sets(params string[] names) => new() { Options = CheckerOptions.Default with { WordSets = names } };
}
