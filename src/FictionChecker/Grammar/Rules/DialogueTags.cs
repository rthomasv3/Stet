using FictionChecker.Segmentation;

namespace FictionChecker.Grammar.Rules;

/// <summary>A line of speech that is closed by a quote mark and followed by a dialogue tag.</summary>
/// <param name="Quote">Position of the closing quote mark.</param>
/// <param name="CommaAfterQuote">A comma sits directly after the closing quote mark.</param>
/// <param name="First">The tag's first token.</param>
/// <param name="Verb">The tag's speech verb.</param>
/// <param name="StartsLower">The tag starts with a lowercase word: "she said", "said Tom", "the man said".</param>
/// <param name="EndsThere">The tag ends right after its verb (or its subject, for "said Tom").</param>
internal readonly record struct TaggedSpeech(int Quote, bool CommaAfterQuote, int First, int Verb, bool StartsLower, bool EndsThere);

/// <summary>
/// Finds speech followed by a tag. A tag is "she said", "Tom said", "the old man said" or "said Tom": a
/// subject and one of the listed speech verbs, straight after the closing quote mark. Anything else after
/// speech is an action beat (<c>“Hello.” She smiled.</c>) and none of the dialogue rules touch it.
/// </summary>
internal static class DialogueTags
{
    private static readonly HashSet<string> Pronouns = new(StringComparer.Ordinal) { "he", "she", "they", "we", "i", "it", "you" };
    private static readonly HashSet<string> NounStarters = new(StringComparer.Ordinal) { "the", "a", "an", "his", "her", "my", "their", "our", "your", "one", "someone", "somebody", "another" };

    /// <summary>Every closed line of speech in the paragraph that a tag follows.</summary>
    public static IEnumerable<TaggedSpeech> Find(Paragraph paragraph)
    {
        if (paragraph.Dialogue is null)
        {
            yield break;
        }

        string text = paragraph.Text;
        foreach (TextSpan span in paragraph.Dialogue.Spans)
        {
            int quote = span.End - 1;
            if (span.Length < 3 || !IsClosingQuote(text[quote], paragraph.QuoteStyle))
            {
                continue; // speech still open at the end of the paragraph, or a range the caller marked
            }

            int after = span.End;
            bool comma = after < text.Length && text[after] == ',';
            if (comma)
            {
                after++;
            }

            int first = paragraph.TokenAtOrAfter(after);
            if (first >= paragraph.Tokens.Count || !paragraph.IsWord(first) || !paragraph.OnlySpacesBetween(after, paragraph.Tokens[first].Start))
            {
                continue;
            }

            if (TryReadTag(paragraph, first, out int verb, out bool startsLower, out bool endsThere))
            {
                yield return new TaggedSpeech(quote, comma, first, verb, startsLower, endsThere);
            }
        }
    }

    private static bool IsClosingQuote(char c, QuoteStyle style) =>
        style == QuoteStyle.Double ? c is '"' or CharClass.RightDoubleQuote : c is '\'' or CharClass.RightSingleQuote;

    private static bool IsTagVerb(Paragraph paragraph, int i) =>
        paragraph.IsWord(i) && char.IsLower(paragraph.Text[paragraph.Tokens[i].Start]) && WordLists.DialogueTagVerbs.Contains(paragraph.Lower(i));

    private static bool Touches(Paragraph paragraph, int i) =>
        i + 1 < paragraph.Tokens.Count && paragraph.OnlySpacesBetween(paragraph.Tokens[i].End, paragraph.Tokens[i + 1].Start);

    private static bool EndsAfter(Paragraph paragraph, int i) =>
        i + 1 >= paragraph.Tokens.Count || paragraph.IsMark(i + 1, '.') || paragraph.IsMark(i + 1, ',');

    private static bool TryReadTag(Paragraph paragraph, int first, out int verb, out bool startsLower, out bool endsThere)
    {
        string text = paragraph.Text;
        verb = -1;
        endsThere = false;
        string word = paragraph.Lower(first);
        startsLower = char.IsLower(text[paragraph.Tokens[first].Start]);

        // "said Tom", "asked the old man": the verb first, then who.
        if (WordLists.DialogueTagVerbs.Contains(word))
        {
            if (!Touches(paragraph, first) || !paragraph.IsWord(first + 1))
            {
                return false;
            }

            bool named = char.IsUpper(text[paragraph.Tokens[first + 1].Start]);
            string subject = paragraph.Lower(first + 1);
            if (!named && !NounStarters.Contains(subject) && !(Pronouns.Contains(subject) && subject is not ("it" or "you")))
            {
                return false;
            }

            verb = first;
            endsThere = EndsAfter(paragraph, first + 1);
            return true;
        }

        // "she said": a pronoun, then the verb.
        if (Pronouns.Contains(word) && (word != "i" || text[paragraph.Tokens[first].Start] == 'I'))
        {
            if (Touches(paragraph, first) && IsTagVerb(paragraph, first + 1))
            {
                verb = first + 1;
                endsThere = EndsAfter(paragraph, verb);
                if (word == "i")
                {
                    startsLower = false; // "I" tells nothing about the capital
                }

                return true;
            }

            return false;
        }

        // "Tom said", "Lady Anne Grey said", "the old man said": up to three words, then the verb.
        bool nounPhrase = startsLower && NounStarters.Contains(word);
        if (!nounPhrase && startsLower)
        {
            return false;
        }

        for (int i = first; i < first + 3 && i < paragraph.Tokens.Count; i++)
        {
            if (!paragraph.IsWord(i) || !Touches(paragraph, i))
            {
                return false;
            }

            if (i > first && !nounPhrase && !char.IsUpper(text[paragraph.Tokens[i].Start]))
            {
                return false;
            }

            if (IsTagVerb(paragraph, i + 1))
            {
                verb = i + 1;
                endsThere = EndsAfter(paragraph, verb);
                return true;
            }
        }

        return false;
    }
}
