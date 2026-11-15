using System.Collections.Frozen;

namespace Stet;

/// <summary>
/// Settings for checking. A checker is created with one set of options, and any check can be given another,
/// so one checker can serve books with different settings. Make a changed copy with a <c>with</c> expression.
/// </summary>
public sealed record CheckerOptions
{
    private static readonly FrozenDictionary<string, bool> NoRules = FrozenDictionary<string, bool>.Empty;
    private static readonly FrozenDictionary<RuleCategory, bool> NoCategories = FrozenDictionary<RuleCategory, bool>.Empty;

    /// <summary>The defaults: American English, lenient dialogue, every rule at its own default.</summary>
    public static CheckerOptions Default { get; } = new();

    /// <summary>The dialect whose spellings are correct.</summary>
    public Dialect Dialect { get; init; } = Dialect.American;

    /// <summary>
    /// Which quote marks delimit speech. Null means the dialect's usual style: single for British English,
    /// double for the others.
    /// </summary>
    public QuoteStyle? QuoteStyle { get; init; }

    /// <summary>How much to check inside dialogue.</summary>
    public DialogueStrictness DialogueStrictness { get; init; } = DialogueStrictness.Lenient;

    /// <summary>Most suggestions offered for one issue. Zero skips working out suggestions, which is faster.</summary>
    public int MaxSuggestions { get; init; } = 5;

    /// <summary>
    /// Rules turned on or off by ID, overriding their defaults and <see cref="CategoryOverrides"/>. IDs that
    /// no rule has are ignored, so settings saved by another version stay usable.
    /// </summary>
    public IReadOnlyDictionary<string, bool> RuleOverrides { get; init; } = NoRules;

    /// <summary>Whole categories turned on or off. Turning a category on enables every rule in it.</summary>
    public IReadOnlyDictionary<RuleCategory, bool> CategoryOverrides { get; init; } = NoCategories;

    /// <summary>
    /// Names of the word sets that apply. Null means every set the checker holds. Names of sets that do not
    /// exist are ignored.
    /// </summary>
    public IReadOnlyCollection<string>? WordSets { get; init; }

    /// <summary>The quote style in effect.</summary>
    public QuoteStyle EffectiveQuoteStyle =>
        QuoteStyle ?? (Dialect is Dialect.British or Dialect.BritishIze ? Stet.QuoteStyle.Single : Stet.QuoteStyle.Double);

    /// <summary>Returns a copy with one rule turned on or off.</summary>
    public CheckerOptions WithRule(string ruleId, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(ruleId);
        var rules = new Dictionary<string, bool>(RuleOverrides, StringComparer.Ordinal) { [ruleId] = enabled };
        return this with { RuleOverrides = rules };
    }

    /// <summary>Returns a copy with one category turned on or off.</summary>
    public CheckerOptions WithCategory(RuleCategory category, bool enabled)
    {
        var categories = new Dictionary<RuleCategory, bool>(CategoryOverrides) { [category] = enabled };
        return this with { CategoryOverrides = categories };
    }

    /// <summary>Whether a rule is on under these options.</summary>
    public bool IsEnabled(RuleInfo rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (RuleOverrides.TryGetValue(rule.Id, out bool byRule))
        {
            return byRule;
        }

        return CategoryOverrides.TryGetValue(rule.Category, out bool byCategory) ? byCategory : rule.EnabledByDefault;
    }

    /// <summary>Options are equal when every setting is, whatever objects hold the overrides and set names.</summary>
    public bool Equals(CheckerOptions? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
            && Dialect == other.Dialect
            && QuoteStyle == other.QuoteStyle
            && DialogueStrictness == other.DialogueStrictness
            && MaxSuggestions == other.MaxSuggestions
            && SameEntries(RuleOverrides, other.RuleOverrides)
            && SameEntries(CategoryOverrides, other.CategoryOverrides)
            && SameNames(WordSets, other.WordSets);
    }

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Dialect, QuoteStyle, DialogueStrictness, MaxSuggestions, RuleOverrides.Count, CategoryOverrides.Count, WordSets?.Count ?? -1);

    private static bool SameEntries<TKey>(IReadOnlyDictionary<TKey, bool> a, IReadOnlyDictionary<TKey, bool> b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (KeyValuePair<TKey, bool> entry in a)
        {
            if (!b.TryGetValue(entry.Key, out bool value) || value != entry.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameNames(IReadOnlyCollection<string>? a, IReadOnlyCollection<string>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        var left = new HashSet<string>(a, StringComparer.Ordinal);
        return left.SetEquals(b);
    }
}
