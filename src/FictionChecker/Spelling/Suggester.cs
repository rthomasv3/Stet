namespace FictionChecker.Spelling;

/// <summary>Proposes corrections for a misspelled word, best first.</summary>
internal sealed class Suggester
{
    private const int EditPenalty = 60;      // one edit outweighs about six doublings of frequency
    private const int UnseenFrequency = 30;  // words absent from the fiction counts are not treated as worthless
    private const int CustomFrequency = 150; // the writer's own words are likely what they meant
    private const int CommonWordFrequency = 110;
    private const int ProperNameFrequency = 90;

    private static readonly string[] KeyboardRows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"];

    private readonly Lexicon _lexicon;
    private readonly BuiltInLists _lists;
    private readonly WordClassifier _classifier;

    public Suggester(Lexicon lexicon, BuiltInLists lists, WordClassifier classifier)
    {
        _lexicon = lexicon;
        _lists = lists;
        _classifier = classifier;
    }

    /// <summary>
    /// Suggests replacements for <paramref name="word"/> (straight apostrophes), in the word's own case.
    /// </summary>
    public List<string> Suggest(string word, in WordClassifier.Context context, int max)
    {
        string lower = word.ToLowerInvariant();
        var scored = new Dictionary<string, int>(StringComparer.Ordinal);

        // A known misspelling's correction always comes first.
        string? known = _lists.Misspellings.GetValueOrDefault(lower);

        int maxDistance = lower.Length <= 3 ? 1 : 2;
        Dialect dialect = context.Dialect;
        bool typedLower = !char.IsUpper(word[0]);
        _lexicon.Search.Find(lower, maxDistance, (index, distance) =>
        {
            WordInfo info = _lexicon.InfoAt(index);
            if (!info.IsIn(dialect) || info.NeverSuggest)
            {
                return;
            }

            string candidate = _lexicon.WordAt(index);
            if (info.IsRare && _lists.Misspellings.ContainsKey(candidate))
            {
                return;
            }

            int penalty = info.IsRare ? 25 : 0;
            if (typedLower && char.IsUpper(candidate[0]))
            {
                // A lowercase slip is rarely meant as a name. Drop the name when the same word exists in
                // lowercase ("Glass" beside "glass"), and rank it lower otherwise.
                if (_lexicon.TryGet(candidate.ToLowerInvariant(), out WordInfo plain) && plain.IsIn(dialect))
                {
                    return;
                }

                penalty += 30;
            }

            int frequency = info.Frequency == 0 ? UnseenFrequency : info.Frequency;
            Add(scored, candidate, Score(lower, candidate, distance, frequency) - penalty);
        });

        CustomWords custom = context.Custom;
        if (!custom.IsEmpty)
        {
            string[] words = custom.SortedWords;
            bool inDialogue = context.InDialogue;
            custom.Search.Find(lower, maxDistance, (index, distance) =>
            {
                if (custom.Contains(words[index], inDialogue))
                {
                    Add(scored, words[index], Score(lower, words[index], distance, CustomFrequency));
                }
            });
        }

        AddSplits(lower, context, scored);

        var ranked = scored
            .Where(kv => !string.Equals(kv.Key, word, StringComparison.Ordinal))
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => MatchCase(word, kv.Key))
            .ToList();

        var result = new List<string>(max);
        if (known is not null)
        {
            result.Add(MatchCase(word, known));
        }

        foreach (string candidate in ranked)
        {
            if (result.Count >= max)
            {
                break;
            }

            if (!result.Contains(candidate))
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    /// <summary>
    /// Whether an unknown capitalized word looks like a slip for a known word rather than a new name.
    /// Away from a sentence start only a near miss of a known name counts ("Lodnon", or a custom name one
    /// letter off), because an ordinary word would not have a capital there. At a sentence start a near miss
    /// of a common word counts too ("Teh").
    /// </summary>
    public bool LooksLikeSlip(string word, in WordClassifier.Context context, bool sentenceStart)
    {
        string lower = word.ToLowerInvariant();
        if (_lists.Misspellings.ContainsKey(lower))
        {
            return true;
        }

        int maxDistance = lower.Length >= 7 ? 2 : 1;
        bool slip = false;
        Dialect dialect = context.Dialect;
        _lexicon.Search.Find(lower, maxDistance, (index, distance) =>
        {
            if (slip)
            {
                return;
            }

            WordInfo info = _lexicon.InfoAt(index);
            if (!info.IsIn(dialect) || info.IsRare || info.NeverSuggest)
            {
                return;
            }

            string candidate = _lexicon.WordAt(index);
            bool proper = char.IsUpper(candidate[0]);
            if (proper ? info.Frequency >= ProperNameFrequency || distance == 1 && info.Frequency > 0 : sentenceStart && info.Frequency >= CommonWordFrequency)
            {
                // A proper name must share the first letter: "Lodnon" is London, but "Mara" is not a slip for "Sara".
                slip = !proper || char.ToLowerInvariant(candidate[0]) == lower[0];
            }
        });

        if (!slip && !context.Custom.IsEmpty)
        {
            CustomWords custom = context.Custom;
            string[] words = custom.SortedWords;
            bool inDialogue = context.InDialogue;
            custom.Search.Find(lower, maxDistance, (index, _) => slip |= custom.Contains(words[index], inDialogue) && char.ToLowerInvariant(words[index][0]) == lower[0]);
        }

        return slip;
    }

    /// <summary>Gives a suggestion the capitals of the word it replaces, unless it has capitals of its own.</summary>
    public static string MatchCase(string original, string candidate)
    {
        if (candidate.Any(char.IsUpper))
        {
            return candidate; // London, Kaelith, iPhone keep their own shape
        }

        bool allUpper = original.Length > 1 && original.Any(char.IsLetter) && !original.Any(char.IsLower);
        if (allUpper)
        {
            return candidate.ToUpperInvariant();
        }

        return char.IsUpper(original[0]) ? char.ToUpperInvariant(candidate[0]) + candidate[1..] : candidate;
    }

    // "alot" → "a lot", "infront" → "in front": both halves must be ordinary words.
    private void AddSplits(string lower, in WordClassifier.Context context, Dictionary<string, int> scored)
    {
        for (int i = 1; i < lower.Length; i++)
        {
            ReadOnlySpan<char> left = lower.AsSpan(0, i);
            ReadOnlySpan<char> right = lower.AsSpan(i);
            if ((left.Length == 1 && left[0] is not ('a' or 'i')) || (right.Length == 1 && right[0] is not ('a' or 'i')))
            {
                continue;
            }

            if (!_lexicon.TryGet(left, out WordInfo l) || !_lexicon.TryGet(right, out WordInfo r)
                || l.IsRare || r.IsRare || !l.IsIn(context.Dialect) || !r.IsIn(context.Dialect))
            {
                continue;
            }

            // Both halves have to be common for a split to beat a one-letter fix.
            int frequency = Math.Min(l.Frequency, r.Frequency);
            Add(scored, string.Concat(left, " ", right), frequency - EditPenalty - 45);
        }
    }

    private static void Add(Dictionary<string, int> scored, string candidate, int score)
    {
        if (!scored.TryGetValue(candidate, out int existing) || score > existing)
        {
            scored[candidate] = score;
        }
    }

    private static int Score(string lower, string candidate, int distance, int frequency)
    {
        int score = frequency - (EditPenalty * distance);
        string candidateLower = candidate.ToLowerInvariant();
        if (candidateLower.Length > 0 && candidateLower[0] == lower[0])
        {
            score += 10; // slips rarely hit the first letter
        }

        if (distance == 1 && IsLikelySlip(lower, candidateLower))
        {
            score += 12;
        }

        if (distance > 0 && SoundKey(lower) == SoundKey(candidateLower))
        {
            score += 15;
        }

        return score;
    }

    // A one-edit difference of a kind fingers make often: swapped neighbours, a doubled or undoubled letter,
    // or a hit on the next key.
    private static bool IsLikelySlip(string typed, string intended)
    {
        if (typed.Length == intended.Length)
        {
            int first = -1;
            int last = -1;
            for (int i = 0; i < typed.Length; i++)
            {
                if (typed[i] != intended[i])
                {
                    if (first < 0)
                    {
                        first = i;
                    }

                    last = i;
                }
            }

            if (first == last)
            {
                return first >= 0 && AreNeighbours(typed[first], intended[first]);
            }

            return last == first + 1 && typed[first] == intended[last] && typed[last] == intended[first];
        }

        // One letter longer or shorter: is the extra letter a double of its neighbour?
        string longer = typed.Length > intended.Length ? typed : intended;
        string shorter = typed.Length > intended.Length ? intended : typed;
        int at = 0;
        while (at < shorter.Length && longer[at] == shorter[at])
        {
            at++;
        }

        return (at > 0 && longer[at] == longer[at - 1]) || (at + 1 < longer.Length && longer[at] == longer[at + 1]);
    }

    private static bool AreNeighbours(char a, char b)
    {
        foreach (string row in KeyboardRows)
        {
            int at = row.IndexOf(a, StringComparison.Ordinal);
            if (at >= 0)
            {
                return (at > 0 && row[at - 1] == b) || (at + 1 < row.Length && row[at + 1] == b);
            }
        }

        return false;
    }

    // A rough sound-alike key: first letter kept, later vowels dropped, common same-sound letters merged,
    // doubles collapsed. "recieve" and "receive" share a key; so do "definately" and "definitely".
    private static string SoundKey(string word)
    {
        Span<char> key = stackalloc char[Math.Min(word.Length, 48)];
        int length = 0;
        for (int i = 0; i < word.Length && length < key.Length; i++)
        {
            char c = word[i];
            if (c == 'p' && i + 1 < word.Length && word[i + 1] == 'h')
            {
                c = 'f';
                i++;
            }

            c = c switch
            {
                'c' or 'q' => 'k',
                'z' => 's',
                'y' when i > 0 => 'i',
                _ => c,
            };

            bool vowel = c is 'a' or 'e' or 'i' or 'o' or 'u';
            if ((vowel && length > 0) || !char.IsLetter(c))
            {
                continue;
            }

            if (length == 0 || key[length - 1] != c)
            {
                key[length++] = c;
            }
        }

        return new string(key[..length]);
    }
}
