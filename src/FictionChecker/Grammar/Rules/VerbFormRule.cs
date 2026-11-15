namespace FictionChecker.Grammar.Rules;

/// <summary>
/// The wrong form of a verb after a helping verb: "have went" for "have gone", "did not went" for "did not
/// go", "should has" for "should have". Only verbs whose forms differ and cannot be mistaken for another
/// word are covered, so "had rose bushes" and "the saw" are never touched.
/// </summary>
internal sealed class VerbFormRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.VerbForm,
        "Verb form",
        "Flags the wrong form of a verb after \"have\", \"did\" or a word like \"should\": \"have went\", \"did not went\", \"should has known\", \"wanted to went\". It is treated as formal: inside dialogue it is only flagged when dialogue checking is strict, because forms like \"I seen\" and \"had took\" are how some characters talk.",
        RuleCategory.Agreement,
        true,
        true,
        ["I have went there many times.", "She has ate nothing since morning.", "He did not went to the meeting.", "They should has known better.", "She wanted to went home.", "He didn't walked far.", "We had already took the road north."],
        ["I have gone there many times.", "He did not go to the meeting.", "The little she had went to her son.", "He had rose bushes by the gate.", "The man she spoke to gave a nod.", "“I have went there many times,” he said.", "He didn't used to mind.", "Those he had known came to see him.", "May came too soon that year."]);

    // Past forms that are never also the base form, a noun or an adjective: past, base, form after "have".
    private static readonly Dictionary<string, (string Base, string Participle)> PastOnly = Table(
        "arose:arise:arisen ate:eat:eaten awoke:awake:awoken became:become:become began:begin:begun blew:blow:blown came:come:come chose:choose:chosen did:do:done drank:drink:drunk drew:draw:drawn flew:fly:flown forgave:forgive:forgiven froze:freeze:frozen gave:give:given grew:grow:grown knew:know:known mistook:mistake:mistaken overcame:overcome:overcome overtook:overtake:overtaken ran:run:run rode:ride:ridden shook:shake:shaken strode:stride:stridden swam:swim:swum threw:throw:thrown took:take:taken undid:undo:undone went:go:gone withdrew:withdraw:withdrawn wrote:write:written");

    // Forms used only after "have" ("gone"), which cannot follow "did" or "to".
    private static readonly Dictionary<string, (string Base, string Participle)> ParticipleOnly = Table(
        "begun:begin:begun done:do:done eaten:eat:eaten fallen:fall:fallen forgotten:forget:forgotten given:give:given gone:go:gone known:know:known seen:see:seen spoken:speak:spoken taken:take:taken written:write:written");

    private static readonly HashSet<string> Have = new(StringComparer.Ordinal) { "have", "has", "had", "having", "haven't", "hasn't", "hadn't", "i've", "we've", "you've", "they've", "could've", "should've", "would've", "must've", "might've" };
    private static readonly HashSet<string> Did = new(StringComparer.Ordinal) { "did", "does", "do" };
    private static readonly HashSet<string> DidNot = new(StringComparer.Ordinal) { "didn't", "doesn't", "don't" };
    private static readonly HashSet<string> Modal = new(StringComparer.Ordinal) { "will", "would", "can", "could", "should", "must", "might", "may", "shall", "won't", "wouldn't", "can't", "cannot", "couldn't", "shouldn't", "mustn't" };
    private static readonly HashSet<string> Between = new(StringComparer.Ordinal) { "not", "never", "always", "already", "just", "ever", "also", "only", "often", "nearly", "almost", "really", "certainly", "probably" };
    private static readonly HashSet<string> TakesTo = new(StringComparer.Ordinal) { "want", "wants", "wanted", "wanting", "going", "need", "needs", "needed", "try", "tries", "tried", "trying", "have", "has", "had", "like", "liked", "decided", "able", "hoped", "wished", "ought", "supposed", "began", "started" };
    private static readonly HashSet<string> OwnsBefore = new(StringComparer.Ordinal) { "what", "whatever", "all", "everything", "anything", "that", "which", "little", "much", "those", "who", "whom" };

    public override RuleInfo Info => Description;

    public override void Check(Sentence sentence, List<Finding> findings)
    {
        for (int i = 0; i + 1 < sentence.Count; i++)
        {
            if (!sentence.IsWord(i))
            {
                continue;
            }

            string helper = sentence.Lower(i);
            bool have = Have.Contains(helper);
            bool did = Did.Contains(helper);
            bool didNot = DidNot.Contains(helper);
            // A capital marks a name or a month: "May came too soon", "the second Will has been read".
            bool modal = Modal.Contains(helper) && !sentence.IsCapitalized(i);
            bool to = helper == "to" && i > 0 && sentence.IsWord(i - 1) && TakesTo.Contains(sentence.Lower(i - 1));
            if (!(have || did || didNot || modal || to))
            {
                continue;
            }

            // Skip over "not", "never", "already" and the like; the verb must follow directly after.
            int v = i + 1;
            bool negated = didNot;
            while (v < sentence.Count && v <= i + 2 && sentence.IsWord(v) && Between.Contains(sentence.Lower(v)) && !to)
            {
                negated |= sentence.Is(v, "not", "never");
                v++;
            }

            if (!sentence.IsWord(v) || sentence.IsCapitalized(v))
            {
                continue;
            }

            string verb = sentence.Lower(v);
            string? fix = null;
            if (have)
            {
                // "the little she had went to her son": "had" there means "owned", and the verb is the sentence's own.
                if (PastOnly.TryGetValue(verb, out (string Base, string Participle) forms) && forms.Participle != verb && !OwnedBefore(sentence, i))
                {
                    fix = forms.Participle;
                }
            }
            else if (did || didNot)
            {
                // "did went" on its own could be "all he did went wrong", so plain "did" needs "not".
                if (negated)
                {
                    if (PastOnly.TryGetValue(verb, out (string Base, string Participle) past) || ParticipleOnly.TryGetValue(verb, out past))
                    {
                        fix = past.Base;
                    }
                    else if (verb != "used" && sentence.Type(v) == WordType.Verb && (sentence.Forms(v) & WordForms.VerbPast) != 0 && (sentence.Forms(v) & WordForms.VerbBase) == 0
                        && verb.EndsWith("ed", StringComparison.Ordinal) && RegularBase(sentence, v) is { } regular)
                    {
                        fix = regular;
                    }
                }
            }
            else if (verb == "has")
            {
                fix = "have";
            }
            else if (PastOnly.TryGetValue(verb, out (string Base, string Participle) plain) || ParticipleOnly.TryGetValue(verb, out plain))
            {
                // "could went", "to went". ("Could have gone" is another slip, but the nearest fix is the base.)
                fix = plain.Base;
            }

            if (fix is null || fix == verb)
            {
                continue;
            }

            var token = sentence.Token(v);
            string helperWritten = sentence.Written(i).ToString();
            findings.Add(new Finding(
                token.Start,
                token.Length,
                $"After {CharClassQuote.Open}{helperWritten}{CharClassQuote.Close} the verb should be {CharClassQuote.Open}{fix}{CharClassQuote.Close}, not {CharClassQuote.Open}{sentence.Written(v)}{CharClassQuote.Close}.",
                have ? "After \"have\", \"has\" or \"had\" a verb takes its past participle: \"have gone\", not \"have went\"." : "After \"did\", \"to\" and words like \"should\" a verb takes its plain form: \"did not go\", \"should have\".",
                [Replace(token.Start, token.Length, sentence.IsAllCapitals(v) ? fix.ToUpperInvariant() : fix)]));
        }
    }

    // Whether "had" follows something it owns: "what she had", "all they had", "the little he had".
    private static bool OwnedBefore(Sentence sentence, int i)
    {
        for (int j = i - 1; j >= 0 && j >= i - 4; j--)
        {
            if (!sentence.IsWord(j))
            {
                return false;
            }

            if (OwnsBefore.Contains(sentence.Lower(j)) || sentence.Type(j) == WordType.Noun)
            {
                return j < i - 1;
            }
        }

        return false;
    }

    // The base of a regular past form ("walked" gives "walk"), when the word data links them.
    private static string? RegularBase(Sentence sentence, int v)
    {
        int word = sentence.WordIndex(v);
        if (word < 0)
        {
            return null;
        }

        foreach ((int baseWord, WordForms forms) in sentence.Data.BasesOf(word))
        {
            if ((forms & WordForms.VerbPast) != 0)
            {
                return sentence.Data.Lexicon.WordAt(baseWord);
            }
        }

        return null;
    }

    private static Dictionary<string, (string Base, string Participle)> Table(string entries)
    {
        var table = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        foreach (string entry in entries.Split(' '))
        {
            string[] parts = entry.Split(':');
            table[parts[0]] = (parts[1], parts[2]);
        }

        return table;
    }
}
