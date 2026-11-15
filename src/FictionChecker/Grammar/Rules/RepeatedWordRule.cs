namespace FictionChecker.Grammar.Rules;

/// <summary>The same word twice in a row: "She opened the the door."</summary>
internal sealed class RepeatedWordRule : GrammarRule
{
    // Pairs that are ordinary English: "he had had enough", "I know that that is true", "gave her her coat".
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "had", "that", "her", "you", "it", "is", "do", "so", "no", "now", "there", "well", "bye", "ha", "ho", "he", "hee", "oh", "ah",
        "yes", "hush", "tut", "hear", "come", "go", "knock", "night", "boo", "blah", "yada", "pom", "tick", "choo", "aye", "nay", "never",
        "please", "wait", "again", "very", "really", "far", "long", "many", "much", "more", "big", "old", "poor", "dear", "little",
        "ever", "on", "out", "up", "down", "over", "round", "in", "la", "na", "da", "ta", "bla", "woof", "quack", "chop", "hip",
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

            // Describing words are repeated for emphasis ("a cold cold night"), and names can be doubled
            // ("Walla Walla").
            WordType type = sentence.Type(i + 1);
            if (type is WordType.Adjective or WordType.Adverb or WordType.Other || (sentence.IsCapitalized(i + 1) && word.Length > 1))
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
