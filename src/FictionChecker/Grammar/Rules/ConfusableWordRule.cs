namespace FictionChecker.Grammar.Rules;

/// <summary>
/// Flags a word that is often confused with another ("their" for "there") when the words on either side
/// say, strongly, that the other was meant. One rule per family of words, so each can be switched off alone.
/// </summary>
internal sealed class ConfusableWordRule : GrammarRule
{
    private readonly ConfusableFamily _family;
    private readonly Func<ConfusableData> _data;
    private readonly HashSet<string> _words = new(StringComparer.Ordinal);
    private readonly RuleInfo _info;

    public ConfusableWordRule(ConfusableFamily family, Func<ConfusableData> data)
    {
        _family = family;
        _data = data;
        foreach (string[] group in family.Groups)
        {
            _words.UnionWith(group);
        }

        _info = new RuleInfo(
            family.RuleId,
            family.Name,
            $"Flags one of these words where the words around it show that another was meant. {family.Explanation}",
            RuleCategory.ConfusableWords,
            ConfusableSets.OnByDefault(family.RuleId),
            false,
            family.Flagged,
            family.Accepted);
    }

    public override RuleInfo Info => _info;

    public override void Check(Sentence sentence, List<Finding> findings)
    {
        bool leading = true;
        for (int i = 0; i < sentence.Count; i++)
        {
            if (!sentence.IsWord(i))
            {
                continue;
            }

            bool first = leading;
            leading = false;
            if (!_words.Contains(sentence.Lower(i)))
            {
                continue;
            }

            // A capital in the middle of a sentence marks a name or a title, which follows its own rules.
            // So does a capital followed by another capital, even at the start ("Than Kosis looked up").
            if (sentence.IsCapitalized(i) && (!first || (sentence.IsWord(i + 1) && sentence.IsCapitalized(i + 1) && !sentence.Is(i + 1, "i"))))
            {
                continue;
            }

            ConfusableData data = _data();
            if (!ConfusableScorer.TryScore(sentence, i, data, out ConfusableEvidence evidence))
            {
                continue;
            }

            string[] members = data.Members(evidence.Group);
            if (!ConfusableSets.IsStrongEnough(members[evidence.Written], members[evidence.Best], evidence))
            {
                continue;
            }

            var token = sentence.Token(i);
            string fix = MatchWriting(sentence, i, members[evidence.Best]);
            findings.Add(new Finding(
                token.Start,
                token.Length,
                $"{CharClassQuote.Open}{sentence.Written(i)}{CharClassQuote.Close} looks like it should be {CharClassQuote.Open}{fix}{CharClassQuote.Close}.",
                _family.Explanation,
                [Replace(token.Start, token.Length, fix)]));
        }
    }

    // The replacement with the capitals and the kind of apostrophe the writer is using.
    private static string MatchWriting(Sentence sentence, int i, string word)
    {
        ReadOnlySpan<char> written = sentence.Written(i);
        if (sentence.IsAllCapitals(i) && written.Length > 1)
        {
            word = word.ToUpperInvariant();
        }
        else if (sentence.IsCapitalized(i))
        {
            word = char.ToUpperInvariant(word[0]) + word[1..];
        }

        if (word.Contains('\'') && !written.Contains('\'') && (written.Contains(Segmentation.CharClass.RightSingleQuote) || sentence.Text.Contains(Segmentation.CharClass.RightSingleQuote)))
        {
            word = word.Replace('\'', Segmentation.CharClass.RightSingleQuote);
        }

        return word;
    }
}
