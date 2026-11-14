namespace FictionChecker.Eval.Text;

/// <summary>
/// A small random generator (SplitMix64) with a fixed algorithm, so the same seed gives the same planted
/// errors on every machine and .NET version.
/// </summary>
public sealed class Rng
{
    private ulong _state;

    /// <summary>Creates a generator from a numeric seed.</summary>
    public Rng(ulong seed) => _state = seed;

    /// <summary>Creates a generator from a seed and a list of names (corpus, book, category...).</summary>
    public Rng(ulong seed, params string[] names)
    {
        ulong hash = 14695981039346656037UL ^ seed;
        foreach (string name in names)
        {
            foreach (char c in name)
            {
                hash = (hash ^ c) * 1099511628211UL;
            }

            hash = (hash ^ 0xFF) * 1099511628211UL;
        }

        _state = hash;
    }

    /// <summary>The next 64 random bits.</summary>
    public ulong NextUInt64()
    {
        ulong z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>A random integer from 0 up to, but not including, <paramref name="maxExclusive"/>.</summary>
    public int Next(int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
        return (int)(NextUInt64() % (ulong)maxExclusive);
    }

    /// <summary>A random item from a list.</summary>
    public T Pick<T>(IReadOnlyList<T> items) => items[Next(items.Count)];

    /// <summary>Shuffles a list in place.</summary>
    public void Shuffle<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
