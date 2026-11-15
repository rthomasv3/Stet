using System.Buffers.Binary;
using System.Reflection;
using System.Text;

namespace Stet.Spelling;

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

    private const string ResourceName = "Stet.Data.lexicon.bin";

    private readonly string[] _words;
    private readonly WordInfo[] _infos;
    private readonly Dictionary<string, int> _index;
    private readonly Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> _bySpan;

    private Lexicon(string[] words, WordInfo[] infos)
    {
        _words = words;
        _infos = infos;
        _index = new Dictionary<string, int>(words.Length, StringComparer.Ordinal);
        for (int i = 0; i < words.Length; i++)
        {
            _index.Add(words[i], i);
        }

        _bySpan = _index.GetAlternateLookup<ReadOnlySpan<char>>();
        Search = new SuggestionIndex(words);
    }

    /// <summary>A fingerprint of the data the lexicon was read from.</summary>
    public uint Checksum { get; private init; }

    /// <summary>Number of words.</summary>
    public int Count => _words.Length;

    /// <summary>Every word with its information, in sorted order.</summary>
    public IEnumerable<KeyValuePair<string, WordInfo>> Words =>
        _words.Select((word, i) => new KeyValuePair<string, WordInfo>(word, _infos[i]));

    /// <summary>Finds words within a few edits of a misspelling.</summary>
    public SuggestionIndex Search { get; }

    /// <summary>The information for the word at an index reported by <see cref="Search"/>.</summary>
    public WordInfo InfoAt(int index) => _infos[index];

    /// <summary>The word at an index reported by <see cref="Search"/>.</summary>
    public string WordAt(int index) => _words[index];

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
        var words = new string[count];
        var infos = new WordInfo[count];
        Span<byte> buffer = stackalloc byte[255];
        int at = 10;
        for (int i = 0; i < count; i++)
        {
            int shared = data[at];
            int rest = data[at + 1];
            data.Slice(at + 2, rest).CopyTo(buffer[shared..]);
            at += 2 + rest;
            words[i] = Encoding.UTF8.GetString(buffer[..(shared + rest)]);
            infos[i] = new WordInfo(data[at], data[at + 1]);
            at += 2;
        }

        // A fingerprint of the words, so data built to run parallel to them can tell it still fits.
        uint checksum = 2166136261;
        foreach (byte b in data)
        {
            checksum = (checksum ^ b) * 16777619;
        }

        return new Lexicon(words, infos) { Checksum = checksum };
    }

    /// <summary>The position of a word written exactly so, or -1.</summary>
    public int IndexOf(ReadOnlySpan<char> word) => _bySpan.TryGetValue(word, out int index) ? index : -1;

    /// <summary>Looks a word up exactly as written.</summary>
    public bool TryGet(ReadOnlySpan<char> word, out WordInfo info)
    {
        if (_bySpan.TryGetValue(word, out int index))
        {
            info = _infos[index];
            return true;
        }

        info = default;
        return false;
    }
}
