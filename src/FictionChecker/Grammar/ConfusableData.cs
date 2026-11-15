using System.Buffers.Binary;
using System.Text;
using FictionChecker.Spelling;

namespace FictionChecker.Grammar;

/// <summary>
/// How often each confusable word is seen beside other words, from the Google Books fiction 2-grams. For
/// every group of confusable words it holds, per member, a count for each neighbouring word on the left and
/// on the right, and counts by the neighbour's word type for neighbours with no count of their own.
///
/// Counts are stored as logarithms (base 2, in steps of 1/<see cref="Unit"/>), so comparing two words is a
/// subtraction. A stored 0 means "not seen often enough to be counted".
/// </summary>
internal sealed class ConfusableData
{
    /// <summary>Version of the data layout.</summary>
    public const byte FormatVersion = 1;

    /// <summary>Stored values are log2(count) times this.</summary>
    public const int Unit = 7;

    /// <summary>Word types a neighbour can have: the twelve types, then sentence start and sentence end.</summary>
    public const int Tags = 14;

    /// <summary>Tag number for "no word before: the sentence starts here".</summary>
    public const int StartTag = 12;

    /// <summary>Tag number for "no word after: the sentence ends here".</summary>
    public const int EndTag = 13;

    /// <summary>The value used for a pair never seen often enough to be stored: a count of about 20.</summary>
    public const int Floor = 30;

    private const string ResourceName = "FictionChecker.Data.confusables.bin";

    private readonly Group[] _groups;
    private readonly Dictionary<string, (int Group, int Member)> _byWord = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (int Group, int Member)>.AlternateLookup<ReadOnlySpan<char>> _bySpan;
    private readonly Dictionary<string, int> _specials = new(StringComparer.Ordinal);

    private ConfusableData(Lexicon lexicon, ReadOnlySpan<byte> data)
    {
        if (data.Length < 16 || !data[..4].SequenceEqual("FCCW"u8))
        {
            throw new InvalidDataException("Not a confusable-word file.");
        }

        if (data[4] != FormatVersion)
        {
            throw new InvalidDataException($"Confusable-word format {data[4]} is not supported; this library reads format {FormatVersion}.");
        }

        int at = 5;
        int count = ReadInt(data, ref at);
        uint checksum = (uint)ReadInt(data, ref at);
        if (count != lexicon.Count || checksum != lexicon.Checksum)
        {
            throw new InvalidDataException("The confusable-word data was built for a different word list. Rebuild it after rebuilding the lexicon.");
        }

        for (int i = 0; i < ConfusableSets.Specials.Length; i++)
        {
            _specials[ConfusableSets.Specials[i]] = lexicon.Count + i;
        }

        _groups = new Group[data[at++]];
        for (int g = 0; g < _groups.Length; g++)
        {
            int members = data[at++];
            var group = new Group(members);
            for (int m = 0; m < members; m++)
            {
                int length = data[at++];
                group.Members[m] = Encoding.UTF8.GetString(data.Slice(at, length));
                at += length;
                group.Count[m] = data[at++];
                _byWord[group.Members[m]] = (g, m);
            }

            group.LeftTags = data.Slice(at, members * Tags).ToArray();
            at += members * Tags;
            group.RightTags = data.Slice(at, members * Tags).ToArray();
            at += members * Tags;
            (group.LeftIds, group.LeftValues) = ReadTable(data, ref at, members);
            (group.RightIds, group.RightValues) = ReadTable(data, ref at, members);
            _groups[g] = group;
        }

        _bySpan = _byWord.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    /// <summary>Number of groups.</summary>
    public int GroupCount => _groups.Length;

    /// <summary>Number of stored neighbour entries, over every group and both sides.</summary>
    public int EntryCount => _groups.Sum(g => g.LeftIds.Length + g.RightIds.Length);

    /// <summary>Loads the data embedded in this assembly.</summary>
    public static ConfusableData LoadEmbedded(Lexicon lexicon)
    {
        using Stream stream = typeof(ConfusableData).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The embedded confusable-word data is missing.");
        byte[] data = new byte[stream.Length];
        stream.ReadExactly(data);
        return new ConfusableData(lexicon, data);
    }

    /// <summary>Reads data built for the given lexicon.</summary>
    public static ConfusableData Read(Lexicon lexicon, ReadOnlySpan<byte> data) => new(lexicon, data);

    /// <summary>Size in bytes of the embedded data.</summary>
    public static long EmbeddedSize()
    {
        using Stream? stream = typeof(ConfusableData).Assembly.GetManifestResourceStream(ResourceName);
        return stream?.Length ?? 0;
    }

    /// <summary>Finds the group a lowercase word belongs to.</summary>
    public bool TryFind(ReadOnlySpan<char> lower, out int group, out int member)
    {
        bool found = _bySpan.TryGetValue(lower, out (int Group, int Member) where);
        (group, member) = where;
        return found;
    }

    /// <summary>The words of a group.</summary>
    public string[] Members(int group) => _groups[group].Members;

    /// <summary>How common each word of the group is overall.</summary>
    public ReadOnlySpan<byte> Counts(int group) => _groups[group].Count;

    /// <summary>The number a punctuation mark or contraction tail is stored under, or -1.</summary>
    public int SpecialId(string token) => _specials.GetValueOrDefault(token, -1);

    /// <summary>
    /// Fills in, for each word of the group, how often it is seen with this neighbour on the given side.
    /// The neighbour's own counts are used when it has any; otherwise the counts for its word type. Returns
    /// whether the neighbour's own counts were used.
    /// </summary>
    public bool Beside(int group, bool left, int neighbourId, int tag, Span<int> values)
    {
        Group g = _groups[group];
        int members = g.Members.Length;
        int[] ids = left ? g.LeftIds : g.RightIds;
        int found = neighbourId >= 0 ? Array.BinarySearch(ids, neighbourId) : -1;
        if (found >= 0)
        {
            byte[] stored = left ? g.LeftValues : g.RightValues;
            for (int m = 0; m < members; m++)
            {
                int value = stored[(found * members) + m];
                values[m] = value == 0 ? Floor : value;
            }

            return true;
        }

        byte[] tags = left ? g.LeftTags : g.RightTags;
        for (int m = 0; m < members; m++)
        {
            int value = tags[(m * Tags) + tag];
            values[m] = value == 0 ? Floor : value;
        }

        return false;
    }

    private static (int[] Ids, byte[] Values) ReadTable(ReadOnlySpan<byte> data, ref int at, int members)
    {
        int count = ReadInt(data, ref at);
        int[] ids = new int[count];
        int id = 0;
        for (int i = 0; i < count; i++)
        {
            id += ReadVarint(data, ref at);
            ids[i] = id;
        }

        byte[] values = data.Slice(at, count * members).ToArray();
        at += count * members;
        return (ids, values);
    }

    private static int ReadInt(ReadOnlySpan<byte> data, ref int at)
    {
        int value = BinaryPrimitives.ReadInt32LittleEndian(data[at..]);
        at += 4;
        return value;
    }

    private static int ReadVarint(ReadOnlySpan<byte> data, ref int at)
    {
        int value = 0;
        int shift = 0;
        while (true)
        {
            byte b = data[at++];
            value |= (b & 0x7F) << shift;
            if (b < 0x80)
            {
                return value;
            }

            shift += 7;
        }
    }

    private sealed class Group(int members)
    {
        public string[] Members { get; } = new string[members];

        public byte[] Count { get; } = new byte[members];

        public byte[] LeftTags { get; set; } = [];

        public byte[] RightTags { get; set; } = [];

        public int[] LeftIds { get; set; } = [];

        public byte[] LeftValues { get; set; } = [];

        public int[] RightIds { get; set; } = [];

        public byte[] RightValues { get; set; } = [];
    }
}
