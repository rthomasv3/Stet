namespace Stet.Grammar.Rules;

/// <summary>"could of gone" for "could have gone".</summary>
internal sealed class CouldOfRule : GrammarRule
{
    private static readonly Pattern Slip = new(
        Match.Word("could", "should", "would", "must", "might", "couldn't", "shouldn't", "wouldn't", "mustn't", "mightn't"),
        Match.Word("of"),
        Match.Any(Match.Form(WordForms.VerbPastParticiple), Match.Word("been", "had", "done", "gone")));

    private static readonly RuleInfo Description = new(
        RuleIds.CouldOf,
        "Could of",
        "Flags \"could of\", \"should of\", \"would of\" and the like before a verb, where \"have\" is meant. It is treated as formal: inside dialogue it is only flagged when dialogue checking is strict, because writers use it on purpose for a character's voice.",
        RuleCategory.Mechanics,
        true,
        true,
        ["He could of gone home.", "She should of known better.", "They must of been tired."],
        ["He could have gone home.", "He could, of course, go home.", "She would of course be late.", "“I could of killed him,” he said."]);

    public override RuleInfo Info => Description;

    public override void Check(Sentence sentence, List<Finding> findings)
    {
        foreach (int i in Slip.FindIn(sentence))
        {
            if (!sentence.TouchesNext(i) || !sentence.TouchesNext(i + 1))
            {
                continue;
            }

            var of = sentence.Token(i + 1);
            string fix = sentence.IsAllCapitals(i + 1) ? "HAVE" : "have";
            findings.Add(new Finding(
                of.Start,
                of.Length,
                $"{CharClassQuote.Open}{sentence.Written(i)} {sentence.Written(i + 1)}{CharClassQuote.Close} should be {CharClassQuote.Open}{sentence.Written(i)} {fix}{CharClassQuote.Close}.",
                "\"Could've\" sounds like \"could of\", but the word is \"have\".",
                [Replace(of.Start, of.Length, fix)]));
        }
    }
}
