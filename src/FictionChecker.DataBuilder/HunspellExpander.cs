using System.IO.Compression;
using System.Text;

namespace FictionChecker.DataBuilder;

/// <summary>
/// Expands a Hunspell dictionary (stems plus prefix and suffix rules) into the flat list of every word it
/// accepts. Written from the published file format; SCOWL's affix files use only its simple parts.
/// </summary>
internal sealed class HunspellExpander
{
    private readonly Dictionary<char, List<Rule>> _rules = [];
    private readonly HashSet<char> _skipFlags = [];
    private readonly HashSet<char> _combinable = [];
    private char _noSuggest;

    /// <summary>A word and whether the dictionary asks that it never be offered as a suggestion.</summary>
    public readonly record struct Entry(string Word, bool NoSuggest);

    /// <summary>Reads the <c>.aff</c> and <c>.dic</c> files from a SCOWL Hunspell zip and expands them.</summary>
    public static List<Entry> ExpandZip(string zipPath)
    {
        using ZipArchive zip = ZipFile.OpenRead(zipPath);
        string aff = ReadEntry(zip, ".aff");
        string dic = ReadEntry(zip, ".dic");
        var expander = new HunspellExpander();
        expander.ReadAffixes(aff);
        return expander.Expand(dic);
    }

    /// <summary>Parses the affix rules.</summary>
    public void ReadAffixes(string aff)
    {
        foreach (string raw in aff.Split('\n'))
        {
            string[] parts = raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            switch (parts[0])
            {
                case "NOSUGGEST":
                    _noSuggest = parts[1][0];
                    break;
                case "ONLYINCOMPOUND":
                    _skipFlags.Add(parts[1][0]);
                    break;
                case "PFX" or "SFX" when parts.Length == 4:
                    // Header line: SFX flag Y|N count. Y means the affix may combine with one of the other kind.
                    if (parts[2] == "Y")
                    {
                        _combinable.Add(parts[1][0]);
                    }

                    break;
                case "PFX" or "SFX" when parts.Length >= 5:
                    // Rule line: SFX flag strip add condition. (The header line has only four fields.)
                    char flag = parts[1][0];
                    string add = parts[3].Split('/')[0];
                    var rule = new Rule(
                        flag,
                        parts[0] == "PFX",
                        parts[2] == "0" ? "" : parts[2],
                        add == "0" ? "" : add,
                        Condition.Parse(parts[4]));
                    if (!_rules.TryGetValue(flag, out List<Rule>? list))
                    {
                        _rules[flag] = list = [];
                    }

                    list.Add(rule);
                    break;
            }
        }
    }

    /// <summary>Expands every stem of a <c>.dic</c> file.</summary>
    public List<Entry> Expand(string dic)
    {
        var seen = new Dictionary<string, bool>(StringComparer.Ordinal);
        string[] lines = dic.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            int slash = line.IndexOf('/', StringComparison.Ordinal);
            string stem = slash < 0 ? line : line[..slash];
            string flags = slash < 0 ? "" : line[(slash + 1)..].Split(' ', '\t')[0];
            if (flags.Any(_skipFlags.Contains) || stem.Any(char.IsDigit))
            {
                continue; // number-building pieces such as "1th"
            }

            bool noSuggest = flags.Contains(_noSuggest);
            Add(seen, stem, noSuggest);

            var prefixes = new List<Rule>();
            var suffixed = new List<(string Word, char Flag)>();
            foreach (char flag in flags)
            {
                if (!_rules.TryGetValue(flag, out List<Rule>? rules))
                {
                    continue;
                }

                foreach (Rule rule in rules)
                {
                    if (rule.IsPrefix)
                    {
                        prefixes.Add(rule);
                    }
                    else if (rule.TryApply(stem, out string? word))
                    {
                        suffixed.Add((word, flag));
                        Add(seen, word, noSuggest);
                    }
                }
            }

            foreach (Rule prefix in prefixes)
            {
                if (prefix.TryApply(stem, out string? word))
                {
                    Add(seen, word, noSuggest);
                }

                // A prefix also goes on a suffixed form when both affixes allow combining.
                foreach ((string form, char suffixFlag) in suffixed)
                {
                    if (_combinable.Contains(prefix.Flag) && _combinable.Contains(suffixFlag) && prefix.TryApply(form, out string? both))
                    {
                        Add(seen, both, noSuggest);
                    }
                }
            }
        }

        return seen.Select(kv => new Entry(kv.Key, kv.Value)).ToList();
    }

    private static void Add(Dictionary<string, bool> seen, string word, bool noSuggest)
    {
        // A word is only "never suggest" if every route to it says so.
        seen[word] = seen.TryGetValue(word, out bool existing) ? existing && noSuggest : noSuggest;
    }

    private static string ReadEntry(ZipArchive zip, string extension)
    {
        ZipArchiveEntry entry = zip.Entries.Single(e => e.Name.EndsWith(extension, StringComparison.Ordinal));
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private sealed record Rule(char Flag, bool IsPrefix, string Strip, string Add, Condition Condition)
    {
        public bool TryApply(string stem, out string word)
        {
            word = "";
            if (IsPrefix)
            {
                if (!stem.StartsWith(Strip, StringComparison.Ordinal) || !Condition.MatchesStart(stem))
                {
                    return false;
                }

                word = Add + stem[Strip.Length..];
                return true;
            }

            if (!stem.EndsWith(Strip, StringComparison.Ordinal) || !Condition.MatchesEnd(stem))
            {
                return false;
            }

            word = stem[..^Strip.Length] + Add;
            return true;
        }
    }

    /// <summary>A Hunspell condition: a sequence of characters, <c>.</c>, <c>[abc]</c> or <c>[^abc]</c>.</summary>
    private sealed class Condition
    {
        private readonly List<(string Chars, bool Negated, bool Any)> _positions = [];

        public static Condition Parse(string text)
        {
            var condition = new Condition();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '.')
                {
                    condition._positions.Add(("", false, true));
                }
                else if (text[i] == '[')
                {
                    int close = text.IndexOf(']', i);
                    string set = text[(i + 1)..close];
                    bool negated = set.StartsWith('^');
                    condition._positions.Add((negated ? set[1..] : set, negated, false));
                    i = close;
                }
                else
                {
                    condition._positions.Add((text[i].ToString(), false, false));
                }
            }

            return condition;
        }

        public bool MatchesEnd(string word) => Matches(word, word.Length - _positions.Count);

        public bool MatchesStart(string word) => Matches(word, 0);

        private bool Matches(string word, int start)
        {
            if (_positions.Count == 1 && _positions[0].Any)
            {
                return true;
            }

            if (start < 0 || start + _positions.Count > word.Length)
            {
                return false;
            }

            for (int i = 0; i < _positions.Count; i++)
            {
                (string chars, bool negated, bool any) = _positions[i];
                if (!any && chars.Contains(word[start + i]) == negated)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
