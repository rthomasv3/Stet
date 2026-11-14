using System.Buffers.Binary;
using System.Reflection;
using System.Text;

namespace FictionChecker.Spelling;

/// <summary>The spelling dialects, in the order of their bits in the lexicon data.</summary>
internal enum Dialect
{
    /// <summary>American English.</summary>
    US = 0,

    /// <summary>British English with -ise spellings (organise).</summary>
    GBIse = 1,

    /// <summary>British English with -ize spellings (organize, as Oxford prefers).</summary>
    GBIze = 2,

    /// <summary>Canadian English.</summary>
    CA = 3,

    /// <summary>Australian English.</summary>
    AU = 4,
}

/// <summary>What the lexicon knows about one word.</summary>
/// <param name="Flags">
/// Bits 0-4: the dialects that accept the word. Bit 5: never offer it as a suggestion. Bit 6: a rarer word. Bit 7: added by this project (genre and general lists).
/// </param>
/// <param name="Frequency">
/// How common the word is in recent fiction on a log scale: 0 means unseen, and each 10 steps doubles the count.
/// </param>
internal readonly record struct WordInfo(byte Flags, byte Frequency)
{
    private const byte NoSuggestBit = 1 << 5;
    private const byte RareBit = 1 << 6;
    private const byte AddedBit = 1 << 7;
    private const byte AllDialects = 0b11111;

    /// <summary>Whether the given dialect accepts the word.</summary>
    public bool IsIn(Dialect dialect) => (Flags & (1 << (int)dialect)) != 0;

    /// <summary>Whether every dialect accepts the word.</summary>
    public bool IsInEveryDialect => (Flags & AllDialects) == AllDialects;

    /// <summary>Whether the word comes from this project's own lists rather than the source dictionaries.</summary>
    public bool IsAdded => (Flags & AddedBit) != 0;

    /// <summary>Whether the word is one of the rarer words (from the larger source dictionary only).</summary>
    public bool IsRare => (Flags & RareBit) != 0;

    /// <summary>Whether the word is valid but should not be suggested (offensive words).</summary>
    public bool NeverSuggest => (Flags & NoSuggestBit) != 0;
}

/// <summary>The built-in word list for all dialects, loaded from data embedded in the assembly.</summary>
internal sealed class Lexicon
{
    /// <summary>The data format version this code reads.</summary>
    public const byte FormatVersion = 1;

    private const string ResourceName = "FictionChecker.Data.lexicon.bin";

    private readonly Dictionary<string, WordInfo> _words;
    private readonly Dictionary<string, WordInfo>.AlternateLookup<ReadOnlySpan<char>> _bySpan;

    private Lexicon(Dictionary<string, WordInfo> words)
    {
        _words = words;
        _bySpan = words.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    /// <summary>Number of words.</summary>
    public int Count => _words.Count;

    /// <summary>Every word with its information, in no particular order.</summary>
    public IEnumerable<KeyValuePair<string, WordInfo>> Words => _words;

    /// <summary>Loads the lexicon embedded in this assembly.</summary>
    public static Lexicon LoadEmbedded()
    {
        using Stream stream = typeof(Lexicon).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The embedded lexicon is missing.");
        byte[] data = new byte[stream.Length];
        stream.ReadExactly(data);
        return Read(data);
    }

    /// <summary>Size in bytes of the embedded lexicon.</summary>
    public static long EmbeddedSize()
    {
        using Stream? stream = typeof(Lexicon).Assembly.GetManifestResourceStream(ResourceName);
        return stream?.Length ?? 0;
    }

    /// <summary>Reads lexicon data. The spelling of a word is exact: "London" and "london" are different entries.</summary>
    public static Lexicon Read(ReadOnlySpan<byte> data)
    {
        if (data.Length < 10 || !data[..4].SequenceEqual("FCLX"u8))
        {
            throw new InvalidDataException("Not a lexicon file.");
        }

        if (data[4] != FormatVersion)
        {
            throw new InvalidDataException($"Lexicon format {data[4]} is not supported; this library reads format {FormatVersion}.");
        }

        int count = BinaryPrimitives.ReadInt32LittleEndian(data[6..]);
        var words = new Dictionary<string, WordInfo>(count, StringComparer.Ordinal);
        Span<byte> buffer = stackalloc byte[255];
        int at = 10;
        for (int i = 0; i < count; i++)
        {
            int shared = data[at];
            int rest = data[at + 1];
            data.Slice(at + 2, rest).CopyTo(buffer[shared..]);
            at += 2 + rest;
            words.Add(Encoding.UTF8.GetString(buffer[..(shared + rest)]), new WordInfo(data[at], data[at + 1]));
            at += 2;
        }

        return new Lexicon(words);
    }

    /// <summary>Looks a word up exactly as written.</summary>
    public bool TryGet(ReadOnlySpan<char> word, out WordInfo info) => _bySpan.TryGetValue(word, out info);
}
