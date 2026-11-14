namespace FictionChecker.Segmentation;

/// <summary>What a token is.</summary>
internal enum TokenKind
{
    /// <summary>Letters, possibly with apostrophes or hyphens inside (don't, well-known, Haydée).</summary>
    Word,

    /// <summary>Anything containing a digit: numbers, ordinals (21st), decades (1920s), codes (R2D2). Not spell-checked.</summary>
    Number,

    /// <summary>A single capital letter used as an initial (the J in "J. R. R. Tolkien"). Not spell-checked.</summary>
    Initial,

    /// <summary>A web address. Not spell-checked.</summary>
    Url,

    /// <summary>An email address. Not spell-checked.</summary>
    Email,

    /// <summary>A punctuation mark, or a run that acts as one mark (an ellipsis of full stops, a double hyphen).</summary>
    Punctuation,
}

/// <summary>Extra facts about a token.</summary>
[Flags]
internal enum TokenFlags
{
    /// <summary>Nothing special.</summary>
    None = 0,

    /// <summary>An apostrophe or single quote mark sits directly before the word ('tis, or an opening quote).</summary>
    LeadingApostrophe = 1,

    /// <summary>An apostrophe or single quote mark sits directly after the word (runnin', boys', or a closing quote).</summary>
    TrailingApostrophe = 2,

    /// <summary>The word contains a hyphen.</summary>
    Hyphenated = 4,

    /// <summary>The token lies in a range the caller asked to skip.</summary>
    Skipped = 8,
}

/// <summary>A piece of a paragraph. Whitespace is not tokenized.</summary>
internal readonly record struct Token(int Start, int Length, TokenKind Kind, TokenFlags Flags = TokenFlags.None)
{
    /// <summary>Position just past the token.</summary>
    public int End => Start + Length;

    /// <summary>The token's range.</summary>
    public TextSpan Span => new(Start, Length);

    /// <summary>Whether a flag is set.</summary>
    public bool Has(TokenFlags flag) => (Flags & flag) != 0;

    /// <summary>The token's text.</summary>
    public ReadOnlySpan<char> TextIn(string paragraph) => paragraph.AsSpan(Start, Length);
}
