using System.Buffers.Binary;
using Stet.Spelling;

namespace Stet.Grammar;

/// <summary>
/// What is known about each dictionary word beyond its spelling: how likely it is to be each word type, the
/// words it is a form of, the sound it starts with and whether it is counted. It also holds the tagger's
/// statistics. Entries run parallel to the lexicon's words.
/// </summary>
internal sealed class WordData
{
    /// <summary>The data format version this code reads.</summary>
    public const byte FormatVersion = 1;

    /// <summary>Word types, plus the start and end of a sentence, as states of the tagger.</summary>
    public const int States = 14;
    public const int TypeCount = 12;
    public const int StartState = 12;
    public const int EndState = 13;

    /// <summary>Costs are in sixteenths of a bit: 16 means half as likely.</summary>
    public const int CostUnit = 16;

    /// <summary>The cost given to a word type a word is never seen as.</summary>
    public const int Never = 15;

    private const string ResourceName = "Stet.Data.wordinfo.bin";

    private readonly Lexicon _lexicon;
    private readonly byte[] _profiles;
    private readonly ushort[] _profileOf;
    private readonly short[] _typeCost;
    private readonly short[] _transition;
    private readonly WordForms[] _forms;
    private readonly int[] _linkStart;
    private readonly (int Base, WordForms Forms)[] _links;
    private readonly Dictionary<int, StartSound> _sounds = [];
    private readonly Dictionary<int, Countability> _countability = [];
    private readonly Dictionary<string, short[]> _after = new(StringComparer.Ordinal);
    private readonly Dictionary<string, short[]>.AlternateLookup<ReadOnlySpan<char>> _afterBySpan;
    private Dictionary<(int Base, WordForms Form), int>? _formsOf;

    private WordData(Lexicon lexicon, ReadOnlySpan<byte> data)
    {
        _lexicon = lexicon;
        if (data.Length < 16 || !data[..4].SequenceEqual("FCWI"u8))
        {
            throw new InvalidDataException("Not a word info file.");
        }

        if (data[4] != FormatVersion)
        {
            throw new InvalidDataException($"Word info format {data[4]} is not supported; this library reads format {FormatVersion}.");
        }

        int at = 5;
        int count = ReadInt(data, ref at);
        uint checksum = (uint)ReadInt(data, ref at);
        if (count != lexicon.Count || checksum != lexicon.Checksum)
        {
            throw new InvalidDataException("The word info data was built for a different word list. Rebuild it after rebuilding the lexicon.");
        }

        _typeCost = new short[TypeCount];
        for (int i = 0; i < TypeCount; i++)
        {
            _typeCost[i] = ReadShort(data, ref at);
        }

        _transition = new short[States * States];
        for (int i = 0; i < _transition.Length; i++)
        {
            _transition[i] = ReadShort(data, ref at);
        }

        int profileCount = ReadInt(data, ref at);
        _profiles = data.Slice(at, profileCount * 6).ToArray();
        at += profileCount * 6;
        _profileOf = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            _profileOf[i] = BinaryPrimitives.ReadUInt16LittleEndian(data[at..]);
            at += 2;
        }

        // Links from a word to the word it is a form of, in word order.
        int linkCount = ReadInt(data, ref at);
        _links = new (int, WordForms)[linkCount];
        _forms = new WordForms[count];
        _linkStart = new int[count + 1];
        int word = 0;
        var owners = new int[linkCount];
        for (int i = 0; i < linkCount; i++)
        {
            word += ReadVarint(data, ref at);
            int zigzag = ReadVarint(data, ref at);
            int baseWord = word + ((zigzag >> 1) ^ -(zigzag & 1));
            var forms = (WordForms)data[at++];
            _links[i] = (baseWord, forms);
            owners[i] = word;
            _forms[word] |= forms;
            if ((forms & (WordForms.VerbThirdPerson | WordForms.VerbPast | WordForms.VerbPastParticiple | WordForms.VerbGerund)) != 0)
            {
                _forms[baseWord] |= WordForms.VerbBase;
            }
        }

        int link = 0;
        for (int i = 0; i < count; i++)
        {
            _linkStart[i] = link;
            while (link < linkCount && owners[link] == i)
            {
                link++;
            }
        }

        _linkStart[count] = linkCount;

        foreach (StartSound sound in (ReadOnlySpan<StartSound>)[StartSound.Consonant, StartSound.Vowel, StartSound.Either])
        {
            foreach (int index in ReadIndexList(data, ref at))
            {
                _sounds[index] = sound;
            }
        }

        foreach (Countability kind in (ReadOnlySpan<Countability>)[Countability.Measured, Countability.Both])
        {
            foreach (int index in ReadIndexList(data, ref at))
            {
                _countability[index] = kind;
            }
        }

        // For each function word, the cost of each word type (and of the sentence ending) coming next.
        int afterCount = ReadInt(data, ref at);
        for (int i = 0; i < afterCount; i++)
        {
            int length = data[at++];
            string key = System.Text.Encoding.UTF8.GetString(data.Slice(at, length));
            at += length;
            short[] costs = new short[TypeCount + 1];
            for (int t = 0; t < costs.Length; t++)
            {
                costs[t] = ReadShort(data, ref at);
            }

            _after[key] = costs;
        }

        _afterBySpan = _after.GetAlternateLookup<ReadOnlySpan<char>>();

        foreach (int index in ReadIndexList(data, ref at))
        {
            _sounds[index] = StartSound.Unknown;
        }
    }

    /// <summary>Loads the data embedded in this assembly.</summary>
    public static WordData LoadEmbedded(Lexicon lexicon)
    {
        using Stream stream = typeof(WordData).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The embedded word info is missing.");
        byte[] data = new byte[stream.Length];
        stream.ReadExactly(data);
        return new WordData(lexicon, data);
    }

    /// <summary>Reads word info data built for the given lexicon.</summary>
    public static WordData Read(Lexicon lexicon, ReadOnlySpan<byte> data) => new(lexicon, data);

    /// <summary>Size in bytes of the embedded data.</summary>
    public static long EmbeddedSize()
    {
        using Stream? stream = typeof(WordData).Assembly.GetManifestResourceStream(ResourceName);
        return stream?.Length ?? 0;
    }

    /// <summary>The lexicon the entries run parallel to.</summary>
    public Lexicon Lexicon => _lexicon;

    /// <summary>
    /// How unlikely a word is to be a word type, in whole bits (0 = nearly always, <see cref="Never"/> = never
    /// seen). False when nothing is known about the word's types.
    /// </summary>
    public bool TryGetTypeCosts(int wordIndex, Span<byte> costs)
    {
        int profile = _profileOf[wordIndex];
        if (profile == 0)
        {
            return false;
        }

        ReadOnlySpan<byte> packed = _profiles.AsSpan(profile * 6, 6);
        for (int i = 0; i < TypeCount; i++)
        {
            costs[i] = (byte)((packed[i / 2] >> (i % 2 * 4)) & 15);
        }

        return true;
    }

    /// <summary>The cost of a word type on its own: rarer types cost more.</summary>
    public int TypeCost(int type) => _typeCost[type];

    /// <summary>The cost of one state following another.</summary>
    public int TransitionCost(int from, int to) => _transition[(from * States) + to];

    /// <summary>
    /// For a function word (lowercase), the cost of each word type coming next, with the cost of the sentence
    /// ending last. Null for other words. These are sharper than the costs by type alone: a noun often
    /// follows "her" and almost never follows "they", though both are pronouns.
    /// </summary>
    public short[]? CostsAfter(ReadOnlySpan<char> lowercaseWord) => _afterBySpan.TryGetValue(lowercaseWord, out short[]? costs) ? costs : null;

    /// <summary>Number of function words with their own following-type costs.</summary>
    public int WordsWithCostsAfter => _after.Count;

    /// <summary>The ways a word is formed from other words.</summary>
    public WordForms FormsOf(int wordIndex) => _forms[wordIndex];

    /// <summary>The words a word is a form of, with how.</summary>
    public ReadOnlySpan<(int Base, WordForms Forms)> BasesOf(int wordIndex) =>
        _links.AsSpan(_linkStart[wordIndex], _linkStart[wordIndex + 1] - _linkStart[wordIndex]);

    /// <summary>The form of a base word, such as the past of "walk", or -1.</summary>
    public int FormOf(int baseIndex, WordForms form)
    {
        if (_formsOf is null)
        {
            var map = new Dictionary<(int, WordForms), int>();
            for (int word = 0; word < _forms.Length; word++)
            {
                foreach ((int baseWord, WordForms forms) in BasesOf(word))
                {
                    for (int bit = 1; bit < 128; bit <<= 1)
                    {
                        if (((int)forms & bit) != 0)
                        {
                            map.TryAdd((baseWord, (WordForms)bit), word);
                        }
                    }
                }
            }

            _formsOf = map;
        }

        return _formsOf.TryGetValue((baseIndex, form), out int found) ? found : -1;
    }

    /// <summary>The sound a word starts with, where its spelling would mislead.</summary>
    public StartSound SoundOf(int wordIndex) => _sounds.GetValueOrDefault(wordIndex);

    /// <summary>Whether a noun is counted or measured.</summary>
    public Countability CountabilityOf(int wordIndex) => _countability.GetValueOrDefault(wordIndex);

    private static List<int> ReadIndexList(ReadOnlySpan<byte> data, ref int at)
    {
        int count = ReadInt(data, ref at);
        var list = new List<int>(count);
        int index = 0;
        for (int i = 0; i < count; i++)
        {
            index += ReadVarint(data, ref at);
            list.Add(index);
        }

        return list;
    }

    private static int ReadInt(ReadOnlySpan<byte> data, ref int at)
    {
        int value = BinaryPrimitives.ReadInt32LittleEndian(data[at..]);
        at += 4;
        return value;
    }

    private static short ReadShort(ReadOnlySpan<byte> data, ref int at)
    {
        short value = BinaryPrimitives.ReadInt16LittleEndian(data[at..]);
        at += 2;
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
}
