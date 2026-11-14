using System.Globalization;
using System.Security.Cryptography;

namespace FictionChecker.DataBuilder;

/// <summary>Builds the lexicon from SCOWL's dialect dictionaries and the fiction word counts.</summary>
internal static class LexiconBuilder
{
    private static readonly HashSet<string> Tags = new(StringComparer.Ordinal)
    {
        "NOUN", "VERB", "ADJ", "ADV", "PRON", "DET", "ADP", "NUM", "CONJ", "PRT", "X", ".",
    };

    private const byte AllDialects = 0b11111;

    /// <summary>Hand-made word lists merged into the lexicon.</summary>
    public static readonly string[] OwnLists = ["genre.txt", "general.txt", "interjections.txt"];

    /// <summary>Reads a word list: one entry per line, blank lines and # comments ignored.</summary>
    public static IEnumerable<string> ReadList(string path) =>
        File.ReadLines(path).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#'));

    /// <summary>Expands each dialect, merges them into one list and attaches frequencies.</summary>
    public static async Task<List<LexiconEntry>> BuildAsync(string downloads, string ngramTable, string listsDirectory)
    {
        var dialects = new Dictionary<string, (byte Mask, bool NoSuggest)>(StringComparer.Ordinal);
        for (int d = 0; d < Sources.Scowl.Length; d++)
        {
            string zip = await FetchAsync(Sources.Scowl[d], Path.Combine(downloads, "scowl"));
            List<HunspellExpander.Entry> words = HunspellExpander.ExpandZip(zip);
            Console.WriteLine($"  {Sources.DialectNames[d],-10} {words.Count,8:N0} words");
            foreach (HunspellExpander.Entry entry in words)
            {
                (byte mask, bool noSuggest) = dialects.GetValueOrDefault(entry.Word, ((byte)0, true));
                dialects[entry.Word] = ((byte)(mask | (1 << d)), noSuggest && entry.NoSuggest);
            }
        }

        // Large dictionaries: words not in any normal list are added and marked large-only.
        var largeOnly = new HashSet<string>(StringComparer.Ordinal);
        (SourceFile Source, byte Mask)[] large =
        [
            (Sources.ScowlLargeUS, 0b00001),
            (Sources.ScowlLargeGB, 0b00110),
            (Sources.ScowlLargeCA, 0b01000),
            (Sources.ScowlLargeAU, 0b10000),
        ];
        var normalWords = new HashSet<string>(dialects.Keys, StringComparer.Ordinal);
        foreach ((SourceFile source, byte mask) in large)
        {
            string zip = await FetchAsync(source, Path.Combine(downloads, "scowl"));
            List<HunspellExpander.Entry> words = HunspellExpander.ExpandZip(zip);
            int added = 0;
            foreach (HunspellExpander.Entry entry in words)
            {
                if (normalWords.Contains(entry.Word))
                {
                    // Already placed by the normal lists, which know -ise from -ize. Only widen to this dialect
                    // when no normal list of the dialect group has it (US, CA and AU have one list each).
                    (byte have, bool ns) = dialects[entry.Word];
                    if ((have & mask) == 0 && mask != 0b00110)
                    {
                        dialects[entry.Word] = ((byte)(have | mask), ns);
                    }

                    continue;
                }

                (byte existing, bool noSuggest) = dialects.GetValueOrDefault(entry.Word, ((byte)0, true));
                dialects[entry.Word] = ((byte)(existing | mask), noSuggest && entry.NoSuggest);
                largeOnly.Add(entry.Word);
                added++;
            }

            Console.WriteLine($"  {source.Name,-36} {words.Count,8:N0} words, {added,7:N0} beyond the normal lists");
        }

        // SCOWL lists one variant per dialect (British "amongst", American "among"), but fiction in every
        // dialect uses both. A word stays restricted only when it has a twin in the other dialects that
        // differs by a systematic spelling pattern (colour/color); otherwise every dialect accepts it.
        int widened = 0;
        foreach (string word in dialects.Keys.ToList())
        {
            (byte mask, bool noSuggest) = dialects[word];
            if (mask != AllDialects && !HasDialectTwin(word, mask, dialects))
            {
                dialects[word] = (AllDialects, noSuggest);
                widened++;
            }
        }

        Console.WriteLine($"  {widened:N0} variants opened to every dialect; {dialects.Count(kv => kv.Value.Mask != AllDialects):N0} words stay dialect-specific");

        // The library accepts X's whenever X is a word, so storing possessives would only repeat the list.
        int before = dialects.Count;
        foreach (string word in dialects.Keys.Where(w => w.EndsWith("'s", StringComparison.Ordinal)).ToList())
        {
            if (dialects.TryGetValue(word[..^2], out (byte Mask, bool NoSuggest) stem) && (stem.Mask & dialects[word].Mask) == dialects[word].Mask)
            {
                dialects.Remove(word);
            }
        }

        Console.WriteLine($"  dropped {before - dialects.Count:N0} possessives whose base word is listed");

        Dictionary<string, long> counts = ReadCounts(ngramTable);
        var entries = dialects
            .Select(kv => new LexiconEntry(kv.Key, kv.Value.Mask, kv.Value.NoSuggest, CountFor(kv.Key, counts), largeOnly.Contains(kv.Key)))
            .ToList();

        // This project's own lists: valid in every dialect.
        foreach (string list in OwnLists)
        {
            int added = 0;
            foreach (string word in ReadList(Path.Combine(listsDirectory, list)))
            {
                if (dialects.TryAdd(word, (AllDialects, false)))
                {
                    entries.Add(new LexiconEntry(word, AllDialects, false, CountFor(word, counts), Added: true));
                    added++;
                }
            }

            Console.WriteLine($"  {list,-36} {added,8:N0} words added");
        }

        return entries;
    }

    // Spelling patterns that separate the dialects, as (one side, other side).
    private static readonly (string A, string B)[] DialectPatterns =
    [
        ("our", "or"), ("tre", "ter"), ("bre", "ber"), ("is", "iz"), ("ys", "yz"), ("ll", "l"), ("pp", "p"),
        ("tt", "t"), ("ence", "ense"), ("ogue", "og"), ("ae", "e"), ("oe", "e"), ("oul", "ol"), ("mme", "m"),
        ("oeu", "eu"), ("gue", "g"),
    ];

    // Dialect words whose twin does not follow a pattern. They stay restricted to their own dialects.
    private static readonly HashSet<string> IrregularDialectWords = new(StringComparer.Ordinal)
    {
        "tyre", "tyres", "kerb", "kerbs", "plough", "ploughs", "ploughed", "ploughing", "plow", "plows", "plowed",
        "plowing", "pyjamas", "pajamas", "aluminium", "aluminum", "jewellery", "jewelry", "sceptic", "sceptics",
        "sceptical", "skeptic", "skeptics", "skeptical", "cosy", "cozy", "cheque", "cheques", "manoeuvre",
        "manoeuvres", "manoeuvred", "manoeuvring", "maneuver", "maneuvers", "maneuvered", "maneuvering",
    };

    private static bool HasDialectTwin(string word, byte mask, Dictionary<string, (byte Mask, bool NoSuggest)> dialects)
    {
        if (IrregularDialectWords.Contains(word.ToLowerInvariant()))
        {
            return true;
        }

        foreach ((string a, string b) in DialectPatterns)
        {
            foreach ((string from, string to) in new[] { (a, b), (b, a) })
            {
                for (int at = word.IndexOf(from, StringComparison.Ordinal); at >= 0; at = word.IndexOf(from, at + 1, StringComparison.Ordinal))
                {
                    string twin = string.Concat(word.AsSpan(0, at), to, word.AsSpan(at + from.Length));
                    // The twin must cover a dialect this word lacks, and must itself be restricted.
                    if (dialects.TryGetValue(twin, out (byte Mask, bool NoSuggest) other) && (other.Mask & ~mask) != 0 && other.Mask != AllDialects)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Reads the 1-gram table into lowercase word totals. Part-of-speech tagged forms (can_VERB) are skipped
    /// here; they are for the grammar data.
    /// </summary>
    public static Dictionary<string, long> ReadCounts(string ngramTable)
    {
        var counts = new Dictionary<string, long>(1_000_000, StringComparer.Ordinal);
        foreach (string line in File.ReadLines(ngramTable))
        {
            int tab = line.IndexOf('\t');
            string token = line[..tab];
            int underscore = token.LastIndexOf('_');
            if (underscore >= 0 && (underscore == 0 || Tags.Contains(token[(underscore + 1)..])))
            {
                continue;
            }

            string key = token.ToLowerInvariant();
            counts[key] = counts.GetValueOrDefault(key) + long.Parse(line.AsSpan(tab + 1), CultureInfo.InvariantCulture);
        }

        return counts;
    }

    /// <summary>
    /// The count for a word. The corpus splits contractions into two tokens (do + n't), so a contraction's own
    /// count is far too low; it borrows an eighth of its stem's count instead.
    /// </summary>
    public static long CountFor(string word, Dictionary<string, long> counts)
    {
        string key = word.ToLowerInvariant();
        long own = counts.GetValueOrDefault(key);
        int apostrophe = key.IndexOf('\'', StringComparison.Ordinal);
        if (apostrophe <= 0)
        {
            return own;
        }

        string stem = key.EndsWith("n't", StringComparison.Ordinal) ? key[..^3] : key[..apostrophe];
        stem = stem switch
        {
            "ca" => "can",
            "wo" => "will",
            "sha" => "shall",
            _ => stem,
        };
        return Math.Max(own, counts.GetValueOrDefault(stem) / 8);
    }

    /// <summary>Downloads a source file into the cache folder if it is not there, and checks its checksum.</summary>
    public static async Task<string> FetchAsync(SourceFile source, string cacheDirectory)
    {
        Directory.CreateDirectory(cacheDirectory);
        string path = Path.Combine(cacheDirectory, source.Name);
        if (!File.Exists(path))
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            byte[] bytes = await http.GetByteArrayAsync(source.Url);
            await File.WriteAllBytesAsync(path, bytes);
        }

        string hash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path)));
        if (hash != source.Sha256)
        {
            throw new InvalidDataException($"{source.Name}: checksum {hash} does not match the recorded {source.Sha256}.");
        }

        return path;
    }
}
