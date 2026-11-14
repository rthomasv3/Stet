using FictionChecker.Segmentation;

namespace FictionChecker.Spelling;

/// <summary>What the classifier decided about a word.</summary>
internal enum WordStatus
{
    /// <summary>Correct.</summary>
    Valid,

    /// <summary>Correct only in another dialect.</summary>
    OtherDialect,

    /// <summary>Correct only with different capitals; <c>Correction</c> holds the right form.</summary>
    WrongCase,

    /// <summary>Not a known word.</summary>
    Unknown,
}

/// <summary>The classifier's verdict, with the corrected form when only the capitals are wrong.</summary>
internal readonly record struct WordVerdict(WordStatus Status, string? Correction = null)
{
    public static readonly WordVerdict Valid = new(WordStatus.Valid);
    public static readonly WordVerdict Unknown = new(WordStatus.Unknown);
    public static readonly WordVerdict OtherDialect = new(WordStatus.OtherDialect);
}

/// <summary>
/// Decides whether a word is correctly spelled: dictionary lookup with the rules for capitals, possessives,
/// contractions, stretched words and informal speech. Words must have straight apostrophes.
/// </summary>
internal sealed class WordClassifier
{
    // Word starts that are fine before a hyphen even though they are not words on their own.
    private static readonly HashSet<string> HyphenPrefixes = new(StringComparer.Ordinal)
    {
        "non", "anti", "pre", "post", "semi", "multi", "pseudo", "quasi", "ultra", "counter", "inter", "intra",
        "sub", "super", "un", "re", "co", "de", "mid", "ex", "self", "neo", "proto", "demi", "hemi", "bi", "tri",
        "mis", "dis", "over", "under", "out", "extra", "micro", "macro", "mega", "mini", "omni", "poly", "trans",
        "vice", "arch", "be", "a", "half", "ill", "well", "all", "cross", "great", "step", "fore", "after",
    };

    private static readonly string[] Clitics = ["'d", "'ve", "'ll", "'re", "'d've", "'ll've", "'n't"];

    private readonly Lexicon _lexicon;
    private readonly BuiltInLists _lists;
    private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _prefixes = HyphenPrefixes.GetAlternateLookup<ReadOnlySpan<char>>();

    public WordClassifier(Lexicon lexicon, BuiltInLists lists)
    {
        _lexicon = lexicon;
        _lists = lists;
    }

    /// <summary>The settings that change a verdict, gathered for one check.</summary>
    internal readonly record struct Context(Dialect Dialect, CustomWords Custom, bool InDialogue, bool Lenient, bool FlagOtherDialects);

    /// <summary>Classifies a word with no hyphen in it.</summary>
    /// <param name="word">The word, straight apostrophes.</param>
    /// <param name="flags">The token's flags (apostrophes at its edges).</param>
    /// <param name="context">Dialect, custom words and whether the word is inside dialogue.</param>
    public WordVerdict Classify(ReadOnlySpan<char> word, TokenFlags flags, in Context context)
    {
        if (word.Length == 0 || word.Length > 64)
        {
            return WordVerdict.Valid;
        }

        WordVerdict verdict = ClassifyForms(word, context);
        if (verdict.Status == WordStatus.Valid)
        {
            return verdict;
        }

        // Possessive: X's is right whenever X is.
        if (word.Length > 2 && word.EndsWith("'s", StringComparison.OrdinalIgnoreCase))
        {
            WordVerdict stem = ClassifyForms(word[..^2], context);
            if (stem.Status != WordStatus.Unknown)
            {
                return stem.Status == WordStatus.WrongCase ? new WordVerdict(WordStatus.WrongCase, stem.Correction + "'s") : Better(verdict, stem);
            }
        }

        // Contractions the dictionary does not list one by one: that've, who'd, it'll.
        foreach (string clitic in Clitics)
        {
            if (word.Length > clitic.Length && word.EndsWith(clitic, StringComparison.OrdinalIgnoreCase)
                && ClassifyForms(word[..^clitic.Length], context).Status == WordStatus.Valid)
            {
                return WordVerdict.Valid;
            }
        }

        Span<char> lower = stackalloc char[word.Length];
        word.ToLowerInvariant(lower);
        if (_lists.Archaic.GetAlternateLookup<ReadOnlySpan<char>>().Contains(lower))
        {
            return WordVerdict.Valid;
        }

        if (IsArchaicVerb(lower, context) || IsRegularDerivation(lower, context))
        {
            return WordVerdict.Valid;
        }

        if (context.InDialogue && context.Lenient && IsInformal(lower, flags, context))
        {
            return WordVerdict.Valid;
        }

        if (IsStretched(lower, context, context.InDialogue && context.Lenient))
        {
            return WordVerdict.Valid;
        }

        return verdict;
    }

    /// <summary>
    /// Whether a part of a hyphenated word is acceptable on its own merits: a prefix (non-, semi-), a single
    /// letter (T-shirt), or a word.
    /// </summary>
    public WordVerdict ClassifyHyphenPart(ReadOnlySpan<char> part, TokenFlags flags, in Context context, bool isFirstPart)
    {
        if (part.Length <= 1)
        {
            return WordVerdict.Valid;
        }

        Span<char> lower = stackalloc char[Math.Min(part.Length, 64)];
        if (part.Length <= 64)
        {
            part.ToLowerInvariant(lower);
            if (_prefixes.Contains(lower))
            {
                return WordVerdict.Valid;
            }
        }

        WordVerdict verdict = Classify(part, flags, context);
        if (verdict.Status == WordStatus.Unknown && !isFirstPart && part.Length <= 64 && IsNounPlusEd(lower, context))
        {
            return WordVerdict.Valid;
        }

        return verdict;
    }

    /// <summary>
    /// Whether a hyphenated word is a stutter: short repeated starts of the final part ("W-w-what", "I-I",
    /// "th-thank"). The final part still has to be a word.
    /// </summary>
    public static bool IsStutter(ReadOnlySpan<char> word, out int finalPartStart)
    {
        finalPartStart = word.LastIndexOf('-') + 1;
        if (finalPartStart <= 0 || finalPartStart >= word.Length)
        {
            return false;
        }

        ReadOnlySpan<char> final = word[finalPartStart..];
        ReadOnlySpan<char> rest = word[..(finalPartStart - 1)];
        while (rest.Length > 0)
        {
            int hyphen = rest.LastIndexOf('-');
            ReadOnlySpan<char> part = rest[(hyphen + 1)..];
            if (part.Length == 0 || part.Length > 3 || part.Length > final.Length || !final.StartsWith(part, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            rest = hyphen < 0 ? default : rest[..hyphen];
        }

        return true;
    }

    /// <summary>
    /// Looks a word up as written and in the other capitalizations that are acceptable for it.
    /// </summary>
    public WordVerdict ClassifyForms(ReadOnlySpan<char> word, in Context context)
    {
        if (word.Length == 0)
        {
            return WordVerdict.Unknown;
        }

        if (context.Custom.Contains(word, context.InDialogue))
        {
            return WordVerdict.Valid;
        }

        WordStatus best = Look(word, context);
        if (best == WordStatus.Valid)
        {
            return WordVerdict.Valid;
        }

        Shape shape = ShapeOf(word);
        Span<char> buffer = stackalloc char[word.Length];
        if (shape is Shape.Capitalized or Shape.AllUpper)
        {
            // A lowercase word is fine with a capital (sentence start, title) or in capitals (shouting).
            word.ToLowerInvariant(buffer);
            best = Better(best, Look(buffer, context));
            if (best != WordStatus.Valid && shape == Shape.AllUpper && word.Length > 1)
            {
                // LONDON for London.
                buffer[0] = word[0];
                best = Better(best, Look(buffer, context));
            }
        }
        else if (shape == Shape.Lower && best == WordStatus.Unknown && word.Length >= 4)
        {
            // Only valid with a capital: london, elizabeth, kaelith.
            word.CopyTo(buffer);
            buffer[0] = char.ToUpperInvariant(buffer[0]);
            if (_lexicon.TryGet(buffer, out _))
            {
                return new WordVerdict(WordStatus.WrongCase, new string(buffer));
            }

            string? custom = context.Custom.FindIgnoringCase(word, context.InDialogue);
            if (custom is not null)
            {
                return new WordVerdict(WordStatus.WrongCase, custom);
            }
        }

        return new WordVerdict(best);
    }

    /// <summary>Whether a word is in the dictionary for the dialect, exactly as written.</summary>
    public WordStatus Look(ReadOnlySpan<char> word, in Context context)
    {
        if (!_lexicon.TryGet(word, out WordInfo info))
        {
            return WordStatus.Unknown;
        }

        // A rare real word that is far more often a slip (calender, wether) is treated as the slip.
        if (info.IsRare && _lists.Misspellings.GetAlternateLookup<ReadOnlySpan<char>>().ContainsKey(word))
        {
            return WordStatus.Unknown;
        }

        if (info.IsIn(context.Dialect) || !context.FlagOtherDialects)
        {
            return WordStatus.Valid;
        }

        return WordStatus.OtherDialect;
    }

    // Old third-person verbs: "maketh", "springeth", "fleeth". The -est forms are left out because they
    // collide with misspelled superlatives ("bigest").
    private bool IsArchaicVerb(ReadOnlySpan<char> lower, in Context context)
    {
        if (lower.Length < 5 || !lower.EndsWith("th", StringComparison.Ordinal))
        {
            return false;
        }

        if (lower.EndsWith("eth", StringComparison.Ordinal))
        {
            ReadOnlySpan<char> stem = lower[..^3];
            if (stem.Length >= 2 && (Look(stem, context) == WordStatus.Valid || (stem.Length >= 3 && stem[^1] == stem[^2] && Look(stem[..^1], context) == WordStatus.Valid)))
            {
                return true; // walketh, runneth
            }
        }

        ReadOnlySpan<char> withE = lower[..^2];
        return withE[^1] == 'e' && withE.Length >= 3 && Look(withE, context) == WordStatus.Valid; // maketh, fleeth
    }

    // Regular derivations fiction coins freely and no dictionary lists in full: "bellowings", "slurringly",
    // "unlidded", "buglike", "wickless". Each needs its base to be a known word.
    private bool IsRegularDerivation(ReadOnlySpan<char> lower, in Context context)
    {
        if (lower.Length < 6)
        {
            return false;
        }

        // -ings (plural of an -ing noun), -ingly and -edly (adverb from a participle).
        if (lower.EndsWith("ings", StringComparison.Ordinal) && Look(lower[..^1], context) == WordStatus.Valid)
        {
            return true;
        }

        if ((lower.EndsWith("ingly", StringComparison.Ordinal) || lower.EndsWith("edly", StringComparison.Ordinal))
            && Look(lower[..^2], context) == WordStatus.Valid)
        {
            return true;
        }

        // -like, -less and -ness on a known word of three letters or more.
        foreach (string suffix in (ReadOnlySpan<string>)["like", "less", "ness"])
        {
            if (lower.Length >= suffix.Length + 3 && lower.EndsWith(suffix, StringComparison.Ordinal)
                && Look(lower[..^suffix.Length], context) == WordStatus.Valid)
            {
                return true;
            }
        }

        // un- on a known participle or adjective: unlidded, unbonded, unwinking.
        if (lower.StartsWith("un", StringComparison.Ordinal) && lower.Length >= 7)
        {
            ReadOnlySpan<char> rest = lower[2..];
            bool descriptive = rest.EndsWith("ed", StringComparison.Ordinal) || rest.EndsWith("ing", StringComparison.Ordinal)
                || rest.EndsWith("able", StringComparison.Ordinal) || rest.EndsWith("ingly", StringComparison.Ordinal);
            if (descriptive && (Look(rest, context) == WordStatus.Valid || (rest.EndsWith("ingly", StringComparison.Ordinal) && Look(rest[..^2], context) == WordStatus.Valid)))
            {
                return true;
            }
        }

        return false;
    }

    // The second half of compounds like "dark-browed", "long-limbed", "good-humouredly": a noun plus -ed.
    private bool IsNounPlusEd(ReadOnlySpan<char> lower, in Context context)
    {
        Span<char> withE = stackalloc char[lower.Length];
        foreach (string ending in (ReadOnlySpan<string>)["edly", "edness", "ed"])
        {
            if (lower.Length <= ending.Length + 2 || !lower.EndsWith(ending, StringComparison.Ordinal))
            {
                continue;
            }

            ReadOnlySpan<char> stem = lower[..^ending.Length];
            if (Look(stem, context) == WordStatus.Valid)
            {
                return true; // browed, limbed
            }

            stem.CopyTo(withE);
            withE[stem.Length] = 'e';
            if (Look(withE[..(stem.Length + 1)], context) == WordStatus.Valid)
            {
                return true; // nosed, natured
            }

            if (stem.Length >= 3 && stem[^1] == stem[^2] && Look(stem[..^1], context) == WordStatus.Valid)
            {
                return true; // legged, lipped
            }
        }

        return false;
    }

    private bool IsInformal(ReadOnlySpan<char> lower, TokenFlags flags, in Context context)
    {
        var informal = _lists.Informal.GetAlternateLookup<ReadOnlySpan<char>>();
        if (informal.Contains(lower))
        {
            return true;
        }

        if ((flags & TokenFlags.LeadingApostrophe) != 0)
        {
            Span<char> withMark = stackalloc char[lower.Length + 1];
            withMark[0] = '\'';
            lower.CopyTo(withMark[1..]);
            if (informal.Contains(withMark) || WordLists.LeadingApostropheWords.GetAlternateLookup<ReadOnlySpan<char>>().Contains(lower))
            {
                return true;
            }
        }

        if ((flags & TokenFlags.TrailingApostrophe) != 0)
        {
            if (WordLists.TrailingApostropheWords.GetAlternateLookup<ReadOnlySpan<char>>().Contains(lower))
            {
                return true;
            }

            // runnin', nothin': a dropped g when the full word is a word.
            if (lower.Length >= 4 && lower.EndsWith("in", StringComparison.Ordinal))
            {
                Span<char> full = stackalloc char[lower.Length + 1];
                lower.CopyTo(full);
                full[^1] = 'g';
                return ClassifyForms(full, context).Status == WordStatus.Valid;
            }
        }

        return false;
    }

    // "Nooooo", "sooo", "hmmmm": valid when squeezing the repeated letters gives a word. A letter typed three
    // times in a longer word ("businesss") is more often a slip, so outside lenient dialogue the word must be
    // short once squeezed, or stretched to four or more.
    private bool IsStretched(ReadOnlySpan<char> lower, in Context context, bool lenientDialogue)
    {
        int longestRun = 1;
        int run = 1;
        for (int i = 1; i < lower.Length; i++)
        {
            run = lower[i] == lower[i - 1] ? run + 1 : 1;
            longestRun = Math.Max(longestRun, run);
        }

        if (longestRun < 3)
        {
            return false;
        }

        int maxSqueezedLength = lenientDialogue || longestRun >= 4 ? int.MaxValue : 4;

        // Try every run squeezed to one letter, then to two (so "noooo" finds "no" and "coool" finds "cool").
        Span<char> squeezed = stackalloc char[lower.Length];
        for (int keep = 1; keep <= 2; keep++)
        {
            int length = 0;
            run = 0;
            for (int i = 0; i < lower.Length; i++)
            {
                run = i > 0 && lower[i] == lower[i - 1] ? run + 1 : 1;
                if (run <= keep || !IsLongRun(lower, i))
                {
                    squeezed[length++] = lower[i];
                }
            }

            if (length >= 2 && length <= maxSqueezedLength && ClassifyForms(squeezed[..length], context).Status == WordStatus.Valid)
            {
                return true;
            }
        }

        return false;
    }

    // Whether position i belongs to a run of three or more of the same letter.
    private static bool IsLongRun(ReadOnlySpan<char> text, int i)
    {
        int start = i;
        while (start > 0 && text[start - 1] == text[i])
        {
            start--;
        }

        int end = i;
        while (end + 1 < text.Length && text[end + 1] == text[i])
        {
            end++;
        }

        return end - start + 1 >= 3;
    }

    private static WordVerdict Better(WordVerdict a, WordVerdict b) => new(Better(a.Status, b.Status), a.Correction ?? b.Correction);

    private static WordStatus Better(WordStatus a, WordStatus b) => (WordStatus)Math.Min((int)a, (int)b);

    private static Shape ShapeOf(ReadOnlySpan<char> word)
    {
        bool anyUpper = false;
        bool anyLower = false;
        bool restLower = true;
        for (int i = 0; i < word.Length; i++)
        {
            if (char.IsUpper(word[i]))
            {
                anyUpper = true;
                restLower &= i == 0;
            }
            else if (char.IsLower(word[i]))
            {
                anyLower = true;
            }
        }

        if (!anyUpper)
        {
            return Shape.Lower;
        }

        if (!anyLower)
        {
            return Shape.AllUpper;
        }

        return char.IsUpper(word[0]) && restLower ? Shape.Capitalized : Shape.Mixed;
    }

    private enum Shape
    {
        Lower,
        Capitalized,
        AllUpper,
        Mixed,
    }
}
