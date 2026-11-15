using FictionChecker.Segmentation;

namespace FictionChecker.Grammar.Rules;

/// <summary>"a" before a vowel sound or "an" before a consonant sound, judged by sound: an hour, a unicorn.</summary>
internal sealed class ArticleRule : GrammarRule
{
    private static readonly RuleInfo Description = new(
        RuleIds.Article,
        "A and an",
        "Flags \"a\" before a vowel sound and \"an\" before a consonant sound. It goes by how the next word is said, not how it is spelled: \"an hour\", \"a unicorn\". Words it cannot be sure of, such as unknown names and abbreviations, are left alone.",
        RuleCategory.Mechanics,
        true,
        false,
        ["She ate a apple.", "He waited a hour.", "It was an unicorn.", "They found an book."],
        ["She ate an apple.", "He waited an hour.", "It was a unicorn.", "A one-eyed man.", "An FBI agent.", "She saw a Uruk scout.", "An' I says to him, go.", "It was an historic day."]);

    public override RuleInfo Info => Description;

    public override void Check(Sentence sentence, List<Finding> findings)
    {
        for (int i = 0; i + 1 < sentence.Count; i++)
        {
            if (!sentence.IsWord(i) || !sentence.IsWord(i + 1) || !sentence.TouchesNext(i))
            {
                continue;
            }

            string article = sentence.Lower(i);
            bool isAn = article == "an";
            if (!isAn && article != "a")
            {
                continue;
            }

            // an' is "and"; a 'orse and an 'eap are speech with a dropped letter.
            Token token = sentence.Token(i);
            Token next = sentence.Token(i + 1);
            if (token.Has(TokenFlags.TrailingApostrophe) || token.Has(TokenFlags.LeadingApostrophe) || next.Has(TokenFlags.LeadingApostrophe))
            {
                continue;
            }

            // Letters and abbreviations are said letter by letter ("an FBI agent", "a U-turn"): not judged.
            string word = sentence.Lower(i + 1);
            if (word.Length < 2 || !char.IsAsciiLetter(word[0]) || sentence.IsAllCapitals(i + 1) || (word.Length > 1 && word[1] is '-' or '.' or '\''))
            {
                continue;
            }

            bool? vowel = StartsWithVowelSound(sentence, i + 1, word);
            if (vowel is null || vowel == isAn)
            {
                continue;
            }

            // "an historic", "an hotel": an older habit that is still defended.
            if (isAn && word[0] == 'h')
            {
                continue;
            }

            string written = sentence.Written(i).ToString();
            string fix = isAn ? written[..1] : written + (char.IsUpper(written[0]) && sentence.IsAllCapitals(i + 1) ? "N" : "n");
            string nextWritten = sentence.Written(i + 1).ToString();
            findings.Add(new Finding(
                token.Start,
                token.Length,
                $"Use {CharClassQuote.Open}{fix}{CharClassQuote.Close} before {CharClassQuote.Open}{nextWritten}{CharClassQuote.Close}.",
                isAn
                    ? "\"An\" goes before a vowel sound. This word starts with a consonant sound."
                    : "\"A\" goes before a consonant sound. This word starts with a vowel sound.",
                [Replace(token.Start, token.Length, fix)]));
        }
    }

    // True for a vowel sound, false for a consonant sound, null when it cannot be told.
    private static bool? StartsWithVowelSound(Sentence sentence, int i, string word)
    {
        int index = sentence.WordIndex(i);
        int hyphen = word.IndexOf('-', StringComparison.Ordinal);
        if (hyphen > 0)
        {
            // "a one-time offer", "an eight-year gap": the first part decides.
            word = word[..hyphen];
            if (word.Length < 2)
            {
                return null;
            }

            index = sentence.Data.Lexicon.IndexOf(word);
        }

        bool spelledVowel = word[0] is 'a' or 'e' or 'i' or 'o' or 'u';
        if (index < 0)
        {
            // Not a dictionary word (a name, a coined word): trust the spelling only where it is safe.
            return word[0] is 'h' or 'u' or 'y' || word.StartsWith("eu", StringComparison.Ordinal) || word.StartsWith("ew", StringComparison.Ordinal) || word.StartsWith("on", StringComparison.Ordinal) || word.StartsWith("ou", StringComparison.Ordinal)
                ? null
                : spelledVowel;
        }

        return sentence.Data.SoundOf(index) switch
        {
            StartSound.Vowel => true,
            StartSound.Consonant => false,
            StartSound.AsSpelled => spelledVowel,
            _ => null,
        };
    }
}
