namespace Stet.Grammar;

/// <summary>What a rule found. The flagged range and the fixes are positions in the paragraph.</summary>
internal sealed record Finding(int Start, int Length, string Message, string Explanation, IReadOnlyList<Suggestion> Suggestions);

/// <summary>
/// A grammar rule. Each rule is one class holding everything about it: its description for the rule list,
/// examples that double as tests, and the check itself.
/// </summary>
internal abstract class GrammarRule
{
    /// <summary>The rule's entry in the rule list.</summary>
    public abstract RuleInfo Info { get; }

    /// <summary>Looks at one sentence, with its words tagged, and adds what it finds.</summary>
    public virtual void Check(Sentence sentence, List<Finding> findings)
    {
    }

    /// <summary>
    /// Looks at the whole paragraph once. For rules about spacing and punctuation, which do not need word
    /// types and may reach across sentences.
    /// </summary>
    public virtual void CheckParagraph(Paragraph paragraph, List<Finding> findings)
    {
    }

    /// <summary>A fix that replaces a range of the paragraph.</summary>
    protected static Suggestion Replace(int start, int length, string text) => new(start, length, text);
}
