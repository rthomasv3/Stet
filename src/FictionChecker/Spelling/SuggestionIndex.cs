namespace FictionChecker.Spelling;

/// <summary>
/// Finds the words of a sorted list that are within a few edits of a given word (insert, delete, substitute,
/// or swap two neighbouring letters).
///
/// Sorted words share prefixes with their neighbours, so the edit-distance table for a shared prefix is
/// computed once and reused, and a whole run of words is skipped as soon as their common prefix is already
/// too far from the target. No extra index is stored beyond the shared-prefix length of each word.
/// </summary>
internal sealed class SuggestionIndex
{
    /// <summary>Longest word considered; anything longer is never a candidate.</summary>
    public const int MaxLength = 40;

    private readonly string[] _words;
    private readonly byte[] _shared;

    /// <summary>Creates an index over words in ordinal order.</summary>
    public SuggestionIndex(string[] sortedWords)
    {
        _words = sortedWords;
        _shared = new byte[sortedWords.Length];
        for (int i = 1; i < sortedWords.Length; i++)
        {
            string a = sortedWords[i - 1];
            string b = sortedWords[i];
            int limit = Math.Min(Math.Min(a.Length, b.Length), MaxLength);
            int n = 0;
            while (n < limit && a[n] == b[n])
            {
                n++;
            }

            _shared[i] = (byte)n;
        }
    }

    /// <summary>Receives each match: the word's index in the list and its distance.</summary>
    public delegate void Found(int index, int distance);

    /// <summary>
    /// Reports every word within <paramref name="maxDistance"/> edits of <paramref name="target"/>. Letters
    /// are compared without regard to case; <paramref name="target"/> must already be lowercase.
    /// </summary>
    public void Find(ReadOnlySpan<char> target, int maxDistance, Found found)
    {
        int n = target.Length;
        if (n == 0 || n > MaxLength)
        {
            return;
        }

        // rows[d] is the edit-distance row after d letters of the current word.
        int width = n + 1;
        int[] rows = new int[(MaxLength + 2) * width];
        for (int j = 0; j <= n; j++)
        {
            rows[j] = j;
        }

        Span<char> current = stackalloc char[MaxLength + 1];
        int computed = 0;            // rows are valid for this many letters of the previous word
        int dead = int.MaxValue;     // a prefix of this length is already too far; skip words that share it

        for (int i = 0; i < _words.Length; i++)
        {
            string word = _words[i];
            int shared = _shared[i];
            if (shared >= dead)
            {
                continue;
            }

            int start = Math.Min(shared, computed);
            dead = int.MaxValue;
            if (word.Length > n + maxDistance || word.Length > MaxLength)
            {
                // Too long to match. Rows up to the shared prefix stay valid for the next word.
                computed = start;
                continue;
            }

            int depth = start;
            bool alive = true;
            while (depth < word.Length)
            {
                char c = char.ToLowerInvariant(word[depth]);
                current[depth] = c;
                depth++;
                int row = depth * width;
                int above = row - width;
                rows[row] = depth;
                int min = depth;
                for (int j = 1; j <= n; j++)
                {
                    int cost = target[j - 1] == c ? 0 : 1;
                    int value = Math.Min(Math.Min(rows[above + j] + 1, rows[row + j - 1] + 1), rows[above + j - 1] + cost);
                    if (depth > 1 && j > 1 && c == target[j - 2] && current[depth - 2] == target[j - 1])
                    {
                        value = Math.Min(value, rows[above - width + j - 2] + 1);
                    }

                    rows[row + j] = value;
                    if (value < min)
                    {
                        min = value;
                    }
                }

                if (min > maxDistance)
                {
                    // Every longer word with this prefix is at least this far away.
                    alive = false;
                    dead = depth;
                    break;
                }
            }

            computed = depth;
            if (alive && depth == word.Length)
            {
                int distance = rows[(depth * width) + n];
                if (distance <= maxDistance)
                {
                    found(i, distance);
                }
            }
        }
    }
}
