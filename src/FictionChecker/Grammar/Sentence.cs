using FictionChecker.Segmentation;

namespace FictionChecker.Grammar;

/// <summary>
/// One sentence as rules see it: its tokens (words and punctuation) with each word's type and what is known
/// about it. Positions given to rules are token numbers within the sentence.
/// </summary>
internal sealed class Sentence
{
    private readonly Token[] _tokens;
    private readonly WordType[] _types;
    private readonly int[] _words;
    private readonly string?[] _lower;
    private readonly WordData _data;
    private readonly FunctionWords _functionWords;
    private readonly DialogueResult? _dialogue;

    public Sentence(string text, Token[] tokens, WordType[] types, int[] words, WordData data, FunctionWords functionWords, DialogueResult? dialogue)
    {
        Text = text;
        _tokens = tokens;
        _types = types;
        _words = words;
        _data = data;
        _functionWords = functionWords;
        _dialogue = dialogue;
        _lower = new string?[tokens.Length];
    }

    /// <summary>The whole paragraph. Token positions refer to it.</summary>
    public string Text { get; }

    /// <summary>Number of tokens.</summary>
    public int Count => _tokens.Length;

    public Token Token(int i) => _tokens[i];

    /// <summary>Whether the token is a word (not punctuation or a number) that the caller did not ask to skip.</summary>
    public bool IsWord(int i) => (uint)i < (uint)_tokens.Length && _tokens[i].Kind == TokenKind.Word && !_tokens[i].Has(TokenFlags.Skipped);

    public WordType Type(int i) => _types[i];

    /// <summary>The token as written.</summary>
    public ReadOnlySpan<char> Written(int i) => _tokens[i].TextIn(Text);

    /// <summary>The token in lowercase with straight apostrophes.</summary>
    public string Lower(int i)
    {
        if (_lower[i] is { } cached)
        {
            return cached;
        }

        Span<char> buffer = stackalloc char[Math.Min(_tokens[i].Length, 64)];
        ReadOnlySpan<char> plain = Tagger.Normalize(Written(i)[..buffer.Length], buffer);
        return _lower[i] = plain.ToString().ToLowerInvariant();
    }

    /// <summary>Whether the token is one of the given words, ignoring capitals.</summary>
    public bool Is(int i, params ReadOnlySpan<string> words)
    {
        if (!IsWord(i))
        {
            return false;
        }

        string lower = Lower(i);
        foreach (string word in words)
        {
            if (lower == word)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>What the function word list says about the token.</summary>
    public WordTraits Traits(int i) => IsWord(i) ? _functionWords.TraitsOf(Lower(i)) : WordTraits.None;

    /// <summary>How the word is formed from other words (plural, past, and so on).</summary>
    public WordForms Forms(int i) => _words[i] >= 0 ? _data.FormsOf(_words[i]) : WordForms.None;

    /// <summary>The word's position in the lexicon (as written, or in lowercase), or -1.</summary>
    public int WordIndex(int i) => _words[i];

    /// <summary>The word data, for rules that look words up themselves.</summary>
    public WordData Data => _data;

    /// <summary>Whether every letter of the token is a capital (an acronym or shouting).</summary>
    public bool IsAllCapitals(int i)
    {
        ReadOnlySpan<char> written = Written(i);
        bool any = false;
        foreach (char c in written)
        {
            if (char.IsLower(c))
            {
                return false;
            }

            any |= char.IsUpper(c);
        }

        return any;
    }

    /// <summary>Whether the token starts with a capital letter.</summary>
    public bool IsCapitalized(int i) => char.IsUpper(Text[_tokens[i].Start]);

    /// <summary>Whether the token lies inside speech.</summary>
    public bool InDialogue(int i) => _dialogue is not null && _dialogue.IsDialogue(_tokens[i].Start);

    /// <summary>Whether only spaces lie between a token and the next, so the two are side by side.</summary>
    public bool TouchesNext(int i)
    {
        if (i + 1 >= _tokens.Length)
        {
            return false;
        }

        ReadOnlySpan<char> between = Text.AsSpan(_tokens[i].End, _tokens[i + 1].Start - _tokens[i].End);
        return between.Length > 0 && between.IndexOfAnyExcept(' ', (char)0xA0, '	') < 0;
    }
}
