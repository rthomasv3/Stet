namespace FictionChecker.Grammar.Rules;

/// <summary>
/// A subject pronoun with a form of "be", "have" or "do" that does not agree with it. Two rules share this
/// check: one for mismatches that are slips wherever they appear ("I has", "they is"), and one for those
/// that are ordinary in dialect speech ("they was", "he don't", "he were"), which is formal and so is
/// skipped inside dialogue unless dialogue checking is strict.
/// </summary>
internal abstract class PronounVerbRuleBase : GrammarRule
{
    // Before the pronoun, these make it something other than the subject of the verb: "one of you is",
    // "he and I are", "taller than they are", "nobody but she was".
    private static readonly HashSet<string> NotSubjectAfter = new(StringComparer.Ordinal) { "and", "or", "nor", "than", "but", "except", "like", "as" };

    // Words after which "he were" is the subjunctive and right: "if he were", "I wish she were".
    private static readonly HashSet<string> SubjunctiveLeadIns = new(StringComparer.Ordinal)
    {
        "if", "though", "although", "wish", "wished", "wishes", "wishing", "as", "suppose", "supposing", "unless", "whether", "lest",
        "would", "rather", "imagine", "pretend", "what", "only",
    };

    private static readonly HashSet<string> AskingLeadIns = new(StringComparer.Ordinal)
    {
        "suggest", "suggested", "suggests", "suggesting", "insist", "insisted", "insists", "demand", "demanded", "demands", "recommend", "recommended",
        "request", "requested", "ask", "asked", "require", "required", "propose", "proposed", "urge", "urged", "order", "ordered", "prefer", "preferred",
        "lest", "ere", "until", "till", "whether", "if", "unless", "though", "that",
    };

    /// <summary>The wrong verb forms after "I", with the form that is meant.</summary>
    protected abstract Dictionary<string, string> AfterI { get; }

    /// <summary>The wrong verb forms after "he", "she" and "it".</summary>
    protected abstract Dictionary<string, string> AfterHeSheIt { get; }

    /// <summary>The wrong verb forms after "we", "you" and "they".</summary>
    protected abstract Dictionary<string, string> AfterWeYouThey { get; }

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
            if (verb is "were" or "weren't" && (HasLeadIn(sentence, i, SubjunctiveLeadIns) || IsOldConditional(sentence, i, pronoun)))
            {
                continue;
            }

            // "She suggested he do so", "they insisted she have a guard": after asking or ordering, the bare
            // verb is right.
            if (verb is "do" or "have" && pronoun is "he" or "she" or "it" && HasLeadIn(sentence, i, AskingLeadIns))
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

        // "What did she have?", "Could he do it?", "Never would they have guessed": after a helping verb the
        // pronoun's own verb is the bare form, which is right.
        string before = sentence.Lower(i - 1);
        WordTraits traits = sentence.Traits(i - 1);
        if ((traits & (WordTraits.Modal | WordTraits.Do)) != 0 || before.EndsWith("n't", StringComparison.Ordinal) || before.EndsWith("'d", StringComparison.Ordinal)
            || before.EndsWith("'ll", StringComparison.Ordinal) || before is "let" or "lets" or "make" or "made" or "had" or "not")
        {
            return false;
        }

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

    private static bool HasLeadIn(Sentence sentence, int i, HashSet<string> leadIns)
    {
        for (int k = 0; k < i; k++)
        {
            if (!sentence.IsWord(k))
            {
                continue;
            }

            // "half-wished" counts as "wished".
            string word = sentence.Lower(k);
            int hyphen = word.LastIndexOf('-');
            if (leadIns.Contains(hyphen < 0 ? word : word[(hyphen + 1)..]))
            {
                return true;
            }
        }

        return false;
    }

    // "It were best to wait", "it were a pity": an old way of saying "it would be".
    private static bool IsOldConditional(Sentence sentence, int i, string pronoun) =>
        pronoun == "it" && sentence.IsWord(i + 2)
        && (sentence.Type(i + 2) is WordType.Adjective or WordType.Adverb or WordType.Determiner || sentence.Is(i + 2, "better", "best", "well", "not", "to", "as"));
}

/// <summary>Mismatches that are slips wherever they appear: "I has", "they is", "she have".</summary>
internal sealed class PronounVerbRule : PronounVerbRuleBase
{
    private static readonly RuleInfo Description = new(
        RuleIds.PronounVerb,
        "Pronoun and verb agreement",
        "Flags a subject pronoun followed by a form of \"be\", \"have\" or \"do\" that does not agree with it, such as \"I has\", \"they is\" or \"she have\". Forms that are common in dialect speech, such as \"they was\" and \"he don't\", belong to a separate rule.",
        RuleCategory.Agreement,
        true,
        false,
        ["I has no idea.", "They is late again.", "She have a brother in the north.", "“We has to go,” he said."],
        ["They are late again.", "One of you is lying.", "He and I are old friends.", "What did she have to lose?", "Could he do it?", "They was late again."]);

    public override RuleInfo Info => Description;

    protected override Dictionary<string, string> AfterI { get; } = new(StringComparer.Ordinal)
    {
        ["is"] = "am", ["are"] = "am", ["has"] = "have", ["does"] = "do", ["doesn't"] = "don't", ["hasn't"] = "haven't", ["isn't"] = "am not",
    };

    protected override Dictionary<string, string> AfterHeSheIt { get; } = new(StringComparer.Ordinal)
    {
        ["are"] = "is", ["am"] = "is", ["have"] = "has", ["aren't"] = "isn't", ["haven't"] = "hasn't",
    };

    protected override Dictionary<string, string> AfterWeYouThey { get; } = new(StringComparer.Ordinal)
    {
        ["is"] = "are", ["am"] = "are", ["has"] = "have", ["does"] = "do", ["doesn't"] = "don't", ["isn't"] = "aren't", ["hasn't"] = "haven't",
    };
}

/// <summary>Mismatches that are ordinary in dialect speech: "they was", "he don't", "he were".</summary>
internal sealed class DialectVerbFormRule : PronounVerbRuleBase
{
    private static readonly RuleInfo Description = new(
        RuleIds.DialectVerbForm,
        "Dialect verb forms",
        "Flags \"they was\", \"we was\", \"you was\", \"he don't\", \"she do\" and \"he were\": mismatches that are slips in standard writing but ordinary in dialect speech. \"If I were\" and similar are left alone. It is treated as formal: inside dialogue it is only flagged when dialogue checking is strict.",
        RuleCategory.Agreement,
        true,
        true,
        ["They was late again.", "He don't know the way.", "We was only talking.", "She were tired of waiting."],
        ["They were late again.", "If I were you, I would go.", "She wished he were here.", "The men who found it were gone.", "It were best to wait.", "“They was here,” he said."]);

    public override RuleInfo Info => Description;

    protected override Dictionary<string, string> AfterI { get; } = new(StringComparer.Ordinal) { ["were"] = "was", ["weren't"] = "wasn't" };

    protected override Dictionary<string, string> AfterHeSheIt { get; } = new(StringComparer.Ordinal)
    {
        ["do"] = "does", ["don't"] = "doesn't", ["were"] = "was", ["weren't"] = "wasn't",
    };

    protected override Dictionary<string, string> AfterWeYouThey { get; } = new(StringComparer.Ordinal) { ["was"] = "were", ["wasn't"] = "weren't" };
}
