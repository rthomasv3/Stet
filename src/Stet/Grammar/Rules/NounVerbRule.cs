using Stet.Segmentation;

namespace Stet.Grammar.Rules;

/// <summary>
/// A plural subject with a singular verb, in the two shapes that can be told apart safely: a plain plural
/// noun phrase opening a clause ("The children was playing") and "there is" before several things
/// ("There is many reasons"). Anything with a word like "of" in it ("the box of letters was") is left
/// alone, because the noun next to the verb is then often not the subject.
/// </summary>
internal sealed class NounVerbRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.NounVerb,
        "Plural noun with a singular verb",
        "Flags \"was\", \"is\", \"has\" or \"does\" straight after a plain plural subject that opens a clause (\"The children was playing\"), and \"there is\" or \"there was\" before \"many\", \"several\" or a number of things. It is treated as formal: inside dialogue it is only flagged when dialogue checking is strict.",
        RuleCategory.Agreement,
        true,
        true,
        ["The children was playing in the yard.", "The old dogs is asleep by the fire.", "There is many reasons to stay.", "There was several men at the gate.", "But the soldiers has gone.", "He ran, but the gates was shut."],
        ["The children were playing in the yard.", "The box of letters was on the table.", "The news was bad.", "Ten miles is a long way.", "The Two Towers was his favourite.", "There is many a slip.", "“The dogs is hungry,” he said.", "What mattered to the men was food.", "The cry of the gulls was loud.", "Nothing of me but my eyes was visible."]);

    private static readonly Dictionary<string, string> Fixes = new(StringComparer.Ordinal)
    {
        ["was"] = "were", ["is"] = "are", ["has"] = "have", ["does"] = "do",
        ["wasn't"] = "weren't", ["isn't"] = "aren't", ["hasn't"] = "haven't", ["doesn't"] = "don't",
    };

    private static readonly HashSet<string> Openers = new(StringComparer.Ordinal) { "the", "these", "those", "my", "his", "her", "its", "our", "their", "your", "both" };
    private static readonly HashSet<string> ClauseStarts = new(StringComparer.Ordinal) { "but", "because", "when", "while", "if", "although", "though", "then", "so" };
    private static readonly HashSet<string> IrregularPlurals = new(StringComparer.Ordinal) { "children", "men", "women", "people", "feet", "teeth", "mice", "geese" };

    // Words that end in -s, or look plural, but take a singular verb.
    private static readonly HashSet<string> SingularInS = new(StringComparer.Ordinal)
    {
        "news", "series", "species", "means", "physics", "politics", "mathematics", "economics", "ethics", "measles", "headquarters", "barracks", "gallows", "works", "crossroads", "whereabouts",
        "remains", "thanks", "odds", "pains", "wages", "times", "others", "years", "days", "hours", "minutes", "weeks", "months", "miles", "pounds", "dollars", "yards", "acres",
    };

    private static readonly HashSet<string> Several = new(StringComparer.Ordinal) { "many", "several", "numerous", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "twenty", "hundreds", "thousands", "dozens" };

    public override RuleInfo Info => Description;

    public override void Check(Sentence sentence, List<Finding> findings)
    {
        for (int v = 1; v < sentence.Count; v++)
        {
            if (!sentence.IsWord(v) || !Fixes.TryGetValue(sentence.Lower(v), out string? fix))
            {
                continue;
            }

            bool found = ThereIs(sentence, v) || PluralSubject(sentence, v);
            if (!found)
            {
                continue;
            }

            var token = sentence.Token(v);
            string written = sentence.Written(v).ToString();
            string replacement = sentence.IsAllCapitals(v) ? fix.ToUpperInvariant() : fix;
            if (written.Contains(Segmentation.CharClass.RightSingleQuote))
            {
                replacement = replacement.Replace('\'', Segmentation.CharClass.RightSingleQuote);
            }

            findings.Add(new Finding(
                token.Start,
                token.Length,
                $"{CharClassQuote.Open}{written}{CharClassQuote.Close} should be {CharClassQuote.Open}{replacement}{CharClassQuote.Close}: it is about more than one.",
                "A plural subject takes a plural verb: \"the children were\", \"there are many\".",
                [Replace(token.Start, token.Length, replacement)]));
        }
    }

    // "There is many reasons": "there", the verb, a word meaning several, then (after at most one more word) a plural noun.
    private static bool ThereIs(Sentence sentence, int v)
    {
        if (!sentence.Is(v, "is", "was") || !sentence.Is(v - 1, "there") || !sentence.IsWord(v + 1) || !Several.Contains(sentence.Lower(v + 1)) || sentence.Is(v + 2, "a", "an"))
        {
            return false;
        }

        for (int n = v + 2; n <= v + 3 && n < sentence.Count; n++)
        {
            if (!sentence.IsWord(n))
            {
                return false;
            }

            if (IsPlural(sentence, n))
            {
                return true;
            }
        }

        return false;
    }

    // "The old dogs is": an opener ("the", "these", "my"), at most two describing words, a plural noun, the verb;
    // and the opener starts the sentence or follows a word like "but" or "when".
    private static bool PluralSubject(Sentence sentence, int v)
    {
        int noun = v - 1;
        if (!IsPlural(sentence, noun) || sentence.IsCapitalized(noun))
        {
            return false;
        }

        int opener = noun - 1;
        while (opener >= 0 && opener >= noun - 2 && sentence.IsWord(opener) && !Openers.Contains(sentence.Lower(opener))
            && sentence.Type(opener) is WordType.Adjective or WordType.Noun && !sentence.Lower(opener).Contains('\'') && !sentence.IsCapitalized(opener))
        {
            opener--;
        }

        if (opener < 0 || !sentence.IsWord(opener) || !Openers.Contains(sentence.Lower(opener)))
        {
            return false;
        }

        // Before the opener: nothing but opening punctuation, or a word that starts a clause.
        int before = opener - 1;
        while (before >= 0 && !sentence.IsWord(before) && sentence.Token(before).Kind == Segmentation.TokenKind.Punctuation && sentence.Text[sentence.Token(before).Start] is not (',' or ';' or ':'))
        {
            before--;
        }

        if (before < 0)
        {
            return true;
        }

        // "but", "when": only where it opens the sentence or follows a comma. In "nothing but my eyes was
        // visible" the subject started earlier.
        return sentence.IsWord(before) && ClauseStarts.Contains(sentence.Lower(before))
            && (before == 0 || (!sentence.IsWord(before - 1) && sentence.Text[sentence.Token(before - 1).Start] is ',' or ';' or CharClass.LeftDoubleQuote or '"'));
    }

    private static bool IsPlural(Sentence sentence, int i)
    {
        if (!sentence.IsWord(i))
        {
            return false;
        }

        string lower = sentence.Lower(i);
        if (IrregularPlurals.Contains(lower))
        {
            return true;
        }

        return lower.EndsWith('s') && !lower.EndsWith("ss", StringComparison.Ordinal) && !SingularInS.Contains(lower)
            && sentence.Type(i) == WordType.Noun && (sentence.Forms(i) & WordForms.NounPlural) != 0;
    }
}
