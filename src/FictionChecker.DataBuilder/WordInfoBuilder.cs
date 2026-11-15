using System.Buffers.Binary;
using System.Globalization;
using FictionChecker.Grammar;
using FictionChecker.Spelling;

namespace FictionChecker.DataBuilder;

/// <summary>
/// Builds the word info data: for each lexicon word, how likely it is to be each word type (from the tagged
/// Google Books counts), which word it is a form of (by spelling rules checked against those counts, plus a
/// hand-written list of irregular forms), the sound it starts with where spelling misleads (from the CMU
/// Pronouncing Dictionary) and whether it is counted or measured (from which words come before it in the
/// Google Books 2-grams). It also holds the tagger's table of which word type follows which.
/// </summary>
internal sealed class WordInfoBuilder
{
    private static readonly string[] TagNames = ["NOUN", "VERB", "ADJ", "ADV", "PRON", "DET", "ADP", "NUM", "CONJ", "PRT", ".", "X"];
    private static readonly HashSet<string> NotPastForms = new(StringComparer.Ordinal) { "heed", "weed", "need", "seed", "feed", "deed", "reed", "speed", "greed", "indeed" };
    private const int MaxCost = WordData.CostUnit * 24;

    private readonly Lexicon _lexicon;
    private readonly Dictionary<string, long[]> _tagged = new(StringComparer.Ordinal);
    private readonly List<(int Word, int Base, WordForms Forms)> _links = [];

    public WordInfoBuilder(Lexicon lexicon) => _lexicon = lexicon;

    /// <summary>Counts for the report printed after a build.</summary>
    public Dictionary<string, long> Stats { get; } = new(StringComparer.Ordinal);

    public byte[] Build(string unigramTable, string bigramDirectory, string closedDirectory, string cmudictPath, string irregularPath)
    {
        ReadTagged(unigramTable);
        (short[] typeCost, short[] transition) = ReadTransitions(bigramDirectory);
        (List<byte[]> profiles, ushort[] profileOf) = BuildProfiles();
        BuildLinks(irregularPath);
        List<int>[] sounds = BuildSounds(cmudictPath);
        List<int>[] counted = BuildCountability(bigramDirectory);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("FCWI"u8);
        writer.Write(WordData.FormatVersion);
        writer.Write(_lexicon.Count);
        writer.Write(_lexicon.Checksum);
        foreach (short cost in typeCost)
        {
            writer.Write(cost);
        }

        foreach (short cost in transition)
        {
            writer.Write(cost);
        }

        writer.Write(profiles.Count);
        foreach (byte[] profile in profiles)
        {
            writer.Write(profile);
        }

        foreach (ushort index in profileOf)
        {
            writer.Write(index);
        }

        _links.Sort((a, b) => a.Word != b.Word ? a.Word.CompareTo(b.Word) : a.Base.CompareTo(b.Base));
        writer.Write(_links.Count);
        int previous = 0;
        foreach ((int word, int baseWord, WordForms forms) in _links)
        {
            WriteVarint(writer, word - previous);
            int delta = baseWord - word;
            WriteVarint(writer, (delta << 1) ^ (delta >> 31));
            writer.Write((byte)forms);
            previous = word;
        }

        foreach (List<int> list in sounds.Take(3).Concat(counted))
        {
            list.Sort();
            writer.Write(list.Count);
            previous = 0;
            foreach (int index in list)
            {
                WriteVarint(writer, index - previous);
                previous = index;
            }
        }

        List<(string Word, short[] Costs)> after = ReadCostsAfter(closedDirectory);
        writer.Write(after.Count);
        foreach ((string word, short[] costs) in after)
        {
            byte[] name = System.Text.Encoding.UTF8.GetBytes(word);
            writer.Write((byte)name.Length);
            writer.Write(name);
            foreach (short cost in costs)
            {
                writer.Write(cost);
            }
        }

        // Words whose first sound cannot be told from their spelling and that the dictionary does not cover.
        sounds[3].Sort();
        writer.Write(sounds[3].Count);
        previous = 0;
        foreach (int index in sounds[3])
        {
            WriteVarint(writer, index - previous);
            previous = index;
        }

        Stats["sound: unknown"] = sounds[3].Count;
        Stats["function words with following-type costs"] = after.Count;
        Stats["profiles"] = profiles.Count;
        Stats["words with types"] = profileOf.Count(p => p != 0);
        Stats["form links"] = _links.Count;
        Stats["sound: consonant despite spelling"] = sounds[0].Count;
        Stats["sound: vowel despite spelling"] = sounds[1].Count;
        Stats["sound: either"] = sounds[2].Count;
        Stats["nouns measured"] = counted[0].Count;
        Stats["nouns counted and measured"] = counted[1].Count;
        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>The links found, for inspection.</summary>
    public IEnumerable<(string Word, string Base, WordForms Forms)> Links =>
        _links.Select(l => (_lexicon.WordAt(l.Word), _lexicon.WordAt(l.Base), l.Forms));

    private void ReadTagged(string table)
    {
        var tagIndex = TagNames.Select((name, i) => (name, i)).ToDictionary(t => t.name, t => t.i, StringComparer.Ordinal);
        foreach (string line in File.ReadLines(table))
        {
            int tab = line.IndexOf('\t', StringComparison.Ordinal);
            int underscore = line.LastIndexOf('_', tab);
            if (underscore <= 0 || !tagIndex.TryGetValue(line[(underscore + 1)..tab], out int tag))
            {
                continue;
            }

            string word = line[..underscore];
            if (_lexicon.IndexOf(word) < 0)
            {
                continue;
            }

            if (!_tagged.TryGetValue(word, out long[]? counts))
            {
                _tagged[word] = counts = new long[WordData.TypeCount];
            }

            counts[tag] += long.Parse(line.AsSpan(tab + 1), CultureInfo.InvariantCulture);
        }
    }

    private double Share(string word, WordType type)
    {
        if (!_tagged.TryGetValue(word, out long[]? counts))
        {
            return -1;
        }

        long total = counts.Sum();
        return total == 0 ? -1 : counts[(int)type] / (double)total;
    }

    // At least the given share of a word's uses, or nothing known about the word and the fallback allowed.
    private bool Is(string word, WordType type, double share, bool whenUnknown)
    {
        double actual = Share(word, type);
        return actual < 0 ? whenUnknown : actual >= share;
    }

    private (short[] TypeCost, short[] Transition) ReadTransitions(string directory)
    {
        var state = TagNames.Select((name, i) => ("_" + name + "_", i)).ToDictionary(t => t.Item1, t => t.i, StringComparer.Ordinal);
        state["_START_"] = WordData.StartState;
        state["_END_"] = WordData.EndState;
        var counts = new double[WordData.States, WordData.States];
        foreach (string file in Directory.EnumerateFiles(directory, "2-*.tsv"))
        {
            foreach (string line in File.ReadLines(file))
            {
                if (line.Length == 0 || line[0] != '_')
                {
                    continue;
                }

                string[] parts = line.Split('\t');
                if (parts.Length == 3 && state.TryGetValue(parts[0], out int from) && state.TryGetValue(parts[1], out int to))
                {
                    counts[from, to] += long.Parse(parts[2], CultureInfo.InvariantCulture);
                }
            }
        }

        var transition = new short[WordData.States * WordData.States];
        var typeTotals = new double[WordData.TypeCount];
        for (int from = 0; from < WordData.States; from++)
        {
            double row = 0;
            for (int to = 0; to < WordData.States; to++)
            {
                row += counts[from, to];
            }

            if (from < WordData.TypeCount)
            {
                typeTotals[from] = row;
            }

            for (int to = 0; to < WordData.States; to++)
            {
                transition[(from * WordData.States) + to] = Cost(row == 0 ? 0 : counts[from, to] / row);
            }

            Stats["transitions from " + (from < WordData.TypeCount ? TagNames[from] : from == WordData.StartState ? "START" : "END")] = (long)row;
        }

        double all = typeTotals.Sum();
        return (typeTotals.Select(t => Cost(t / all)).ToArray(), transition);
    }

    // Lines are "they TAB _VERB_ TAB count": how often each word type follows a function word.
    private static List<(string Word, short[] Costs)> ReadCostsAfter(string directory)
    {
        var state = TagNames.Select((name, i) => ("_" + name + "_", i)).ToDictionary(t => t.Item1, t => t.i, StringComparer.Ordinal);
        state["_END_"] = WordData.TypeCount;
        var counts = new SortedDictionary<string, double[]>(StringComparer.Ordinal);
        if (Directory.Exists(directory))
        {
            foreach (string file in Directory.EnumerateFiles(directory, "2-*.tsv"))
            {
                foreach (string line in File.ReadLines(file))
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length != 3 || !state.TryGetValue(parts[1], out int next))
                    {
                        continue;
                    }

                    string word = parts[0].ToLowerInvariant();
                    if (!counts.TryGetValue(word, out double[]? row))
                    {
                        counts[word] = row = new double[WordData.TypeCount + 1];
                    }

                    row[next] += long.Parse(parts[2], CultureInfo.InvariantCulture);
                }
            }
        }

        var result = new List<(string, short[])>();
        foreach ((string word, double[] row) in counts)
        {
            double total = row.Sum();
            if (total >= 100_000)
            {
                result.Add((word, row.Select(c => Cost(c / total)).ToArray()));
            }
        }

        return result;
    }

    private static short Cost(double probability) =>
        probability <= 0 ? (short)MaxCost : (short)Math.Min(MaxCost, Math.Round(-WordData.CostUnit * Math.Log2(probability)));

    private (List<byte[]> Profiles, ushort[] ProfileOf) BuildProfiles()
    {
        var profiles = new List<byte[]> { Enumerable.Repeat((byte)0xFF, 6).ToArray() };
        var ids = new Dictionary<string, ushort>(StringComparer.Ordinal);
        var profileOf = new ushort[_lexicon.Count];
        var nibbles = new int[WordData.TypeCount];
        for (int i = 0; i < _lexicon.Count; i++)
        {
            if (!_tagged.TryGetValue(_lexicon.WordAt(i), out long[]? counts))
            {
                continue;
            }

            long total = counts.Sum();
            bool any = false;
            for (int t = 0; t < WordData.TypeCount; t++)
            {
                double share = total == 0 ? 0 : counts[t] / (double)total;
                bool keep = share >= 0.003 && counts[t] >= 20;
                nibbles[t] = keep ? Math.Min(WordData.Never - 1, (int)Math.Round(-Math.Log2(share))) : WordData.Never;
                any |= keep;
            }

            if (!any)
            {
                continue;
            }

            var packed = new byte[6];
            for (int t = 0; t < WordData.TypeCount; t++)
            {
                packed[t / 2] |= (byte)(nibbles[t] << (t % 2 * 4));
            }

            string key = Convert.ToHexString(packed);
            if (!ids.TryGetValue(key, out ushort id))
            {
                ids[key] = id = checked((ushort)profiles.Count);
                profiles.Add(packed);
            }

            profileOf[i] = id;
        }

        return (profiles, profileOf);
    }

    private void AddLink(string word, string baseWord, WordForms forms)
    {
        int w = _lexicon.IndexOf(word);
        int b = _lexicon.IndexOf(baseWord);
        if (w < 0 || b < 0 || forms == WordForms.None)
        {
            return;
        }

        int existing = _links.FindIndex(l => l.Word == w && l.Base == b);
        if (existing >= 0)
        {
            _links[existing] = (w, b, _links[existing].Forms | forms);
        }
        else
        {
            _links.Add((w, b, forms));
        }
    }

    private void BuildLinks(string irregularPath)
    {
        var irregularVerbs = new HashSet<string>(StringComparer.Ordinal);
        var irregularForms = new HashSet<string>(StringComparer.Ordinal);
        var irregular = new List<(string Word, string Base, WordForms Forms)>();
        foreach (string line in LexiconBuilder.ReadList(irregularPath))
        {
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                continue;
            }

            WordForms[] kinds = parts[0] switch
            {
                "V" => [WordForms.VerbPast, WordForms.VerbPastParticiple],
                "N" => [WordForms.NounPlural],
                _ => [WordForms.Comparative, WordForms.Superlative],
            };
            if (parts[0] == "V")
            {
                irregularVerbs.Add(parts[1]);
            }

            for (int k = 0; k < kinds.Length && k + 2 < parts.Length; k++)
            {
                foreach (string form in parts[k + 2].Split('/'))
                {
                    irregular.Add((form, parts[1], kinds[k]));
                    irregularForms.Add(form);
                }
            }
        }

        // Found first by index so the linear search in AddLink stays cheap: collect, then merge once.
        var found = new Dictionary<(int, int), WordForms>();
        void Link(string word, string baseWord, WordForms forms)
        {
            int w = _lexicon.IndexOf(word);
            int b = _lexicon.IndexOf(baseWord);
            if (w >= 0 && b >= 0 && forms != WordForms.None)
            {
                found[(w, b)] = found.GetValueOrDefault((w, b)) | forms;
            }
        }

        foreach ((string word, string baseWord, WordForms forms) in irregular)
        {
            Link(word, baseWord, forms);
        }

        bool Known(string word) => word.Length >= 2 && _lexicon.IndexOf(word) >= 0;
        static bool IsVowel(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u';

        // "hop" doubles its last letter before an ending (hopping), so "hoping" belongs to "hope".
        static bool Doubles(string stem) =>
            stem.Length >= 3 && !IsVowel(stem[^1]) && stem[^1] is not ('w' or 'x' or 'y') && IsVowel(stem[^2]) && !IsVowel(stem[^3]);

        // A one-syllable word ending consonant-vowel-consonant always doubles: "can" gives "canned".
        static bool MustDouble(string stem) => Doubles(stem) && stem.Count(IsVowel) == 1;

        // "morning" is not a form of "morn", nor "thing" of anything: a form is not far commoner than its base.
        bool FarMoreCommon(string word, string stem) =>
            _tagged.TryGetValue(word, out long[]? w) && w.Sum() > 20 * (_tagged.TryGetValue(stem, out long[]? b) ? b.Sum() : 0);

        for (int i = 0; i < _lexicon.Count; i++)
        {
            string word = _lexicon.WordAt(i);
            if (word.Length < 4 || !word.All(c => c is >= 'a' and <= 'z'))
            {
                continue;
            }

            if (word.EndsWith('s') && !word.EndsWith("ss", StringComparison.Ordinal))
            {
                string? stem = null;
                if (word.EndsWith("ies", StringComparison.Ordinal) && Known(word[..^3] + "y"))
                {
                    stem = word[..^3] + "y";
                }
                else if (word.EndsWith("es", StringComparison.Ordinal) && Known(word[..^2]) && word[^3] is 's' or 'x' or 'z' or 'h' or 'o')
                {
                    stem = word[..^2];
                }
                else if (Known(word[..^1]) && word[^2] is not ('s' or 'x' or 'z'))
                {
                    stem = word[..^1];
                }

                if (stem is not null)
                {
                    WordForms forms = WordForms.None;
                    bool unknown = Share(word, WordType.Noun) < 0;
                    if (unknown ? Is(stem, WordType.Noun, 0.3, false) : Is(word, WordType.Noun, 0.05, false) && Is(stem, WordType.Noun, 0.05, false))
                    {
                        forms |= WordForms.NounPlural;
                    }

                    if (unknown ? Is(stem, WordType.Verb, 0.3, false) : Is(word, WordType.Verb, 0.02, false) && Is(stem, WordType.Verb, 0.02, false))
                    {
                        forms |= WordForms.VerbThirdPerson;
                    }

                    Link(word, stem, forms);
                }
            }

            foreach ((string ending, WordForms forms) in (ReadOnlySpan<(string, WordForms)>)[("ed", WordForms.VerbPast | WordForms.VerbPastParticiple), ("ing", WordForms.VerbGerund)])
            {
                if (!word.EndsWith(ending, StringComparison.Ordinal) || word.Length < ending.Length + 2 || (ending == "ed" && (irregularForms.Contains(word) || NotPastForms.Contains(word))))
                {
                    continue;
                }

                string cut = word[..^ending.Length];
                string? stem = null;
                bool cutIsVerb = Known(cut) && Is(cut, WordType.Verb, 0.02, false);
                if (cut.Length >= 3 && cut[^1] == cut[^2] && (cutIsVerb || (Known(cut) && cut[^1] is 'l' or 's' or 'f' or 'z')))
                {
                    stem = cut; // "called" is from "call", not "cal"
                }
                else if (cut.Length >= 3 && cut[^1] == cut[^2] && Known(cut[..^1]) && Doubles(cut[..^1]))
                {
                    stem = cut[..^1];
                }
                else if (ending == "ed" && cut.EndsWith('i') && Known(cut[..^1] + "y"))
                {
                    stem = cut[..^1] + "y";
                }
                else if (ending == "ing" && cut.EndsWith('y') && Known(cut[..^1] + "ie"))
                {
                    stem = cut[..^1] + "ie";
                }
                else if (Known(cut + "e") && (MustDouble(cut) || (Is(cut + "e", WordType.Verb, 0.02, false) && (!cutIsVerb || Doubles(cut)))))
                {
                    stem = cut + "e"; // "caned" cannot be from "can", which would give "canned"
                }
                else if (Known(cut) && !MustDouble(cut) && (cut.Length >= 3 || cut is "go" or "do" or "be"))
                {
                    stem = cut;
                }

                if (stem is null || !Is(stem, WordType.Verb, 0.02, false) || (ending == "ed" && irregularVerbs.Contains(stem)) || FarMoreCommon(word, stem))
                {
                    continue;
                }

                if (Is(word, WordType.Verb, 0.02, true) || Is(word, WordType.Adjective, 0.02, false) || Is(word, WordType.Noun, 0.5, false))
                {
                    Link(word, stem, forms);
                }
            }

            foreach ((string ending, WordForms forms) in (ReadOnlySpan<(string, WordForms)>)[("er", WordForms.Comparative), ("est", WordForms.Superlative)])
            {
                if (!word.EndsWith(ending, StringComparison.Ordinal) || word.Length < ending.Length + 3 || irregularForms.Contains(word))
                {
                    continue;
                }

                string cut = word[..^ending.Length];
                string? stem =
                    cut.Length >= 3 && cut[^1] == cut[^2] && Known(cut[..^1]) && Doubles(cut[..^1]) ? cut[..^1]
                    : cut.EndsWith('i') && Known(cut[..^1] + "y") ? cut[..^1] + "y"
                    : Known(cut + "e") ? cut + "e"
                    : Known(cut) ? cut
                    : null;
                if (stem is not null && !FarMoreCommon(word, stem) && Is(stem, WordType.Adjective, 0.3, false)
                    && (Is(word, WordType.Adjective, 0.05, true) || Is(word, WordType.Adverb, 0.05, false)))
                {
                    Link(word, stem, forms);
                }
            }
        }

        _links.AddRange(found.Select(kv => (kv.Key.Item1, kv.Key.Item2, kv.Value)));
    }

    private List<int>[] BuildSounds(string cmudictPath)
    {
        // Word to (some pronunciation starts with a vowel sound, some with a consonant sound).
        var starts = new Dictionary<string, (bool Vowel, bool Consonant)>(StringComparer.Ordinal);
        foreach (string raw in File.ReadLines(cmudictPath))
        {
            int hash = raw.IndexOf('#', StringComparison.Ordinal);
            string line = hash < 0 ? raw : raw[..hash];
            int space = line.IndexOf(' ', StringComparison.Ordinal);
            if (space <= 0 || space + 1 >= line.Length)
            {
                continue;
            }

            string word = line[..space];
            int paren = word.IndexOf('(', StringComparison.Ordinal);
            if (paren > 0)
            {
                word = word[..paren];
            }

            bool vowel = line[space + 1] is 'A' or 'E' or 'I' or 'O' or 'U';
            (bool v, bool c) = starts.GetValueOrDefault(word);
            starts[word] = (v || vowel, c || !vowel);
        }

        List<int>[] lists = [[], [], [], []];
        for (int i = 0; i < _lexicon.Count; i++)
        {
            string word = _lexicon.WordAt(i);
            if (!char.IsAsciiLetter(word[0]))
            {
                continue;
            }

            string lower = word.ToLowerInvariant();
            if (!starts.TryGetValue(lower, out (bool Vowel, bool Consonant) sound))
            {
                // Not in the pronouncing dictionary. For most letters the spelling settles it (the few words
                // with a silent h are all in the dictionary); for these it does not: "uncle" and "unit",
                // "only" and "one", "eel" and "eulogy".
                if (lower[0] == 'u' || lower.StartsWith("eu", StringComparison.Ordinal) || lower.StartsWith("ew", StringComparison.Ordinal) || lower.StartsWith("one", StringComparison.Ordinal) || lower.StartsWith("once", StringComparison.Ordinal))
                {
                    lists[3].Add(i);
                }

                continue;
            }

            bool spelledVowel = char.ToLowerInvariant(word[0]) is 'a' or 'e' or 'i' or 'o' or 'u';
            if (sound.Vowel && sound.Consonant)
            {
                lists[2].Add(i);
            }
            else if (sound.Vowel != spelledVowel)
            {
                lists[sound.Vowel ? 1 : 0].Add(i);
            }
        }

        return lists;
    }

    private List<int>[] BuildCountability(string bigramDirectory)
    {
        var plurals = new Dictionary<int, int>();
        foreach ((int word, int baseWord, WordForms forms) in _links)
        {
            if ((forms & WordForms.NounPlural) != 0 && word != baseWord)
            {
                plurals[word] = baseWord;
            }
        }

        var measured = new Dictionary<int, long>();
        var counted = new Dictionary<int, long>();
        foreach (string file in Directory.EnumerateFiles(bigramDirectory, "2-*.tsv"))
        {
            foreach (string line in File.ReadLines(file))
            {
                string[] parts = line.Split('\t');
                if (parts.Length != 3)
                {
                    continue;
                }

                string first = parts[0].ToLowerInvariant();
                bool mass = first is "much" or "less";
                bool count = first is "many" or "few" or "fewer" or "several" or "these" or "those";
                int index = mass || count ? _lexicon.IndexOf(parts[1]) : -1;
                if (index < 0)
                {
                    continue;
                }

                long n = long.Parse(parts[2], CultureInfo.InvariantCulture);
                if (mass)
                {
                    measured[index] = measured.GetValueOrDefault(index) + n;
                }
                else if (plurals.TryGetValue(index, out int singular))
                {
                    counted[singular] = counted.GetValueOrDefault(singular) + n;
                }
            }
        }

        List<int>[] lists = [[], []];
        foreach ((int index, long mass) in measured)
        {
            string word = _lexicon.WordAt(index);
            if (mass < 200 || !Is(word, WordType.Noun, 0.3, false) || plurals.ContainsKey(index))
            {
                continue;
            }

            // The share of the evidence that treats the noun as measured ("much water") and not as counted
            // ("many waters"). A stray "less chair" beside thousands of "many chairs" changes nothing.
            double share = mass / (double)(mass + counted.GetValueOrDefault(index));
            if (share >= 0.9)
            {
                lists[0].Add(index);
            }
            else if (share >= 0.15)
            {
                lists[1].Add(index);
            }
        }

        return lists;
    }

    private static void WriteVarint(BinaryWriter writer, int value)
    {
        uint v = (uint)value;
        while (v >= 0x80)
        {
            writer.Write((byte)(v | 0x80));
            v >>= 7;
        }

        writer.Write((byte)v);
    }
}
