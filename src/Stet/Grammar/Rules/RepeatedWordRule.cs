namespace Stet.Grammar.Rules;

/// <summary>The same word twice in a row: "She opened the the door."</summary>
internal sealed class RepeatedWordRule : GrammarRule
{
    // Pairs that are ordinary English: "he had had enough", "I know that that is true", "gave her her coat".
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "had", "that", "her", "you", "it", "is", "do", "so", "no", "now", "there", "well", "bye", "ha", "ho", "hee", "oh", "ah",
        "yes", "hush", "tut", "hear", "come", "go", "knock", "night", "boo", "blah", "yada", "pom", "tick", "choo", "aye", "nay",
        "please", "wait", "la", "na", "da", "ta", "bla", "woof", "quack", "chop", "hip",
    };

    // Words doubled for emphasis: "very very tired", "never ever ever", "far far away".
    private static readonly HashSet<string> Emphatic = new(StringComparer.Ordinal)
    {
        "very", "really", "so", "too", "far", "long", "much", "more", "many", "never", "ever", "again", "quite", "always", "almost",
        "over", "on", "round", "down", "up", "away", "back", "nearly", "pretty", "awfully", "terribly",
    };

    private static readonly RuleInfo Description = new(
        RuleIds.RepeatedWord,
        "Repeated words",
        "Flags a word typed twice in a row. Repeats that are ordinary English, such as \"had had\" and \"that that\", and repeats for emphasis, such as \"very very\", are left alone.",
        RuleCategory.Mechanics,
        true,
        false,
        ["She opened the the door.", "He walked to to the gate.", "They were were late again."],
        ["He had had enough.", "I know that that is true.", "It was a long, long road.", "She was very very tired.", "No, no, not that one."]);

    public override RuleInfo Info => Description;

    public override void Check(Sentence sentence, List<Finding> findings)
    {
        for (int i = 0; i + 1 < sentence.Count; i++)
        {
            if (!sentence.IsWord(i) || !sentence.IsWord(i + 1) || !sentence.TouchesNext(i))
            {
                continue;
            }

            string word = sentence.Lower(i);
            if (word != sentence.Lower(i + 1) || Allowed.Contains(word))
            {
                continue;
            }

            // Describing words are repeated for emphasis ("a cold cold night", "very very"), and names can be
            // doubled ("Walla Walla").
            WordType type = sentence.Type(i + 1);
            if (type is WordType.Adjective or WordType.Other || (type == WordType.Adverb && Emphatic.Contains(word)))
            {
                continue;
            }

            // "Walla Walla", "Naught Naught One": two capitals are a name. Only a function word doubled at the
            // very start ("The The door") is taken for a slip.
            if (sentence.IsCapitalized(i + 1) && word != "i" && !word.StartsWith("i'", StringComparison.Ordinal)
                && !(i == 0 && sentence.Traits(i) != WordTraits.None))
            {
                continue;
            }

            // A name and then the plain word ("On Mount Doom doom shall fall"), a made-up sound ("whop whop",
            // "chek chek"), a number word ("a thousand thousand years") or a chain of possessives ("her
            // mother's mother's mother").
            if ((i > 0 && sentence.IsCapitalized(i)) || sentence.WordIndex(i) < 0 || (word.EndsWith("'s", StringComparison.Ordinal) && type == WordType.Noun)
                || word is "thousand" or "hundred" or "million" or "dozen" or "heh" or "hm" or "hmm" or "mm" or "um" or "uh" or "er" or "ow" or "ooh" or "aah" or "tsk" or "ya" or "yeah")
            {
                continue;
            }

            var first = sentence.Token(i);
            var second = sentence.Token(i + 1);
            string written = sentence.Written(i).ToString();
            findings.Add(new Finding(
                second.Start,
                second.Length,
                string.Concat(CharClassQuote.Open, sentence.Written(i + 1).ToString(), CharClassQuote.Close, " is repeated."),
                "The same word appears twice in a row.",
                [Replace(first.Start, second.End - first.Start, written)]));
        }
    }
}
