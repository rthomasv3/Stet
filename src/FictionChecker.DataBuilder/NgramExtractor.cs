using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace FictionChecker.DataBuilder;

/// <summary>
/// Streams the Google Books 1-gram file and keeps one line per token: its total count in recent books.
/// The 0.94 GB download is never stored; only the small filtered table is.
/// </summary>
internal static class NgramExtractor
{
    /// <summary>Counts are summed from this year on, so they reflect modern fiction.</summary>
    public const int FirstYear = 1950;

    /// <summary>Tokens rarer than this in total are dropped.</summary>
    public const long MinCount = 200;

    /// <summary>
    /// Downloads, filters and writes <c>token TAB count</c> lines (plain words and part-of-speech tagged forms
    /// such as <c>can_VERB</c>). Returns the SHA-256 of the downloaded file.
    /// </summary>
    public static async Task<string> RunAsync(SourceFile source, string outputPath, CancellationToken cancel)
    {
        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        using HttpResponseMessage response = await http.GetAsync(source.Url, HttpCompletionOption.ResponseHeadersRead, cancel);
        response.EnsureSuccessStatusCode();

        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using Stream network = await response.Content.ReadAsStreamAsync(cancel);
        await using var hashing = new HashingReadStream(network, sha);
        await using var gzip = new GZipStream(hashing, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8, false, 1 << 20);

        string temp = outputPath + ".part";
        long lines = 0;
        long kept = 0;
        await using (var writer = new StreamWriter(temp, false, new UTF8Encoding(false)))
        {
            while (await reader.ReadLineAsync(cancel) is { } line)
            {
                lines++;
                int tab = line.IndexOf('\t');
                if (tab <= 0)
                {
                    continue;
                }

                long count = SumRecent(line.AsSpan(tab + 1));
                if (count >= MinCount)
                {
                    writer.Write(line.AsSpan(0, tab));
                    writer.Write('\t');
                    writer.WriteLine(count.ToString(CultureInfo.InvariantCulture));
                    kept++;
                }

                if (lines % 2_000_000 == 0)
                {
                    Console.WriteLine($"  {lines:N0} tokens read, {kept:N0} kept");
                }
            }
        }

        File.Move(temp, outputPath, overwrite: true);
        string hash = Convert.ToHexStringLower(sha.GetHashAndReset());
        Console.WriteLine($"Done: {lines:N0} tokens read, {kept:N0} kept. SHA-256 {hash}");
        return hash;
    }

    // Each field is "year,match_count,volume_count".
    private static long SumRecent(ReadOnlySpan<char> fields)
    {
        long total = 0;
        while (fields.Length > 0)
        {
            int tab = fields.IndexOf('\t');
            ReadOnlySpan<char> field = tab < 0 ? fields : fields[..tab];
            fields = tab < 0 ? default : fields[(tab + 1)..];

            int firstComma = field.IndexOf(',');
            if (firstComma <= 0)
            {
                continue;
            }

            ReadOnlySpan<char> rest = field[(firstComma + 1)..];
            int secondComma = rest.IndexOf(',');
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

/// <summary>Passes a stream through while adding what is read to a hash.</summary>
internal sealed class HashingReadStream(Stream inner, IncrementalHash hash) : Stream
{
    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = inner.Read(buffer, offset, count);
        hash.AppendData(buffer, offset, read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int read = await inner.ReadAsync(buffer, cancellationToken);
        hash.AppendData(buffer.Span[..read]);
        return read;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
