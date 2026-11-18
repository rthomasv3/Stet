namespace Stet.Eval.Scoring;

/// <summary>False alarms for one rule in one group.</summary>
public sealed record RuleResult(string RuleId, string Category, int Flags, double Per1000);

/// <summary>How one category of planted error fared in one group.</summary>
public sealed record PlantedResult(string Category, int Planted, int Checked, int Caught, int FirstFixRight, int FixInTop5)
{
    /// <summary>Share of checked planted errors that were flagged.</summary>
    public double CatchRate => Ratio(Caught, Checked);

    /// <summary>Share of caught errors where the first suggestion restores the original text.</summary>
    public double FirstFixRate => Ratio(FirstFixRight, Caught);

    /// <summary>Share of caught errors where one of the first five suggestions restores the original text.</summary>
    public double Top5FixRate => Ratio(FixInTop5, Caught);

    private static double Ratio(int part, int whole) => whole == 0 ? 0 : Math.Round((double)part / whole, 4);
}

/// <summary>Results for one corpus and one side of the split (or a total).</summary>
public sealed record GroupResult(
    string Corpus,
    string Split,
    int Books,
    int Paragraphs,
    int UncheckedParagraphs,
    long Words,
    int Flags,
    int ConfirmedRealErrors,
    IReadOnlyList<RuleResult> Rules,
    IReadOnlyList<PlantedResult> Planted)
{
    /// <summary>False alarms per 1,000 words of clean text. Confirmed real errors are not counted.</summary>
    public double FlagsPer1000 => Words == 0 ? 0 : Math.Round(Flags * 1000.0 / Words, 4);
}

/// <summary>A full evaluation report. Numbers only, safe to commit.</summary>
/// <param name="Checker">The report's name.</param>
/// <param name="LibraryVersion">The version of Stet the harness was built with.</param>
/// <param name="Settings">What was measured.</param>
/// <param name="Groups">Results for each corpus and side of the split, and the totals.</param>
/// <param name="CheckerVersion">For a checker that is not Stet: what it is and its version, as given on the command line.</param>
/// <param name="Description">One sentence on how the checker was set up for this report, or null.</param>
public sealed record EvalReport(string Checker, string LibraryVersion, EvalSettings Settings, IReadOnlyList<GroupResult> Groups, string? CheckerVersion = null, string? Description = null)
{
    /// <summary>Finds a group, or null.</summary>
    public GroupResult? Find(string corpus, string split) =>
        Groups.FirstOrDefault(g => g.Corpus == corpus && g.Split == split);
}

/// <summary>A flag on clean text, with enough context to review it. Contains book text: local reports only.</summary>
public sealed record ReviewFlag(string Corpus, string BookId, int Paragraph, string Hash, int Start, int Length, string RuleId, string Snippet);

/// <summary>A planted error the checker did not catch. Contains book text: local reports only.</summary>
public sealed record MissedPlant(string Corpus, string BookId, int Paragraph, string Category, string Kind, string Snippet);
