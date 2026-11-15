using System.Text.Json;

namespace Stet.Eval.Corpora;

/// <summary>Which side of the development/test split a book is on.</summary>
public enum Split
{
    /// <summary>May be looked at and tuned against.</summary>
    Dev,

    /// <summary>Measured only. Never tuned against.</summary>
    Test,
}

/// <summary>One book of a prepared corpus: story paragraphs only, in reading order.</summary>
/// <param name="Id">Book identifier.</param>
/// <param name="Split">Development or test.</param>
/// <param name="Genre">Genre label, or empty.</param>
/// <param name="Dialect">Spelling the book uses: "US" or "GB".</param>
/// <param name="Paragraphs">Story paragraphs in order.</param>
public sealed record Book(string Id, Split Split, string Genre, string Dialect, IReadOnlyList<string> Paragraphs);

/// <summary>A prepared corpus: a folder with a manifest and one text file per book.</summary>
public sealed record Corpus(string Name, IReadOnlyList<Book> Books)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Loads a prepared corpus. The folder holds <c>manifest.json</c> and, for each book, a UTF-8 text file with
    /// one paragraph per line.
    /// </summary>
    public static Corpus Load(string name, string directory)
    {
        string manifestPath = Path.Combine(directory, "manifest.json");
        Manifest manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidDataException($"Empty manifest: {manifestPath}");

        var books = new List<Book>();
        foreach (ManifestBook entry in manifest.Books.OrderBy(b => b.Id, StringComparer.Ordinal))
        {
            string[] paragraphs = File.ReadAllLines(Path.Combine(directory, entry.File))
                .Where(line => line.Length > 0)
                .ToArray();
            Split split = string.Equals(entry.Split, "test", StringComparison.OrdinalIgnoreCase) ? Split.Test : Split.Dev;
            books.Add(new Book(entry.Id, split, entry.Genre ?? "", entry.Dialect ?? "US", paragraphs));
        }

        return new Corpus(name, books);
    }

    private sealed record Manifest(string Name, List<ManifestBook> Books);

    private sealed record ManifestBook(string Id, string Split, string? Genre, string? Dialect, string File);
}
