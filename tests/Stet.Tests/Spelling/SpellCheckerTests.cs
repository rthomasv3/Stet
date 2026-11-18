using Stet.Segmentation;
using Stet.Spelling;

namespace Stet.Tests.Spelling;

public class SpellCheckerTests
{
    private static readonly Lexicon Words = Lexicon.LoadEmbedded();
    private static readonly BuiltInLists Lists = BuiltInLists.LoadEmbedded();
    private static readonly SpellingOptions US = new();
    private static readonly SpellingOptions GB = new() { Dialect = Dialect.British, QuoteStyle = QuoteStyle.Double };

    private static SpellChecker NewChecker() => new(Words, Lists);

    private static string[] Flagged(string text, SpellingOptions? options = null, SpellChecker? checker = null) =>
        (checker ?? NewChecker()).CheckParagraph(text, options ?? US).Issues.Select(i => $"{i.Kind}:{i.Word}").ToArray();

    private static void AssertClean(string text, SpellingOptions? options = null, SpellChecker? checker = null) =>
        Assert.Empty(Flagged(text, options, checker));

    [Theory]
    [InlineData("The cat sat on the mat, and nobody minded.")]
    [InlineData("Don’t stop; it’s nearly five o’clock, isn't it?")]
    [InlineData("She visited London with Elizabeth in 1920.")]
    [InlineData("THE DOOR SLAMMED. “GET OUT!” LONDON BURNED.")]
    [InlineData("Mr. J. R. R. Tolkien wrote to www.example.com on the 21st.")]
    [InlineData("A well-known, half-forgotten, non-magical T-shirt.")]
    [InlineData("The dog’s bowl, the dogs’ bowls, James’s hat and London’s fog.")]
    [InlineData("That’ve been said; who’d know? It’ll do. There’re more.")]
    [InlineData("***")]
    public void AcceptsCorrectText(string text)
    {
        AssertClean(text);
    }

    [Fact]
    public void FlagsAMisspellingWithItsPosition()
    {
        SpellingIssue issue = Assert.Single(NewChecker().CheckParagraph("The qwick fox.", US).Issues);

        Assert.Equal((4, 5, SpellingKind.Misspelling, "qwick"), (issue.Start, issue.Length, issue.Kind, issue.Word));
        Assert.Equal("spelling.misspelling", issue.RuleId);
        Assert.Equal("quick", issue.Suggestions[0].Text);
        Assert.Equal((4, 5), (issue.Suggestions[0].Start, issue.Suggestions[0].Length));
        Assert.InRange(issue.Suggestions.Count, 1, 5);
    }

    [Fact]
    public void PositionsCountUtf16Units()
    {
        SpellingIssue issue = Assert.Single(NewChecker().CheckParagraph("😀 “A qwick fox.”", US).Issues);

        Assert.Equal(6, issue.Start);
    }

    [Fact]
    public void OtherDialectSpellingsAreTheirOwnKind()
    {
        Assert.Equal(["OtherDialect:colour"], Flagged("The colour faded."));
        Assert.Equal(["OtherDialect:color"], Flagged("The color faded.", GB));
        Assert.Equal(["OtherDialect:organize"], Flagged("They organize it.", GB));
        AssertClean("They organize it.", GB with { Dialect = Dialect.BritishIze });
        AssertClean("The colour faded.", US with { FlagOtherDialects = false });

        SpellingIssue issue = NewChecker().CheckParagraph("The colour faded.", US).Issues[0];
        Assert.Equal("spelling.other-dialect", issue.RuleId);
        Assert.Equal("color", issue.Suggestions[0].Text);
    }

    [Fact]
    public void WordsThatNeedACapitalAreFlaggedWithTheFix()
    {
        SpellingIssue issue = Assert.Single(NewChecker().CheckParagraph("We left london at dawn.", US).Issues);

        Assert.Equal(SpellingKind.Capitalization, issue.Kind);
        Assert.Equal("London", Assert.Single(issue.Suggestions).Text);
        Assert.Equal("spelling.capitalization", issue.RuleId);
    }

    [Fact]
    public void UnknownCapitalizedWordsAreReportedAsProbableNames()
    {
        Assert.Equal(["UnknownName:Aragost", "UnknownName:Zorvath"], Flagged("Then Aragost rode to Zorvath."));
        Assert.Equal(["UnknownName:Aragost"], Flagged("Aragost rode north."));

        SpellingIssue issue = NewChecker().CheckParagraph("Then Aragost rode.", US).Issues[0];
        Assert.Equal("spelling.unknown-name", issue.RuleId);
    }

    [Fact]
    public void CapitalizedSlipsAreStillMisspellings()
    {
        Assert.Equal(["Misspelling:Teh"], Flagged("Teh door opened."));
        Assert.Equal(["Misspelling:Lodnon"], Flagged("We left Lodnon at dawn."));
        Assert.Equal(["Misspelling:Becuase"], Flagged("“Becuase I said so,” she said."));

        SpellChecker checker = NewChecker();
        checker.SetWords("world", ["Kaelith"]);
        Assert.Equal(["Misspelling:Kaelth"], Flagged("Then Kaelth spoke.", checker: checker));
    }

    [Fact]
    public void MixedCaseMustMatchExactly()
    {
        AssertClean("An iPhone lay on the table at McDonald’s.");
        Assert.Equal(["Misspelling:mIxed"], Flagged("A mIxed word."));
    }

    [Fact]
    public void CustomWordsKeepTheirCase()
    {
        SpellChecker checker = NewChecker();
        checker.SetWords("world", ["Kaelith Vorn", "liveship"]);

        AssertClean("Kaelith and Vorn boarded the liveship. KAELITH! Liveship sailors. Kaelith’s liveship’s deck.", checker: checker);
        SpellingIssue issue = Assert.Single(checker.CheckParagraph("Then kaelith spoke.", US).Issues);
        Assert.Equal(SpellingKind.Capitalization, issue.Kind);
        Assert.Equal("Kaelith", issue.Suggestions[0].Text);
    }

    [Fact]
    public void CustomWordsAppearInSuggestions()
    {
        SpellChecker checker = NewChecker();
        checker.SetWords("world", ["Kaelith"]);

        SpellingIssue issue = Assert.Single(checker.CheckParagraph("Then Kaelth spoke.", US).Issues);

        Assert.Equal("Kaelith", issue.Suggestions[0].Text);
    }

    [Fact]
    public void WordSetsCanBeChangedAndCleared()
    {
        SpellChecker checker = NewChecker();
        checker.SetWords("a", ["zorple"]);
        checker.AddWords("a", ["blenk"]);
        AssertClean("A zorple and a blenk.", checker: checker);

        checker.RemoveWords("a", ["zorple"]);
        Assert.Equal(["Misspelling:zorple"], Flagged("A zorple and a blenk.", checker: checker));

        checker.SetWords("a", ["zorple"]);
        Assert.Equal(["Misspelling:blenk"], Flagged("A zorple and a blenk.", checker: checker));

        checker.ClearWords("a");
        Assert.Equal(2, Flagged("A zorple and a blenk.", checker: checker).Length);
    }

    [Fact]
    public void DialogueOnlyWordsStayFlaggedInNarration()
    {
        SpellChecker checker = NewChecker();
        checker.SetWords("slang", ["wicious"], WordScope.DialogueOnly);

        Assert.Equal(["Misspelling:wicious"], Flagged("A wicious dog. “A wicious dog,” he said.", checker: checker));
    }

    [Theory]
    [InlineData("“I dunno, I’m gonna go,” he said.")]
    [InlineData("“We was runnin’ and shoutin’ all night,” she said.")]
    [InlineData("“Give ’em a cup o’ tea, ’cause it’s cold,” she said.")]
    [InlineData("“Lemme see. Coulda been worse,” he said.")]
    [InlineData("“I didn’ know, an’ I couldn’ ask,” he said.")]
    public void LenientDialogueAcceptsInformalSpeech(string text)
    {
        AssertClean(text);
    }

    [Fact]
    public void InformalSpeechIsFlaggedInNarrationAndStrictDialogue()
    {
        Assert.Equal(["Misspelling:shoutin", "Misspelling:coulda"], Flagged("He was shoutin’ and it coulda gone badly."));
        Assert.Equal(["Misspelling:shoutin"], Flagged("“Stop shoutin’,” he said.", US with { Dialogue = DialogueStrictness.Strict }));
    }

    [Fact]
    public void LenientDialogueStillCatchesTypos()
    {
        Assert.Equal(["Misspelling:qwick"], Flagged("“Be qwick about it, I’m gonna go,” he said."));
    }

    [Fact]
    public void DialogueOffSkipsSpeechOnly()
    {
        Assert.Equal(["Misspelling:narratoin"], Flagged("“Be qwick,” he said in narratoin.", US with { Dialogue = DialogueStrictness.Off }));
    }

    [Fact]
    public void SingleQuoteStyleFindsDialogueToo()
    {
        var options = new SpellingOptions { Dialect = Dialect.British };

        AssertClean("‘I dunno, I’m gonna go,’ he said.", options);
        Assert.Equal(["Misspelling:coulda"], Flagged("‘I dunno,’ he said, and it coulda gone badly.", options));
    }

    [Theory]
    [InlineData("“Nooooo!” she cried. “Sooo good. Hmmmmm.”")]
    [InlineData("“W-w-what? I-I don’t know. Th-thank you.”")]
    [InlineData("Hmm. Ugh. Shh! Psst, over here.")]
    [InlineData("Thou hast spoken, and he hath heard; ’tis done.")]
    public void FictionConventionsAreAccepted(string text)
    {
        AssertClean(text);
    }

    [Theory]
    [InlineData("He maketh and she springeth; the river runneth and time fleeth.")]
    [InlineData("A dark-browed, long-limbed, good-naturedly red-nosed man.")]
    [InlineData("The bellowings and creakings went on slurringly, unwinkingly.")]
    [InlineData("An unlidded, buglike, wickless thing.")]
    [InlineData("He didn‘t care; it wasn´t his. A bona fide lapis lazuli.")]
    [InlineData("They walked among the trees, amongst friends, under a grey sky; she had learnt and dreamt.")]
    [InlineData("A nonuniversity, premilitary friend in a semitrance sang “Three O’Clock in the Morning”. Ay, ay.")]
    public void RegularDerivationsAndVariantsAreAccepted(string text)
    {
        AssertClean(text);
        AssertClean(text, GB);
    }

    [Theory]
    [InlineData("The bigest one.", "bigest")]
    [InlineData("A qwickings sound.", "qwickings")]
    [InlineData("An unqwicked thing.", "unqwicked")]
    [InlineData("A dark-qwicked man.", "qwicked")]
    [InlineData("We saw le chat.", "le")]
    [InlineData("A preqwick thing.", "preqwick")]
    [InlineData("Come overthe hill.", "overthe")]
    public void DerivationRulesDoNotExcuseRealErrors(string text, string flagged)
    {
        Assert.Equal([$"Misspelling:{flagged}"], Flagged(text));
    }

    [Fact]
    public void StretchedAndStutteredTyposAreStillFlagged()
    {
        Assert.Equal(["Misspelling:qwiiick"], Flagged("A qwiiick step."));
        Assert.Equal(["Misspelling:qwick"], Flagged("Q-q-qwick now."));
    }

    [Fact]
    public void HyphenatedWordsFlagOnlyTheirBadParts()
    {
        SpellingIssue issue = Assert.Single(NewChecker().CheckParagraph("A half-qwick answer.", US).Issues);

        Assert.Equal((7, 5, "qwick"), (issue.Start, issue.Length, issue.Word));
    }

    [Fact]
    public void SkipRangesAreNotChecked()
    {
        var context = new SpellingContext { SkipRanges = [new TextSpan(4, 5)] };

        Assert.Empty(NewChecker().CheckParagraph("The qwick fox.", US, context).Issues);
    }

    [Fact]
    public void CallerDialogueRangesGetDialogueTreatment()
    {
        var context = new SpellingContext { DialogueRanges = [new TextSpan(0, 12)] };

        Assert.Empty(NewChecker().CheckParagraph("I coulda won, he thought.", US, context).Issues);
        Assert.Single(NewChecker().CheckParagraph("I coulda won, he thought.", US).Issues);
    }

    [Fact]
    public void DocumentCarriesOpenQuotesBetweenParagraphs()
    {
        string[] paragraphs = ["“I dunno where it began.", "“But I’m gonna find out,” he said. He coulda."];

        List<SpellingResult> results = NewChecker().CheckDocument(paragraphs, US);

        Assert.True(results[0].EndsInDialogue);
        Assert.Empty(results[0].Issues);
        Assert.Equal("coulda", Assert.Single(results[1].Issues).Word);
    }

    [Fact]
    public void RareLookAlikesOfCommonSlipsAreFlagged()
    {
        SpellingIssue issue = Assert.Single(NewChecker().CheckParagraph("Mark the calender.", US).Issues);

        Assert.Equal("calendar", issue.Suggestions[0].Text);
    }

    [Theory]
    [InlineData("recieve", "receive")]
    [InlineData("definately", "definitely")]
    [InlineData("teh", "the")]
    [InlineData("freind", "friend")]
    [InlineData("wich", "which")]
    [InlineData("becuase", "because")]
    [InlineData("alot", "a lot")]
    [InlineData("sudenly", "suddenly")]
    [InlineData("thier", "their")]
    [InlineData("shoudl", "should")]
    [InlineData("swrod", "sword")]
    [InlineData("whisperd", "whispered")]
    public void FirstSuggestionIsTheLikelyWord(string wrong, string expected)
    {
        Assert.Equal(expected, NewChecker().Suggest(wrong, US)[0]);
    }

    [Fact]
    public void SuggestionsKeepTheWordsCase()
    {
        SpellChecker checker = NewChecker();

        Assert.Equal("The", checker.Suggest("Teh", US)[0]);
        Assert.Equal("THE", checker.Suggest("TEH", US)[0]);
        Assert.Equal("London", checker.Suggest("Lodnon", US)[0]);
        Assert.Equal("don’t", checker.CheckParagraph("I dno’t know.", US).Issues[0].Suggestions[0].Text);
    }

    [Fact]
    public void LowercaseSlipsAreNotOfferedNames()
    {
        IReadOnlyList<string> suggestions = NewChecker().Suggest("glaas", US);

        Assert.Equal("glass", suggestions[0]);
        Assert.DoesNotContain("Glass", suggestions);
    }

    [Fact]
    public void TripledLettersInLongWordsAreSlipsOutsideDialogue()
    {
        Assert.Equal(["Misspelling:businesss", "Misspelling:noddded"], Flagged("The businesss was closed and he noddded."));
        AssertClean("Nooo. Sooo good. The crowd went “Businessss!” and he said, “Pleeease.”");
    }

    [Fact]
    public void SuggestionsFollowTheDialect()
    {
        Assert.Contains("color", NewChecker().Suggest("colro", US));
        Assert.DoesNotContain("colour", NewChecker().Suggest("colro", US));
        Assert.Contains("colour", NewChecker().Suggest("coluor", GB));
    }

    [Fact]
    public void SplitsAndJoinsAreOffered()
    {
        SpellChecker checker = NewChecker();

        Assert.Contains("in front", checker.Suggest("infront", US));

        SpellingIssue issue = Assert.Single(checker.CheckParagraph("He looked every wher for it.", US).Issues);
        Assert.Equal("wher", issue.Word);
        Assert.Contains(issue.Suggestions, s => s.Text == "everywher" || s.Text == "where");
    }

    [Fact]
    public void JoiningWithANeighbourCanFixAWord()
    {
        SpellingIssue issue = Assert.Single(NewChecker().CheckParagraph("They went up stairs to bedd room.", US).Issues);

        Assert.Equal("bedd", issue.Word);
        Assert.Contains(issue.Suggestions, s => s.Text == "bed");
    }

    [Fact]
    public void OffensiveWordsAreValidButNeverSuggested()
    {
        var offensive = Words.Words.Where(w => w.Value.NeverSuggest && w.Value.IsIn(Dialect.American) && w.Key.Length >= 5).Select(w => w.Key).Take(5).ToList();
        Assert.NotEmpty(offensive);
        SpellChecker checker = NewChecker();

        foreach (string word in offensive)
        {
            Assert.True(checker.IsValidWord(word, US));
            string typo = word[..^1] + (word[^1] == 'x' ? 'q' : 'x');
            Assert.DoesNotContain(word, checker.Suggest(typo, US));
        }
    }

    [Fact]
    public void WordHelpersWork()
    {
        SpellChecker checker = NewChecker();

        Assert.True(checker.IsValidWord("don’t", US));
        Assert.True(checker.IsValidWord("well-known", US));
        Assert.False(checker.IsValidWord("qwick", US));
        Assert.False(checker.IsValidWord("colour", US));
    }

    [Fact]
    public void ConcurrentChecksAndWordSetChangesAreSafe()
    {
        SpellChecker checker = NewChecker();
        string text = "Kaelith drew the shortsword qwickly, and Vorn laughed at teh sight.";

        Parallel.For(0, 200, i =>
        {
            if (i % 10 == 0)
            {
                checker.SetWords("world", i % 20 == 0 ? ["Kaelith", "Vorn"] : ["Vorn"]);
            }

            SpellingResult result = checker.CheckParagraph(text, US);
            Assert.InRange(result.Issues.Count, 2, 4); // 2 with both names known, 4 before any set is in place
        });
    }

    [Fact]
    public void LargeWordSetsAreCheapToReplace()
    {
        SpellChecker checker = NewChecker();
        string[] words = Enumerable.Range(0, 20000).Select(i => "Name" + i.ToString(System.Globalization.CultureInfo.InvariantCulture).Replace('0', 'a').Replace('1', 'b').Replace('2', 'c')).ToArray();
        checker.SetWords("warm-up", words);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        checker.SetWords("world", words);
        clock.Stop();

        // Target is 50 ms on a developer machine; allow slack for slower test runners.
        Assert.True(clock.ElapsedMilliseconds < 250, $"Replacing 20,000 words took {clock.ElapsedMilliseconds} ms");
    }
}
