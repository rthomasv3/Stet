namespace FictionChecker.Grammar.Rules;

/// <summary>A subject pronoun with a verb form that does not agree with it: "they was", "he don't".</summary>
internal sealed class PronounVerbRule : GrammarRule
{
    // For each group of pronouns, the verb forms that are wrong after it and the form that is meant.
    private static readonly Dictionary<string, string> AfterI = new(StringComparer.Ordinal)
    {
        ["is"] = "am", ["are"] = "am", ["has"] = "have", ["does"] = "do", ["doesn't"] = "don't", ["hasn't"] = "haven't",
        ["isn't"] = "am not", ["were"] = "was", ["weren't"] = "wasn't",
    };

    private static readonly Dictionary<string, string> AfterHeSheIt = new(StringComparer.Ordinal)
    {
        ["are"] = "is", ["am"] = "is", ["have"] = "has", ["do"] = "does", ["don't"] = "doesn't", ["aren't"] = "isn't",
        ["haven't"] = "hasn't", ["were"] = "was", ["weren't"] = "wasn't",
    };

    private static readonly Dictionary<string, string> AfterWeYouThey = new(StringComparer.Ordinal)
    {
        ["is"] = "are", ["am"] = "are", ["was"] = "were", ["has"] = "have", ["does"] = "do", ["doesn't"] = "don't",
        ["isn't"] = "aren't", ["wasn't"] = "weren't", ["hasn't"] = "haven't",
    };

    // Before the pronoun, these make it something other than the subject of the verb: "one of you is",
    // "he and I are", "taller than they are", "nobody but she was".
    private static readonly HashSet<string> NotSubjectAfter = new(StringComparer.Ordinal) { "and", "or", "nor", "than", "but", "except", "like", "as" };

    // Words after which "he were" is the subjunctive and right: "if he were", "I wish she were".
    private static readonly HashSet<string> SubjunctiveLeadIns = new(StringComparer.Ordinal)
    {
        "if", "though", "although", "wish", "wished", "wishes", "wishing", "as", "suppose", "supposing", "unless", "whether", "lest",
        "would", "rather", "imagine", "pretend", "what", "only",
    };

    private static readonly RuleInfo Description = new(
        RuleIds.PronounVerb,
        "Pronoun and verb agreement",
        "Flags a subject pronoun followed by a form of \"be\", \"have\" or \"do\" that does not agree with it, such as \"they was\", \"I has\" or \"he don't\". \"If I were\" and similar are left alone. It is treated as formal: inside dialogue it is only flagged when dialogue checking is strict, because speech in dialect breaks it on purpose.",
        RuleCategory.Agreement,
        true,
        true,
        ["They was late again.", "I has no idea.", "He don't know the way.", "We was only talking.", "She have a brother in the north."],
        ["They were late again.", "If I were you, I would go.", "She wished he were here.", "One of you is lying.", "He and I are old friends.", "The men who found it were gone.", "“They was here,” he said."]);

    public override RuleInfo Info => Description;

    public override void Check(Sentence sentence, List<Finding> findings)
    {
        for (int i = 0; i + 1 < sentence.Count; i++)
        {
            if (!sentence.IsWord(i) || !sentence.IsWord(i + 1) || !sentence.TouchesNext(i))
            {
                continue;
            }

            string pronoun = sentence.Lower(i);
            Dictionary<string, string>? wrong = pronoun switch
            {
                "i" => sentence.Written(i) is "I" ? AfterI : null,
                "he" or "she" or "it" => AfterHeSheIt,
                "we" or "you" or "they" => AfterWeYouThey,
                _ => null,
            };
            if (wrong is null || !wrong.TryGetValue(sentence.Lower(i + 1), out string? right) || !IsSubject(sentence, i, pronoun))
            {
                continue;
            }

            string verb = sentence.Lower(i + 1);
            if (verb is "were" or "weren't" && HasSubjunctiveLeadIn(sentence, i))
            {
                continue;
            }

            // "it were" is also an old way of saying "it would be" ("it were best to wait").
            if (pronoun == "it" && verb is "were" or "weren't")
            {
                continue;
            }

            var token = sentence.Token(i + 1);
            string written = sentence.Written(i + 1).ToString();
            string fix = written.Contains(Segmentation.CharClass.RightSingleQuote) ? right.Replace('\'', Segmentation.CharClass.RightSingleQuote) : right;
            if (sentence.IsAllCapitals(i + 1))
            {
                fix = fix.ToUpperInvariant();
            }

            findings.Add(new Finding(
                token.Start,
                token.Length,
                $"{CharClassQuote.Open}{sentence.Written(i)} {written}{CharClassQuote.Close} should be {CharClassQuote.Open}{sentence.Written(i)} {fix}{CharClassQuote.Close}.",
                "The verb does not agree with its subject.",
                [Replace(token.Start, token.Length, fix)]));
        }
    }

    private static bool IsSubject(Sentence sentence, int i, string pronoun)
    {
        if (i == 0 || !sentence.IsWord(i - 1))
        {
            // First in the sentence or after punctuation. After a closing quote mark the pronoun may belong
            // to a tag, but then its verb is "said", not one of the forms checked.
            return true;
        }

        string before = sentence.Lower(i - 1);
        WordTraits traits = sentence.Traits(i - 1);
        if (NotSubjectAfter.Contains(before) || ((traits & WordTraits.Preposition) != 0 && (traits & (WordTraits.Subordinator | WordTraits.Conjunction)) == 0))
        {
            return false;
        }

        if (pronoun is not ("you" or "it"))
        {
            return true;
        }

        // "you" and "it" are also objects ("the men who found it were gone"), so they only count as the
        // subject where a clause clearly starts.
        return (traits & (WordTraits.Conjunction | WordTraits.Subordinator | WordTraits.Question)) != 0 || before is "then" or "so" or "now" or "that" or "here" or "there";
    }

    private static bool HasSubjunctiveLeadIn(Sentence sentence, int i)
    {
        for (int k = 0; k < i; k++)
        {
            if (sentence.IsWord(k) && SubjunctiveLeadIns.Contains(sentence.Lower(k)))
            {
                return true;
            }
        }

        return false;
    }
}
