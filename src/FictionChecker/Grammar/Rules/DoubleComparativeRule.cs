namespace FictionChecker.Grammar.Rules;

/// <summary>"more stronger", "most quickest": the comparison made twice.</summary>
internal sealed class DoubleComparativeRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.DoubleComparative,
        "Double comparison",
        "Flags \"more\" before a word that already means more (\"more stronger\") and \"most\" before one that already means most (\"most quickest\"). It is treated as formal: inside dialogue it is only flagged when dialogue checking is strict, because \"more better\" is how some characters talk.",
        RuleCategory.Mechanics,
        true,
        true,
        ["This rope is more stronger than the other.", "It was the most quickest way to the harbour.", "She felt more better after the rest."],
        ["This rope is stronger than the other.", "More older men came than young ones.", "There will be more later.", "“It's more better this way,” he said.", "He wanted more, better food.", "Most best men give a speech."]);

    private static readonly HashSet<string> Irregular = new(StringComparer.Ordinal) { "better", "worse" };
    private static readonly HashSet<string> IrregularMost = new(StringComparer.Ordinal) { "best", "worst" };
    private static readonly HashSet<string> NotComparing = new(StringComparer.Ordinal) { "later", "inner", "upper", "outer", "former", "latter", "elder", "lesser", "utter", "other" };

    public override RuleInfo Info => Description;

    public override void Check(Sentence sentence, List<Finding> findings)
    {
        for (int i = 0; i + 1 < sentence.Count; i++)
        {
            bool more = sentence.Is(i, "more");
            if (!more && !sentence.Is(i, "most"))
            {
                continue;
            }

            int w = i + 1;
            if (!sentence.IsWord(w) || !sentence.TouchesNext(i) || sentence.IsCapitalized(w))
            {
                continue;
            }

            string word = sentence.Lower(w);
            bool doubled = more
                ? ((sentence.Forms(w) & WordForms.Comparative) != 0 && word.EndsWith("er", StringComparison.Ordinal)) || Irregular.Contains(word)
                : ((sentence.Forms(w) & WordForms.Superlative) != 0 && word.EndsWith("est", StringComparison.Ordinal)) || IrregularMost.Contains(word);
            if (!doubled || NotComparing.Contains(word))
            {
                continue;
            }

            // "more older men", "most best men": "more" counts the men. Leave it when several things follow.
            if (sentence.IsWord(w + 1) && sentence.Type(w + 1) == WordType.Noun && ((sentence.Forms(w + 1) & WordForms.NounPlural) != 0 || sentence.Is(w + 1, "men", "women", "people", "children")))
            {
                continue;
            }

            var first = sentence.Token(i);
            var second = sentence.Token(w);
            string fix = sentence.Written(w).ToString();
            if (sentence.IsCapitalized(i))
            {
                fix = char.ToUpperInvariant(fix[0]) + fix[1..];
            }

            findings.Add(new Finding(
                first.Start,
                second.End - first.Start,
                $"{CharClassQuote.Open}{sentence.Written(w)}{CharClassQuote.Close} already means {CharClassQuote.Open}{(more ? "more" : "most")}{CharClassQuote.Close}.",
                "A word ending in -er or -est already makes the comparison, so \"more\" or \"most\" before it says it twice.",
                [Replace(first.Start, second.End - first.Start, fix)]));
        }
    }
}
