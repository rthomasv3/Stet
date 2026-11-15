using System.Text;

namespace Stet.DataBuilder;

/// <summary>One word of the lexicon as the builder sees it.</summary>
/// <param name="Word">The word, in the case the dictionary gives it.</param>
/// <param name="Dialects">Bit i is set when dialect i (see <see cref="Sources.DialectNames"/>) accepts the word.</param>
/// <param name="NoSuggest">The word is valid but should never be offered as a suggestion.</param>
/// <param name="Count">How often it appears in recent fiction; 0 when unseen.</param>
/// <param name="Added">The word comes from this project's own lists, not from SCOWL.</param>
/// <param name="LargeOnly">The word comes only from SCOWL's large size (rarer words).</param>
internal sealed record LexiconEntry(string Word, byte Dialects, bool NoSuggest, long Count, bool LargeOnly = false, bool Added = false);

/// <summary>
/// Writes the lexicon file read by <c>Stet.Spelling.Lexicon</c>.
///
/// Format (version 1): the bytes "FCLX", a version byte, a dialect-count byte, the word count as a 4-byte
/// little-endian integer, then one record per word in UTF-8 byte order:
///   1 byte   how many leading bytes are shared with the previous word
///   1 byte   how many bytes follow
///   n bytes  the rest of the word in UTF-8
///   1 byte   flags: bits 0-4 dialects, bit 5 never-suggest, bit 6 rarer word (SCOWL large size only),
///            bit 7 added by this project's own lists
///   1 byte   frequency: 0 unseen, otherwise 1 + round(10 * log2(count / 200)), capped at 255
/// </summary>
internal static class LexiconWriter
{
    public const byte FormatVersion = 1;
    public const byte NoSuggestBit = 1 << 5;
    public const byte LargeOnlyBit = 1 << 6;
    public const byte AddedBit = 1 << 7;

    public static byte[] Write(IEnumerable<LexiconEntry> entries, int dialectCount)
    {
        var sorted = entries
            .Select(e => (Entry: e, Bytes: Encoding.UTF8.GetBytes(e.Word)))
            .OrderBy(e => e.Bytes, ByteOrder.Instance)
            .ToList();

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("FCLX"u8);
        writer.Write(FormatVersion);
        writer.Write((byte)dialectCount);
        writer.Write(sorted.Count);

        byte[] previous = [];
        foreach ((LexiconEntry entry, byte[] bytes) in sorted)
        {
            if (bytes.Length > 255)
            {
                throw new InvalidDataException($"Word too long: {entry.Word}");
            }

            int shared = 0;
            while (shared < previous.Length && shared < bytes.Length && previous[shared] == bytes[shared])
            {
                shared++;
            }

            writer.Write((byte)shared);
            writer.Write((byte)(bytes.Length - shared));
            writer.Write(bytes, shared, bytes.Length - shared);
            writer.Write((byte)(entry.Dialects | (entry.NoSuggest ? NoSuggestBit : 0) | (entry.LargeOnly ? LargeOnlyBit : 0) | (entry.Added ? AddedBit : 0)));
            writer.Write(FrequencyByte(entry.Count));
            previous = bytes;
        }

        writer.Flush();
        return stream.ToArray();
    }

    public static byte FrequencyByte(long count)
    {
        if (count < NgramExtractor.MinCount)
        {
            return 0;
        }

        double scaled = 1 + Math.Round(10 * Math.Log2((double)count / NgramExtractor.MinCount), MidpointRounding.AwayFromZero);
        return (byte)Math.Min(255, scaled);
    }

    private sealed class ByteOrder : IComparer<byte[]>
    {
        public static readonly ByteOrder Instance = new();

        public int Compare(byte[]? x, byte[]? y) => x.AsSpan().SequenceCompareTo(y);
    }
}
