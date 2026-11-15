using FictionChecker.Segmentation;

namespace FictionChecker.Grammar.Rules;

/// <summary><c>“Hello.” she said.</c> where a comma is needed: <c>“Hello,” she said.</c></summary>
internal sealed class FullStopBeforeTagRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.FullStopBeforeTag,
        "Full stop before a dialogue tag",
        "Flags speech that ends with a full stop and is followed by its tag, such as \"she said\". The speech and its tag are one sentence, so the speech ends with a comma. Speech followed by an action is left alone.",
        RuleCategory.DialoguePunctuation,
        true,
        false,
        ["“Hello.” she said.", "“Come in.” said the old man.", "“Not yet.” Tom said."],
        ["“Hello,” she said.", "“Hello.” She smiled.", "“Hello.” Tom said nothing more.", "“Well...” she said.", "“Stop!” he cried.", "“I should keep them.” She sighed. “I’d feel better.”"]);

    public override RuleInfo Info => Description;

    public override void CheckParagraph(Paragraph paragraph, List<Finding> findings)
    {
        string text = paragraph.Text;
        foreach (TaggedSpeech speech in DialogueTags.Find(paragraph))
        {
            int stop = speech.Quote - 1;
            if (speech.CommaAfterQuote || text[stop] != '.' || (stop > 0 && text[stop - 1] is '.' or ' '))
            {
                continue; // not a single full stop: an ellipsis trails off and is fine
            }

            // A lowercase pronoun after the full stop is wrong either way, and with a speech verb the comma is
            // the likelier fix. Otherwise the verb must be one that only reports speech: "She sighed." is a
            // sentence of its own. And "Tom said" could open a new sentence ("Tom said nothing more."), so a
            // tag that starts with a capital only counts when it ends straight after its verb.
            bool lowerPronoun = speech.StartsLower && speech.PronounFirst;
            if (!lowerPronoun && (!speech.PlainSpeechVerb || !speech.StartsLower && (!speech.EndsThere || speech.First == speech.Verb)))
            {
                continue;
            }

            findings.Add(new Finding(
                stop,
                1,
                "Speech followed by its tag ends with a comma, not a full stop.",
                "The speech and \"she said\" are one sentence. If the next words are a new sentence, start them with a capital.",
                [Replace(stop, 1, ",")]));
        }
    }
}

/// <summary><c>“Hello,” She said.</c> where the tag carries the sentence on: <c>“Hello,” she said.</c></summary>
internal sealed class CapitalizedTagRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.CapitalizedTag,
        "Capitalized dialogue tag",
        "Flags a dialogue tag that starts with a capital letter after speech ending in a comma, as in “Hello,” She said. The tag carries the sentence on, so it starts in lowercase. Names keep their capitals.",
        RuleCategory.DialoguePunctuation,
        true,
        false,
        ["“Hello,” She said.", "“Come in,” Said the old man."],
        ["“Hello,” she said.", "“Hello,” Tom said.", "“Hello,” I said.", "“Hello.” She said nothing more."]);

    private static readonly HashSet<string> Pronouns = new(StringComparer.Ordinal) { "he", "she", "they", "we", "it", "you" };

    public override RuleInfo Info => Description;

    public override void CheckParagraph(Paragraph paragraph, List<Finding> findings)
    {
        string text = paragraph.Text;
        foreach (TaggedSpeech speech in DialogueTags.Find(paragraph))
        {
            Token first = paragraph.Tokens[speech.First];
            if (speech.CommaAfterQuote || text[speech.Quote - 1] != ',' || !char.IsUpper(text[first.Start]))
            {
                continue;
            }

            // Only a pronoun or the verb itself can be wrongly capitalized; "Tom said" is right.
            string word = paragraph.Lower(speech.First);
            bool wrong = (speech.First != speech.Verb && Pronouns.Contains(word)) || IsCapitalizedVerb(paragraph, speech.First);
            if (!wrong || first.Length < 2 || !char.IsLower(text[first.Start + 1]))
            {
                continue;
            }

            string written = first.TextIn(text).ToString();
            string fix = char.ToLowerInvariant(written[0]) + written[1..];
            findings.Add(new Finding(
                first.Start,
                first.Length,
                $"After speech ending in a comma the tag starts in lowercase: {CharClassQuote.Open}{fix}{CharClassQuote.Close}.",
                "The speech and its tag are one sentence, so the tag does not start with a capital.",
                [Replace(first.Start, first.Length, fix)]));
        }
    }

    private static bool IsCapitalizedVerb(Paragraph paragraph, int i) => WordLists.DialogueTagVerbs.Contains(paragraph.Lower(i));
}

/// <summary><c>“Hello” she said.</c> where a comma is needed: <c>“Hello,” she said.</c></summary>
internal sealed class MissingCommaBeforeTagRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.MissingCommaBeforeTag,
        "Missing comma before a dialogue tag",
        "Flags speech that runs into its tag with no punctuation, as in “Hello” she said. A comma goes before the closing quote mark.",
        RuleCategory.DialoguePunctuation,
        true,
        false,
        ["“Hello” she said.", "“Come in” said the old man.", "“Not yet” Tom said quietly."],
        ["“Hello,” she said.", "“Hello—” she said.", "“Hello” was all she wrote.", "He called it “home” and smiled."]);

    public override RuleInfo Info => Description;

    public override void CheckParagraph(Paragraph paragraph, List<Finding> findings)
    {
        string text = paragraph.Text;
        foreach (TaggedSpeech speech in DialogueTags.Find(paragraph))
        {
            if (speech.CommaAfterQuote || !char.IsLetterOrDigit(text[speech.Quote - 1]))
            {
                continue;
            }

            // “Stay here” He went out: a capitalized pronoun points to a missing full stop, not a comma, and
            // a verb that is not purely a speech verb may be no tag at all (“form” the look reminded him).
            bool lowerPronoun = speech.StartsLower && speech.PronounFirst;
            if (!lowerPronoun && (!speech.PlainSpeechVerb || (speech.PronounFirst && !speech.StartsLower && paragraph.Lower(speech.First) != "i")))
            {
                continue;
            }

            string mark = text[speech.Quote].ToString();
            findings.Add(new Finding(
                speech.Quote,
                1,
                "A comma is needed between the speech and its tag.",
                "Speech followed by \"she said\" ends with a comma inside the quote marks.",
                [Replace(speech.Quote, 1, "," + mark)]));
        }
    }
}

/// <summary><c>“Hello”, she said.</c> where the comma belongs inside: <c>“Hello,” she said.</c></summary>
internal sealed class CommaOutsideQuoteRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.CommaOutsideQuote,
        "Comma outside the quote marks",
        "Flags a comma placed after the closing quote mark when a dialogue tag follows, as in “Hello”, she said. The comma goes inside the quote marks, with double and with single quote marks alike.",
        RuleCategory.DialoguePunctuation,
        true,
        false,
        ["“Hello”, she said.", "“Come in”, said the old man."],
        ["“Hello,” she said.", "He called it “home”, though it was not.", "“Hello!”, she said."]);

    public override RuleInfo Info => Description;

    public override void CheckParagraph(Paragraph paragraph, List<Finding> findings)
    {
        string text = paragraph.Text;
        foreach (TaggedSpeech speech in DialogueTags.Find(paragraph))
        {
            if (!speech.CommaAfterQuote || !char.IsLetterOrDigit(text[speech.Quote - 1]))
            {
                continue;
            }

            string mark = text[speech.Quote].ToString();
            findings.Add(new Finding(
                speech.Quote,
                2,
                "The comma goes inside the quote marks.",
                "Before a dialogue tag, the comma that ends the speech sits before the closing quote mark.",
                [Replace(speech.Quote, 2, "," + mark)]));
        }
    }
}
