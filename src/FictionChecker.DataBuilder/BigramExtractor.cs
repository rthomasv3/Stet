using System.Buffers;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace FictionChecker.DataBuilder;

/// <summary>
/// Streams the Google Books 2-gram files (47 files, about 31 GB compressed) and keeps a small part of them:
/// counts of one word type following another (for the tagger), and the neighbours of a short list of focus
/// words (for confusable-word statistics and for telling counted nouns from measured ones). Nothing large is
/// stored: each file is filtered as it downloads. A second mode keeps, for the function words, only the
/// counts of the word type before and after each ("they _VERB_"), which sharpen the tagger. A third keeps
/// what follows a word with one exact tag, to tell the 's of "it's" from the 's of "John's".
/// </summary>
internal static class BigramExtractor
{
    public const int Files = 47;
    public const int FirstYear = NgramExtractor.FirstYear;
    public const long MinCount = 40;

    private static readonly SearchValues<byte> Upper = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZ"u8);

    public static string Url(int file) =>
        string.Create(CultureInfo.InvariantCulture, $"http://storage.googleapis.com/books/ngrams/books/20200217/eng-fiction/2-{file:00000}-of-{Files:00000}.gz");

    /// <summary>Filters every file not already done into <paramref name="outputDir"/>, several at a time.</summary>
    public static async Task RunAsync(string outputDir, IReadOnlySet<string> focus, bool onlyBesideTypes, int parallel, CancellationToken cancel, string[]? taggedFirst = null)
    {
        byte[][]? exactFirst = taggedFirst?.Select(Encoding.UTF8.GetBytes).ToArray();
        Directory.CreateDirectory(outputDir);
        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        await Parallel.ForEachAsync(Enumerable.Range(0, Files), new ParallelOptions { MaxDegreeOfParallelism = parallel, CancellationToken = cancel }, async (file, token) =>
        {
            string output = Path.Combine(outputDir, string.Create(CultureInfo.InvariantCulture, $"2-{file:00000}.tsv"));
            if (File.Exists(output))
            {
                return;
            }

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    (long lines, long kept, string hash) = await FilterAsync(http, Url(file), output + ".part", focus, onlyBesideTypes, exactFirst, token);
                    File.Move(output + ".part", output, overwrite: true);
                    await File.WriteAllTextAsync(output + ".sha256", hash + "\n", token);
                    Console.WriteLine($"  file {file}: {lines:N0} lines read, {kept:N0} kept, SHA-256 {hash}");
                    return;
                }
                catch (Exception e) when (e is HttpRequestException or IOException or InvalidDataException && attempt < 3)
                {
                    Console.WriteLine($"  file {file}: {e.Message}; trying again");
                }
            }
        });
    }

    private static async Task<(long Lines, long Kept, string Hash)> FilterAsync(HttpClient http, string url, string outputPath, IReadOnlySet<string> focus, bool onlyBesideTypes, byte[][]? exactFirst, CancellationToken cancel)
    {
        using HttpResponseMessage response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancel);
        response.EnsureSuccessStatusCode();
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using Stream network = await response.Content.ReadAsStreamAsync(cancel);
        await using var hashing = new HashingReadStream(network, sha);
        await using var gzip = new GZipStream(hashing, CompressionMode.Decompress);
        await using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(false), 1 << 16);

        var lookup = ((HashSet<string>)focus).GetAlternateLookup<ReadOnlySpan<char>>();
        byte[] buffer = new byte[1 << 22];
        int filled = 0;
        long lines = 0;
        long kept = 0;
        while (true)
        {
            int read = await gzip.ReadAsync(buffer.AsMemory(filled), cancel);
            if (read == 0)
            {
                break;
            }

            filled += read;
            int start = 0;
            while (true)
            {
                int newline = buffer.AsSpan(start, filled - start).IndexOf((byte)'\n');
                if (newline < 0)
                {
                    break;
                }

                lines++;
                if (Keep(buffer.AsSpan(start, newline), lookup, onlyBesideTypes, exactFirst, out int tab, out long count))
                {
                    writer.Write(Encoding.UTF8.GetString(buffer, start, tab).Replace(' ', '\t'));
                    writer.Write('\t');
                    writer.WriteLine(count.ToString(CultureInfo.InvariantCulture));
                    kept++;
                }

                start += newline + 1;
            }

            if (start == 0 && filled == buffer.Length)
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }

            buffer.AsSpan(start, filled - start).CopyTo(buffer);
            filled -= start;
        }

        await writer.FlushAsync(cancel);
        return (lines, kept, Convert.ToHexStringLower(sha.GetHashAndReset()));
    }

    // A line is "first second TAB year,count,volumes TAB ...".
    private static bool Keep(ReadOnlySpan<byte> line, HashSet<string>.AlternateLookup<ReadOnlySpan<char>> focus, bool onlyBesideTypes, byte[][]? exactFirst, out int tab, out long count)
    {
        count = 0;
        tab = line.IndexOf((byte)'\t');
        if (tab <= 0)
        {
            return false;
        }

        ReadOnlySpan<byte> gram = line[..tab];
        int space = gram.IndexOf((byte)' ');
        if (space <= 0 || space == gram.Length - 1)
        {
            return false;
        }

        if (exactFirst is not null)
        {
            // Third mode: what follows a word carrying one exact tag ("'s_VERB", the 's that stands for "is").
            bool match = false;
            foreach (byte[] wantedFirst in exactFirst)
            {
                match |= gram[..space].SequenceEqual(wantedFirst);
            }

            if (!match || Classify(gram[(space + 1)..], focus) == Kind.Tagged)
            {
                return false;
            }

            count = SumRecent(line[(tab + 1)..]);
            return count >= MinCount;
        }

        Kind first = Classify(gram[..space], focus);
        Kind second = Classify(gram[(space + 1)..], focus);
        bool tags = first == Kind.Tag && second == Kind.Tag;
        // In the second mode only a focus word next to a bare word type is kept: "they _VERB_".
        bool wanted = onlyBesideTypes
            ? (first == Kind.Focus && second == Kind.Tag) || (second == Kind.Focus && first == Kind.Tag)
            : tags
                || (first == Kind.Focus && second != Kind.Tagged)
                || (second == Kind.Focus && first != Kind.Tagged);
        if (!wanted)
        {
            return false;
        }

        count = SumRecent(line[(tab + 1)..]);
        return tags ? count > 0 : count >= MinCount;
    }

    private enum Kind
    {
        Plain,
        Focus,
        Tag,
        Tagged,
    }

    private static Kind Classify(ReadOnlySpan<byte> token, HashSet<string>.AlternateLookup<ReadOnlySpan<char>> focus)
    {
        int underscore = token.IndexOf((byte)'_');
        if (underscore >= 0)
        {
            // "_NOUN_" is a word type standing for any word; "can_VERB" is a tagged word, which is not kept.
            return token.Length > 2 && token[0] == '_' && token[^1] == '_' && (token[1..^1].IndexOfAnyExcept(Upper) < 0 || token.SequenceEqual("_._"u8)) ? Kind.Tag : Kind.Tagged;
        }

        if (token.Length > 12)
        {
            return Kind.Plain;
        }

        Span<char> lower = stackalloc char[token.Length];
        for (int i = 0; i < token.Length; i++)
        {
            byte b = token[i];
            if (b >= 0x80)
            {
                return Kind.Plain;
            }

            lower[i] = (char)(b is >= (byte)'A' and <= (byte)'Z' ? b + 32 : b);
        }

        return focus.Contains(lower) ? Kind.Focus : Kind.Plain;
    }

    private static long SumRecent(ReadOnlySpan<byte> fields)
    {
        long total = 0;
        while (fields.Length > 0)
        {
            int tab = fields.IndexOf((byte)'\t');
            ReadOnlySpan<byte> field = tab < 0 ? fields : fields[..tab];
            fields = tab < 0 ? default : fields[(tab + 1)..];
            int firstComma = field.IndexOf((byte)',');
            if (firstComma <= 0)
            {
                continue;
            }

            ReadOnlySpan<byte> rest = field[(firstComma + 1)..];
            int secondComma = rest.IndexOf((byte)',');
            if (secondComma <= 0)
            {
                continue;
            }

            if (int.Parse(field[..firstComma], CultureInfo.InvariantCulture) >= FirstYear)
            {
                total += long.Parse(rest[..secondComma], CultureInfo.InvariantCulture);
            }
        }

        return total;
    }
}
