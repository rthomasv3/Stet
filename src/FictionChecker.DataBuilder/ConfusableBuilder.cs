using System.Globalization;
using System.Text;
using FictionChecker.Grammar;
using FictionChecker.Spelling;

namespace FictionChecker.DataBuilder;

/// <summary>
/// Builds confusables.bin: for each group of confusable words, how often each member is seen beside other
/// words, from the 2-gram neighbours kept by <see cref="BigramExtractor"/>.
///
/// A contraction is two tokens in the Google Books data ("they 're"), so its left neighbours are those of
/// its first part and its right neighbours those of its tail, scaled to how common the contraction is. For
/// "it's" and "who's" the tail's neighbours come from the 's tagged as a verb, so that "John's hat" does
/// not count as evidence for "it's hat".
/// </summary>
internal static class ConfusableBuilder
{
    private static readonly string[] TagNames = ["_NOUN_", "_VERB_", "_ADJ_", "_ADV_", "_PRON_", "_DET_", "_ADP_", "_NUM_", "_CONJ_", "_PRT_", "_._", "_X_", "_START_", "_END_"];

    public static byte[] Build(Lexicon lexicon, string bigramDir, string taggedDir, long minTotal, List<(string Name, long Value)> stats)
    {
        var members = new List<Member>();
        string[][] groups = ConfusableSets.Families.SelectMany(f => f.Groups).ToArray();
        for (int g = 0; g < groups.Length; g++)
        {
            foreach (string word in groups[g])
            {
                int apostrophe = word.IndexOf('\'');
                members.Add(apostrophe < 0
                    ? new Member(g, word, word, word)
                    : new Member(g, word, word[..apostrophe], word[apostrophe..] == "'s" ? "'s_VERB" : word[apostrophe..]));
            }
        }

        ILookup<string, Member> byLeftToken = members.ToLookup(m => m.LeftToken, StringComparer.Ordinal);
        ILookup<string, Member> byRightToken = members.ToLookup(m => m.RightToken, StringComparer.Ordinal);
        var specials = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < ConfusableSets.Specials.Length; i++)
        {
            specials[ConfusableSets.Specials[i]] = lexicon.Count + i;
        }

        var tags = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < TagNames.Length; i++)
        {
            tags[TagNames[i]] = i;
        }

        foreach (string file in Directory.EnumerateFiles(bigramDir, "*.tsv").Concat(Directory.EnumerateFiles(taggedDir, "*.tsv")))
        {
            foreach (string line in File.ReadLines(file))
            {
                string[] parts = line.Split('\t');
                if (parts.Length != 3)
                {
                    continue;
                }

                long count = long.Parse(parts[2], CultureInfo.InvariantCulture);
                string first = parts[0].Contains('_') ? parts[0] : parts[0].ToLowerInvariant();
                string second = parts[1].Contains('_') ? parts[1] : parts[1].ToLowerInvariant();

                // The second word is a member: the first is its left neighbour.
                foreach (Member member in byLeftToken[second])
                {
                    Add(member.Left, member.LeftTags, parts[0], count);
                }

                foreach (Member member in byRightToken[first])
                {
                    Add(member.Right, member.RightTags, parts[1], count);
                }

                // "they 're": how common the contraction itself is.
                foreach (Member member in members)
                {
                    if (member.IsContraction && first == member.LeftToken && second == member.Word[member.LeftToken.Length..])
                    {
                        member.PairCount += count;
                    }
                }
            }
        }

        void Add(Dictionary<int, long> words, long[] tagCounts, string token, long count)
        {
            if (tags.TryGetValue(token, out int tag))
            {
                tagCounts[tag] += count;
                return;
            }

            if (token.Contains('_'))
            {
                return;
            }

            string lower = token.ToLowerInvariant();
            int id = specials.TryGetValue(lower, out int special) ? special : lexicon.IndexOf(token);
            if (id < 0)
            {
                id = lexicon.IndexOf(lower);
            }

            if (id >= 0)
            {
                words[id] = words.GetValueOrDefault(id) + count;
            }
        }

        // Scale each member's counts so that they are counts of the member itself.
        foreach (Member member in members)
        {
            long leftTotal = member.LeftTags.Sum();
            long rightTotal = member.RightTags.Sum();
            if (rightTotal == 0)
            {
                rightTotal = member.Right.Values.Sum();
            }

            member.Count = member.IsContraction ? member.PairCount : leftTotal;
            member.LeftScale = member.IsContraction && leftTotal > 0 ? (double)member.Count / leftTotal : 1;
            member.RightScale = member.IsContraction && rightTotal > 0 ? (double)member.Count / rightTotal : 1;
            stats.Add(($"{member.Word}: count", member.Count));
            stats.Add(($"{member.Word}: left neighbours", member.Left.Count));
            stats.Add(($"{member.Word}: right neighbours", member.Right.Count));
            if (member.Count == 0 || member.LeftTags.Sum() == 0 || rightTotal == 0)
            {
                throw new InvalidDataException($"No counts for '{member.Word}'. Run the 'bigrams' and 'bigrams-tagged' commands first.");
            }
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        writer.Write("FCCW"u8);
        writer.Write(ConfusableData.FormatVersion);
        writer.Write(lexicon.Count);
        writer.Write(lexicon.Checksum);
        writer.Write((byte)groups.Length);
        long entries = 0;
        for (int g = 0; g < groups.Length; g++)
        {
            List<Member> group = members.Where(m => m.Group == g).ToList();
            writer.Write((byte)group.Count);
            foreach (Member member in group)
            {
                byte[] name = Encoding.UTF8.GetBytes(member.Word);
                writer.Write((byte)name.Length);
                writer.Write(name);
                writer.Write(Quantize(member.Count));
            }

            foreach (Member member in group)
            {
                writer.Write(member.LeftTags.Select(c => Quantize(c * member.LeftScale)).ToArray());
            }

            foreach (Member member in group)
            {
                writer.Write(member.RightTags.Select(c => Quantize(c * member.RightScale)).ToArray());
            }

            entries += WriteTable(writer, group, left: true, minTotal);
            entries += WriteTable(writer, group, left: false, minTotal);
        }

        stats.Add(("neighbour entries", entries));
        writer.Flush();
        return stream.ToArray();
    }

    private static int WriteTable(BinaryWriter writer, List<Member> group, bool left, long minTotal)
    {
        var ids = new SortedSet<int>();
        foreach (Member member in group)
        {
            ids.UnionWith((left ? member.Left : member.Right).Keys);
        }

        var kept = new List<int>();
        var values = new List<byte>();
        Span<byte> row = stackalloc byte[group.Count];
        foreach (int id in ids)
        {
            double total = 0;
            for (int m = 0; m < group.Count; m++)
            {
                Member member = group[m];
                double scaled = (left ? member.Left : member.Right).GetValueOrDefault(id) * (left ? member.LeftScale : member.RightScale);
                total += scaled;
                row[m] = Quantize(scaled);
            }

            if (total >= minTotal && row.IndexOfAnyExcept((byte)0) >= 0)
            {
                kept.Add(id);
                values.AddRange(row);
            }
        }

        writer.Write(kept.Count);
        int previous = 0;
        foreach (int id in kept)
        {
            int delta = id - previous;
            while (delta >= 0x80)
            {
                writer.Write((byte)(delta | 0x80));
                delta >>= 7;
            }

            writer.Write((byte)delta);
            previous = id;
        }

        writer.Write(values.ToArray());
        return kept.Count;
    }

    // log2(count) in steps of 1/Unit; 0 for anything under one.
    private static byte Quantize(double count) =>
        count < 1 ? (byte)0 : (byte)Math.Clamp((int)Math.Round(Math.Log2(count) * ConfusableData.Unit), 1, 255);

    private sealed class Member(int group, string word, string leftToken, string rightToken)
    {
        public int Group { get; } = group;

        public string Word { get; } = word;

        public string LeftToken { get; } = leftToken;

        public string RightToken { get; } = rightToken;

        public bool IsContraction => LeftToken != RightToken;

        public Dictionary<int, long> Left { get; } = [];

        public Dictionary<int, long> Right { get; } = [];

        public long[] LeftTags { get; } = new long[ConfusableData.Tags];

        public long[] RightTags { get; } = new long[ConfusableData.Tags];

        public long PairCount { get; set; }

        public long Count { get; set; }

        public double LeftScale { get; set; } = 1;

        public double RightScale { get; set; } = 1;
    }
}
