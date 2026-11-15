namespace Stet.Grammar.Rules;

/// <summary>
/// "wont" and "cant" written for "won't" and "can't". Both are words in their own right ("as was his
/// wont", "pious cant"), so the spelling check passes them; before a verb they can only be the contraction.
/// </summary>
internal sealed class MissingApostropheRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.MissingApostrophe,
        "Missing apostrophe",
        "Flags \"wont\" and \"cant\" directly before a verb, where \"won't\" and \"can't\" are meant. The words themselves (\"as was his wont\", \"he was wont to walk\") are left alone.",
        RuleCategory.Mechanics,
        true,
        false,
        ["He wont tell anyone, I promise.", "She cant have gone far.", "I cant see a thing."],
        ["He won't tell anyone.", "He rose early, as was his wont.", "She was wont to walk at dusk.", "It was pious cant and nothing more.", "His wont was to rise early."]);

    private static readonly HashSet<string> Owners = new(StringComparer.Ordinal) { "his", "her", "their", "my", "your", "our", "its", "the", "was", "were", "is", "are", "be", "been", "as", "of", "and", "such", "that", "this", "pious", "mere" };

    public override RuleInfo Info => Description;

    public override void Check(Sentence sentence, List<Finding> findings)
    {
        for (int i = 0; i + 1 < sentence.Count; i++)
        {
            if (!sentence.Is(i, "wont", "cant") || !sentence.IsWord(i + 1) || !sentence.TouchesNext(i))
            {
                continue;
            }

            if (i > 0 && sentence.IsWord(i - 1) && (Owners.Contains(sentence.Lower(i - 1)) || sentence.Lower(i - 1).EndsWith("'s", StringComparison.Ordinal) || sentence.Type(i - 1) == WordType.Adjective))
            {
                continue;
            }

            // The next word must be a verb in its plain form, and nothing else ("cant hook" is a tool).
            bool plainVerb = sentence.Is(i + 1, "be", "have", "do", "go") || ((sentence.Forms(i + 1) & WordForms.VerbBase) != 0 && sentence.Type(i + 1) == WordType.Verb);
            if (!plainVerb)
            {
                continue;
            }

            var token = sentence.Token(i);
            string written = sentence.Written(i).ToString();
            char apostrophe = sentence.Text.Contains(Segmentation.CharClass.RightSingleQuote) ? Segmentation.CharClass.RightSingleQuote : '\'';
            string fix = written[..^1] + apostrophe + written[^1];
            findings.Add(new Finding(
                token.Start,
                token.Length,
                $"{CharClassQuote.Open}{written}{CharClassQuote.Close} should be {CharClassQuote.Open}{fix}{CharClassQuote.Close}.",
                "Before a verb this is the short form of \"will not\" or \"cannot\", which takes an apostrophe.",
                [Replace(token.Start, token.Length, fix)]));
        }
    }
}
