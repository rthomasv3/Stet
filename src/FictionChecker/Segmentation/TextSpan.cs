namespace FictionChecker.Segmentation;

/// <summary>A range of text. Positions are UTF-16 units, matching .NET and JavaScript strings.</summary>
internal readonly record struct TextSpan(int Start, int Length)
{
    /// <summary>Position just past the range.</summary>
    public int End => Start + Length;

    /// <summary>Whether a position is inside the range.</summary>
    public bool Contains(int position) => position >= Start && position < End;

    /// <summary>Creates a span from its start and end positions.</summary>
    public static TextSpan FromBounds(int start, int end) => new(start, end - start);

    /// <summary>Whether a position falls in any of the given spans.</summary>
    public static bool AnyContains(IReadOnlyList<TextSpan>? spans, int position)
    {
        if (spans is null)
        {
            return false;
        }

        for (int i = 0; i < spans.Count; i++)
        {
            if (spans[i].Contains(position))
            {
                return true;
            }
        }

        return false;
    }
}
