using System.Collections.Frozen;
using System.Text;

namespace FictionChecker.Spelling;

/// <summary>The hand-made lists embedded in the assembly that the checker treats specially.</summary>
internal sealed class BuiltInLists
{
    private const string Prefix = "FictionChecker.Data.Lists.";

    private BuiltInLists(FrozenSet<string> informal, FrozenSet<string> archaic, FrozenDictionary<string, string> misspellings)
    {
        Informal = informal;
        Archaic = archaic;
        Misspellings = misspellings;
    }

    /// <summary>Informal speech (gonna, ain't, y'all): accepted inside dialogue when strictness is lenient.</summary>
    public FrozenSet<string> Informal { get; }

    /// <summary>Archaic forms (hath, dost, wouldst) common in fantasy and historical fiction.</summary>
    public FrozenSet<string> Archaic { get; }

    /// <summary>Common misspellings mapped to their corrections (recieve → receive, alot → a lot).</summary>
    public FrozenDictionary<string, string> Misspellings { get; }

    /// <summary>Loads the lists embedded in this assembly.</summary>
    public static BuiltInLists LoadEmbedded()
    {
        var misspellings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in ReadLines("misspellings.txt"))
        {
            int arrow = line.IndexOf('>', StringComparison.Ordinal);
            misspellings[line[..arrow]] = line[(arrow + 1)..];
        }

        return new BuiltInLists(
            ReadLines("informal.txt").ToFrozenSet(StringComparer.Ordinal),
            ReadLines("archaic.txt").ToFrozenSet(StringComparer.Ordinal),
            misspellings.ToFrozenDictionary(StringComparer.Ordinal));
    }

    /// <summary>Total size in bytes of the embedded lists.</summary>
    public static long EmbeddedSize() =>
        typeof(BuiltInLists).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal))
            .Sum(n =>
            {
                using Stream stream = typeof(BuiltInLists).Assembly.GetManifestResourceStream(n)!;
                return stream.Length;
            });

    private static List<string> ReadLines(string name)
    {
        using Stream stream = typeof(BuiltInLists).Assembly.GetManifestResourceStream(Prefix + name)
            ?? throw new InvalidOperationException($"The embedded list {name} is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();
            if (line.Length > 0 && line[0] != '#')
            {
                lines.Add(line);
            }
        }

        return lines;
    }
}
