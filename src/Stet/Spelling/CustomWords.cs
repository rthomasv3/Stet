using System.Collections.Frozen;
using Stet.Segmentation;

namespace Stet.Spelling;

/// <summary>
/// An unchanging snapshot of the caller's word sets (world names, personal dictionary, a book's slang).
/// Changing a set makes a new snapshot, so a check that is running never sees half an update.
///
/// A word keeps the case it was added with: "Kaelith" is valid as Kaelith or KAELITH but not kaelith, while
/// a word added in lowercase is also valid capitalized.
/// </summary>
internal sealed class CustomWords
{
    private readonly FrozenDictionary<string, (FrozenSet<string> Words, WordScope Scope)> _sets;
    private readonly HashSet<string> _everywhere = new(StringComparer.Ordinal);
    private readonly HashSet<string> _dialogue = new(StringComparer.Ordinal);
    private readonly HashSet<string> _everywhereUpper = new(StringComparer.Ordinal);
    private readonly HashSet<string> _dialogueUpper = new(StringComparer.Ordinal);
    private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _everywhereSpan;
    private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _dialogueSpan;
    private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _everywhereUpperSpan;
    private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _dialogueUpperSpan;

    private CustomWords(FrozenDictionary<string, (FrozenSet<string> Words, WordScope Scope)> sets)
    {
        _sets = sets;
        foreach ((FrozenSet<string> words, WordScope scope) in sets.Values)
        {
            HashSet<string> exact = scope == WordScope.Everywhere ? _everywhere : _dialogue;
            HashSet<string> upper = scope == WordScope.Everywhere ? _everywhereUpper : _dialogueUpper;
            foreach (string word in words)
            {
                exact.Add(word);
                upper.Add(word.ToUpperInvariant());
            }
        }

        _everywhereSpan = _everywhere.GetAlternateLookup<ReadOnlySpan<char>>();
        _dialogueSpan = _dialogue.GetAlternateLookup<ReadOnlySpan<char>>();
        _everywhereUpperSpan = _everywhereUpper.GetAlternateLookup<ReadOnlySpan<char>>();
        _dialogueUpperSpan = _dialogueUpper.GetAlternateLookup<ReadOnlySpan<char>>();

        string[] all = [.. _everywhere.Union(_dialogue).Where(w => w.Length <= SuggestionIndex.MaxLength)];
        Array.Sort(all, StringComparer.Ordinal);
        SortedWords = all;
        Search = new SuggestionIndex(all);
    }

    /// <summary>No custom words.</summary>
    public static CustomWords Empty { get; } = new(FrozenDictionary<string, (FrozenSet<string>, WordScope)>.Empty);

    /// <summary>Whether there are no custom words at all.</summary>
    public bool IsEmpty => _everywhere.Count == 0 && _dialogue.Count == 0;

    /// <summary>Every custom word, sorted, for suggestions.</summary>
    public string[] SortedWords { get; }

    /// <summary>Finds custom words close to a misspelling.</summary>
    public SuggestionIndex Search { get; }

    /// <summary>Names of the sets in this snapshot.</summary>
    public IEnumerable<string> SetNames => _sets.Keys;

    /// <summary>
    /// Returns a snapshot in which the named set holds exactly <paramref name="words"/>, or this snapshot when
    /// it already does. Entries are split on spaces, so "Kaelith Vorn" adds both names. Apostrophes are stored
    /// straight.
    /// </summary>
    public CustomWords WithSet(string name, IEnumerable<string> words, WordScope scope)
    {
        var cleaned = new HashSet<string>(StringComparer.Ordinal);
        foreach (string entry in words)
        {
            foreach (string part in entry.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                string word = part.Replace(CharClass.RightSingleQuote, '\'').Trim('\'', '"', '.', ',', ';', ':', '!', '?', '(', ')');
                if (word.Length > 0)
                {
                    cleaned.Add(word);
                }
            }
        }

        // Nothing changes: keep this snapshot, so caches built on it stay valid. An app can then hand over
        // its whole word list before every check without paying for it.
        if (_sets.TryGetValue(name, out (FrozenSet<string> Words, WordScope Scope) existing)
            && existing.Scope == scope && existing.Words.Count == cleaned.Count && existing.Words.IsSupersetOf(cleaned))
        {
            return this;
        }

        var sets = _sets.ToDictionary(StringComparer.Ordinal);
        sets[name] = (cleaned.ToFrozenSet(StringComparer.Ordinal), scope);
        return new CustomWords(sets.ToFrozenDictionary(StringComparer.Ordinal));
    }

    /// <summary>Returns a snapshot with words added to a set (created if missing, keeping its scope if present).</summary>
    public CustomWords WithAdded(string name, IEnumerable<string> words, WordScope scopeIfNew = WordScope.Everywhere)
    {
        if (_sets.TryGetValue(name, out (FrozenSet<string> Words, WordScope Scope) existing))
        {
            return WithSet(name, existing.Words.Concat(words), existing.Scope);
        }

        return WithSet(name, words, scopeIfNew);
    }

    /// <summary>Returns a snapshot with words removed from a set.</summary>
    public CustomWords WithRemoved(string name, IEnumerable<string> words)
    {
        if (!_sets.TryGetValue(name, out (FrozenSet<string> Words, WordScope Scope) existing))
        {
            return this;
        }

        var remove = new HashSet<string>(words.Select(w => w.Replace(CharClass.RightSingleQuote, '\'')), StringComparer.Ordinal);
        return WithSet(name, existing.Words.Where(w => !remove.Contains(w)), existing.Scope);
    }

    /// <summary>
    /// Returns a snapshot holding only the named sets (names that do not exist are ignored), or this snapshot
    /// when that is every set.
    /// </summary>
    public CustomWords Subset(IReadOnlyCollection<string> names)
    {
        var kept = _sets.Where(kv => names.Contains(kv.Key)).ToList();
        return kept.Count == _sets.Count ? this : new CustomWords(kept.ToFrozenDictionary(StringComparer.Ordinal));
    }

    /// <summary>Whether a named set is the very same in another snapshot (or missing from both).</summary>
    public bool HasSameSet(CustomWords other, string name)
    {
        bool here = _sets.TryGetValue(name, out (FrozenSet<string> Words, WordScope Scope) mine);
        bool there = other._sets.TryGetValue(name, out (FrozenSet<string> Words, WordScope Scope) theirs);
        return here == there && (!here || (ReferenceEquals(mine.Words, theirs.Words) && mine.Scope == theirs.Scope));
    }

    /// <summary>Returns a snapshot without the named set.</summary>
    public CustomWords WithoutSet(string name)
    {
        if (!_sets.ContainsKey(name))
        {
            return this;
        }

        var sets = _sets.ToDictionary(StringComparer.Ordinal);
        sets.Remove(name);
        return new CustomWords(sets.ToFrozenDictionary(StringComparer.Ordinal));
    }

    /// <summary>
    /// Whether a word is valid by the custom sets. <paramref name="word"/> must have straight apostrophes.
    /// </summary>
    public bool Contains(ReadOnlySpan<char> word, bool inDialogue)
    {
        if (IsEmpty || word.Length == 0 || word.Length > 64)
        {
            return false;
        }

        if (ContainsExact(word, inDialogue))
        {
            return true;
        }

        if (!char.IsUpper(word[0]))
        {
            return false;
        }

        Span<char> buffer = stackalloc char[word.Length];
        if (IsAllUpper(word))
        {
            // KAELITH matches Kaelith; SHOUTED matches shouted.
            word.ToUpperInvariant(buffer);
            return _everywhereUpperSpan.Contains(buffer) || (inDialogue && _dialogueUpperSpan.Contains(buffer));
        }

        // A lowercase custom word is fine with a capital (sentence start), if the rest is lowercase.
        word.CopyTo(buffer);
        buffer[0] = char.ToLowerInvariant(buffer[0]);
        return IsAllLower(buffer) && ContainsExact(buffer, inDialogue);
    }

    /// <summary>
    /// The custom word that differs from <paramref name="word"/> only by case (so "kaelith" finds "Kaelith"),
    /// or null.
    /// </summary>
    public string? FindIgnoringCase(ReadOnlySpan<char> word, bool inDialogue)
    {
        if (IsEmpty || word.Length > 64)
        {
            return null;
        }

        Span<char> upper = stackalloc char[word.Length];
        word.ToUpperInvariant(upper);
        bool everywhere = _everywhereUpperSpan.Contains(upper);
        if (!everywhere && !(inDialogue && _dialogueUpperSpan.Contains(upper)))
        {
            return null;
        }

        foreach (string candidate in everywhere ? _everywhere : _dialogue)
        {
            if (candidate.Length == word.Length && candidate.AsSpan().Equals(word, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool ContainsExact(ReadOnlySpan<char> word, bool inDialogue) =>
        _everywhereSpan.Contains(word) || (inDialogue && _dialogueSpan.Contains(word));

    private static bool IsAllUpper(ReadOnlySpan<char> word)
    {
        if (word.Length < 2)
        {
            return false;
        }

        foreach (char c in word)
        {
            if (char.IsLower(c))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAllLower(ReadOnlySpan<char> word)
    {
        foreach (char c in word)
        {
            if (char.IsUpper(c))
            {
                return false;
            }
        }

        return true;
    }
}
