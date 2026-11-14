using FictionChecker.Eval.Text;

namespace FictionChecker.Eval.Planting;

/// <summary>
/// A paragraph with one known error planted in it.
/// </summary>
/// <param name="Text">The paragraph with the error.</param>
/// <param name="Start">Start of the region where a flag counts as catching the error (in <see cref="Text"/>).</param>
/// <param name="Length">Length of that region.</param>
/// <param name="OriginalStart">Start of the matching region in the clean paragraph.</param>
/// <param name="OriginalLength">Length of the matching region in the clean paragraph.</param>
/// <param name="Kind">The specific change, for example "transpose" or "their>there".</param>
public sealed record PlantedError(string Text, int Start, int Length, int OriginalStart, int OriginalLength, string Kind);

/// <summary>Plants one category of error into clean text.</summary>
public interface IErrorPlanter
{
    /// <summary>Category name used in reports.</summary>
    string Category { get; }

    /// <summary>Plants one error, or returns null when the paragraph has no suitable place.</summary>
    PlantedError? TryPlant(string paragraph, Rng rng);
}

/// <summary>The planters, one per category.</summary>
public static class Planters
{
    /// <summary>All planters in report order.</summary>
    public static IReadOnlyList<IErrorPlanter> All { get; } =
    [
        new TypoPlanter(),
        new ConfusablePlanter(),
        new ArticlePlanter(),
        new RepeatedWordPlanter(),
        new AgreementPlanter(),
        new DialoguePunctuationPlanter(),
    ];
}

internal static class Casing
{
    /// <summary>Gives <paramref name="replacement"/> the capitalization of <paramref name="original"/>.</summary>
    public static string Match(string original, string replacement)
    {
        if (original.Length > 1 && original.All(c => !char.IsLetter(c) || char.IsUpper(c)))
        {
            return replacement.ToUpperInvariant();
        }

        if (char.IsUpper(original[0]))
        {
            return char.ToUpperInvariant(replacement[0]) + replacement[1..];
        }

        return replacement;
    }

    /// <summary>Uses the apostrophe style of <paramref name="original"/> (straight or curly) in <paramref name="replacement"/>.</summary>
    public static string MatchApostrophe(string original, string replacement) =>
        original.Contains('’') ? replacement.Replace('\'', '’') : replacement;

    /// <summary>Lowercases a word and makes its apostrophes straight.</summary>
    public static string Normalize(string word) => word.ToLowerInvariant().Replace('’', '\'');
}
