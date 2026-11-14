using System.Text.Json;

namespace FictionChecker.Eval;

/// <summary>Settings that decide what is measured. Committed, so every run measures the same thing.</summary>
public sealed record EvalSettings
{
    /// <summary>Seed for sampling and error planting.</summary>
    public ulong Seed { get; init; } = 20261114;

    /// <summary>Clean text checked per book: one run of consecutive paragraphs of about this many words.</summary>
    public int CleanWordsPerBook { get; init; } = 30000;

    /// <summary>Errors planted per category, for each corpus and each side of the split.</summary>
    public int PlantedPerCategory { get; init; } = 500;

    /// <summary>Flags written per rule to the local review file.</summary>
    public int ReviewFlagsPerRule { get; init; } = 40;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Reads settings from a JSON file, or returns the defaults when it does not exist.</summary>
    public static EvalSettings Load(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<EvalSettings>(File.ReadAllText(path), JsonOptions) ?? new EvalSettings() : new EvalSettings();
}

/// <summary>Machine-specific settings (where the corpora are). Never committed.</summary>
public sealed record LocalSettings
{
    /// <summary>Corpus name to the folder holding its prepared files.</summary>
    public Dictionary<string, string> Corpora { get; init; } = [];

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Reads the local settings file.</summary>
    public static LocalSettings Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Local settings not found. Copy eval/eval.local.example.json to {path} and set the corpus folders.", path);
        }

        return JsonSerializer.Deserialize<LocalSettings>(File.ReadAllText(path), JsonOptions) ?? new LocalSettings();
    }
}
