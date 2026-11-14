using FictionChecker.Eval.Text;

namespace FictionChecker.Eval.Planting;

/// <summary>Breaks pronoun and verb agreement ("they were" → "they was").</summary>
public sealed class AgreementPlanter : IErrorPlanter
{
    private static readonly Dictionary<(string Pronoun, string Verb), string> Swaps = new()
    {
        [("they", "were")] = "was", [("we", "were")] = "was", [("you", "were")] = "was",
        [("he", "was")] = "were", [("she", "was")] = "were", [("it", "was")] = "were",
        [("i", "have")] = "has", [("they", "have")] = "has", [("we", "have")] = "has",
        [("he", "has")] = "have", [("she", "has")] = "have", [("it", "has")] = "have",
        [("he", "doesn't")] = "don't", [("she", "doesn't")] = "don't", [("it", "doesn't")] = "don't",
        [("they", "don't")] = "doesn't", [("i", "don't")] = "doesn't", [("we", "don't")] = "doesn't",
        [("we", "are")] = "is", [("they", "are")] = "is", [("you", "are")] = "is",
        [("he", "is")] = "are", [("she", "is")] = "are", [("i", "am")] = "is",
    };

    // After these, "he were" and the like are the subjunctive and correct, so nothing is planted.
    private static readonly HashSet<string> SubjunctiveLeadIns = new(StringComparer.Ordinal) { "if", "though", "wish", "wished", "wishes", "as", "suppose", "supposing", "whether", "unless", "lest" };

    /// <inheritdoc />
    public string Category => "agreement";

    /// <inheritdoc />
    public PlantedError? TryPlant(string paragraph, Rng rng)
    {
        List<Token> tokens = Words.Tokenize(paragraph);
        var candidates = new List<(int Index, string Replacement)>();
        for (int i = 0; i + 1 < tokens.Count; i++)
        {
            Token pronoun = tokens[i];
            Token verb = tokens[i + 1];
            if (verb.Start != pronoun.End + 1 || paragraph[pronoun.End] != ' ')
            {
                continue;
            }

            var key = (Casing.Normalize(pronoun.Text), Casing.Normalize(verb.Text));
            if (!Swaps.TryGetValue(key, out string? replacement) || verb.Text != verb.Text.ToLowerInvariant())
            {
                continue;
            }

            // The pronoun must look like the subject: not "between you and he was", not "made them. He".
            bool leadIn = i > 0 && SubjunctiveLeadIns.Contains(Casing.Normalize(tokens[i - 1].Text));
            bool joined = i > 0 && Casing.Normalize(tokens[i - 1].Text) is "and" or "or" or "of" or "than";
            if (!leadIn && !joined)
            {
                candidates.Add((i, replacement));
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        (int index, string swap) = rng.Pick(candidates);
        Token subject = tokens[index];
        Token target = tokens[index + 1];
        string wrong = Casing.MatchApostrophe(target.Text, swap);
        string text = string.Concat(paragraph.AsSpan(0, target.Start), wrong, paragraph.AsSpan(target.End));
        return new PlantedError(
            text,
            subject.Start,
            target.Start + wrong.Length - subject.Start,
            subject.Start,
            target.End - subject.Start,
            $"{Casing.Normalize(subject.Text)} {Casing.Normalize(target.Text)}>{swap}");
    }
}
