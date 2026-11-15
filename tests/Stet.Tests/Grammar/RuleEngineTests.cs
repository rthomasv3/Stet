using Stet.Grammar;
using Stet.Grammar.Rules;
using Stet.Segmentation;
using Stet.Spelling;

namespace Stet.Tests.Grammar;

public class RuleEngineTests
{
    private static readonly Lexicon Words = Lexicon.LoadEmbedded();
    private static readonly GrammarEngine Engine = GrammarEngine.CreateDefault(Words);

    private static List<Sentence> Sentences(string text, CustomWords? custom = null, DialogueResult? dialogue = null) =>
        Engine.Sentences(text, Tokenizer.Tokenize(text), dialogue, custom ?? CustomWords.Empty).ToList();

    private static List<Issue> Check(string text, DialogueStrictness strictness = DialogueStrictness.Lenient, GrammarEngine? engine = null)
    {
        engine ??= Engine;
        var issues = new List<Issue>();
        DialogueResult dialogue = DialogueDetector.Detect(text, QuoteStyle.Double);
        engine.Check(text, Tokenizer.Tokenize(text), dialogue, CustomWords.Empty, [.. engine.Rules.Select(_ => true)], strictness, QuoteStyle.Double, issues);
        return issues;
    }

    public static IEnumerable<object[]> FlaggedExamples =>
        Engine.Rules.SelectMany(rule => rule.Info.FlaggedExamples.Select(example => new object[] { rule.Info.Id, example }));

    public static IEnumerable<object[]> AcceptedExamples =>
        Engine.Rules.SelectMany(rule => rule.Info.AcceptedExamples.Select(example => new object[] { rule.Info.Id, example }));

    [Theory]
    [MemberData(nameof(FlaggedExamples))]
    public void A_rule_flags_each_of_its_must_flag_examples(string ruleId, string example)
    {
        Assert.Contains(Check(example), issue => issue.RuleId == ruleId);
    }

    [Theory]
    [MemberData(nameof(AcceptedExamples))]
    public void A_rule_leaves_each_of_its_must_not_flag_examples_alone(string ruleId, string example)
    {
        Assert.DoesNotContain(Check(example), issue => issue.RuleId == ruleId);
    }

    [Fact]
    public void Every_rule_is_fully_described()
    {
        Assert.All(Engine.Rules, rule =>
        {
            Assert.Matches("^[a-z-]+\\.[a-z-]+$", rule.Info.Id);
            Assert.NotEqual(RuleCategory.Spelling, rule.Info.Category);
            Assert.NotEmpty(rule.Info.Name);
            Assert.NotEmpty(rule.Info.Description);
            Assert.NotEmpty(rule.Info.FlaggedExamples);
            Assert.NotEmpty(rule.Info.AcceptedExamples);
        });
        Assert.Equal(Engine.Rules.Count, Engine.Rules.Select(r => r.Info.Id).Distinct().Count());
    }

    [Fact]
    public void A_paragraph_is_split_into_tagged_sentences()
    {
        List<Sentence> sentences = Sentences("The dog barked. She did not look up.");

        Assert.Equal(2, sentences.Count);
        Assert.Equal(4, sentences[0].Count);
        Assert.Equal(WordType.Determiner, sentences[0].Type(0));
        Assert.Equal(WordType.Noun, sentences[0].Type(1));
        Assert.Equal(WordType.Verb, sentences[0].Type(2));
        Assert.Equal(WordType.Punctuation, sentences[0].Type(3));
        Assert.Equal("she", sentences[1].Lower(0));
        Assert.True(sentences[1].IsCapitalized(0));
    }

    [Fact]
    public void A_sentence_knows_its_function_words_and_word_forms()
    {
        Sentence s = Sentences("They were walking home with her dogs.")[0];

        Assert.True(s.Traits(0).HasFlag(WordTraits.Pronoun | WordTraits.Subject | WordTraits.Plural));
        Assert.True(s.Traits(1).HasFlag(WordTraits.Be | WordTraits.Past));
        Assert.True(s.Forms(2).HasFlag(WordForms.VerbGerund));
        Assert.True(s.Traits(5).HasFlag(WordTraits.Possessive));
        Assert.True(s.Forms(6).HasFlag(WordForms.NounPlural));
        Assert.Equal(WordTraits.None, s.Traits(7));
        Assert.True(s.Is(0, "we", "they"));
        Assert.False(s.Is(7, "."));
    }

    [Fact]
    public void A_pattern_matches_words_side_by_side_and_stops_at_punctuation()
    {
        var pattern = new Pattern(Match.Word("could", "should", "would"), Match.Word("of"));

        Assert.Equal([1], pattern.FindIn(Sentences("He could of gone.")[0]));
        Assert.Equal([3], pattern.FindIn(Sentences("Yes, she Should Of known.")[0]));
        Assert.Empty(pattern.FindIn(Sentences("He could, of course, go.")[0]));
        Assert.Empty(pattern.FindIn(Sentences("He could.")[0]));
        Assert.Equal(2, pattern.Length);
    }

    [Fact]
    public void Pattern_tests_cover_types_traits_forms_and_combinations()
    {
        Sentence s = Sentences("Their dogs barked loudly.")[0];
        var possessiveThenPlural = new Pattern(Match.Trait(WordTraits.Possessive), Match.All(Match.Type(WordType.Noun), Match.Form(WordForms.NounPlural)));
        var verbThenNotNoun = new Pattern(Match.Type(WordType.Verb), Match.Not(Match.Type(WordType.Noun)));
        var either = new Pattern(Match.Any(Match.Word("cats"), Match.Word("dogs")), Match.AnyWord);

        Assert.Equal([0], possessiveThenPlural.FindIn(s));
        Assert.Equal([2], verbThenNotNoun.FindIn(s));
        Assert.Equal([1], either.FindIn(s));
    }

    [Fact]
    public void A_repeat_across_a_sentence_break_or_punctuation_is_not_flagged()
    {
        Assert.Empty(Check("He knew it. It was late."));
        Assert.Empty(Check("What a day, day after day."));
        Assert.Empty(Check("In he came (came running, in fact)."));
    }

    [Fact]
    public void A_repeat_is_found_whatever_the_capitals_and_across_a_line_of_spaces()
    {
        Assert.Single(Check("The the door was open."));
        Assert.Single(Check("She walked to  to the gate."), issue => issue.RuleId == RuleIds.RepeatedWord);
    }

    [Theory]
    [InlineData("He was not not going.", 1)]
    [InlineData("I'm I'm sorry.", 1)]
    [InlineData("It's it's cold.", 1)]
    [InlineData("They sailed to Walla Walla in spring.", 0)]
    [InlineData("It was a cold cold night.", 0)]
    [InlineData("It was far far away.", 0)]
    [InlineData("Never never do that.", 0)]
    [InlineData("Now now, child.", 0)]
    [InlineData("I gave her her coat and told you you were wrong.", 0)]
    public void Repeats_for_emphasis_names_and_ordinary_pairs_are_told_from_slips(string text, int expected)
    {
        Assert.Equal(expected, Check(text).Count);
    }

    private sealed class FormalRule : GrammarRule
    {
        public override RuleInfo Info { get; } = new("test.formal", "Formal", "Flags every 'ain't'.", RuleCategory.Agreement, true, true, ["It ain't so."], ["It is not so."]);

        public override void Check(Sentence sentence, List<Finding> findings)
        {
            for (int i = 0; i < sentence.Count; i++)
            {
                if (sentence.Is(i, "ain't"))
                {
                    findings.Add(new Finding(sentence.Token(i).Start, sentence.Token(i).Length, "Informal.", "Informal.", []));
                }
            }
        }
    }

    [Fact]
    public void A_formal_rule_skips_speech_when_dialogue_checking_is_lenient()
    {
        var engine = new GrammarEngine(WordData.LoadEmbedded(Words), FunctionWords.LoadEmbedded(), [new FormalRule()]);
        const string Text = "“It ain’t so,” he said, though it ain’t for him to say.";

        Assert.Single(Check(Text, DialogueStrictness.Lenient, engine));
        Assert.Equal(2, Check(Text, DialogueStrictness.Strict, engine).Count);
        Assert.Single(Check(Text, DialogueStrictness.Off, engine));
        Assert.Equal(2, Check("He said it ain’t so, and it ain’t.", DialogueStrictness.Lenient, engine).Count);
    }
}
