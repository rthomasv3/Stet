using System.Diagnostics.CodeAnalysis;
using FictionChecker.Segmentation;

namespace FictionChecker;

/// <summary>
/// Remembers results per paragraph so that only changed paragraphs are checked again. Use it in place of the
/// checker for a document that is checked repeatedly as it is edited.
///
/// A result is reused only when everything that affects it is the same: the paragraph's text, the options,
/// the word sets in use, the speech found in the paragraph and the skip ranges. Changing a word set or an
/// option therefore never returns a stale result, and results made with a changed word set are dropped. The
/// cache holds a limited number of paragraphs and drops the least recently used. Safe to use from several
/// threads at once.
/// </summary>
public sealed class CheckCache
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Key, LinkedListNode<(Key Key, ParagraphResult Result)>> _entries = [];
    private readonly LinkedList<(Key Key, ParagraphResult Result)> _order = new();
    private object _wordsVersion;

    /// <summary>Creates a cache over a checker.</summary>
    /// <param name="checker">The checker that does the checking.</param>
    /// <param name="capacity">Most paragraphs remembered. A 100,000-word novel has about 4,000.</param>
    public CheckCache(Checker checker, int capacity = 10_000)
    {
        ArgumentNullException.ThrowIfNull(checker);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Checker = checker;
        Capacity = capacity;
        _wordsVersion = checker.WordsVersion;
    }

    /// <summary>The checker that does the checking.</summary>
    public Checker Checker { get; }

    /// <summary>Most paragraphs remembered.</summary>
    public int Capacity { get; }

    /// <summary>Paragraphs remembered now.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _entries.Count;
            }
        }
    }

    /// <summary>Checks one paragraph, reusing a remembered result when nothing that affects it has changed.</summary>
    /// <inheritdoc cref="Checker.CheckParagraph(string, ParagraphContext?)" path="/param"/>
    public ParagraphResult CheckParagraph(string text, ParagraphContext? context = null) => Checker.CheckParagraph(text, context, this);

    /// <summary>Checks consecutive paragraphs, reusing remembered results for those that have not changed.</summary>
    /// <inheritdoc cref="Checker.CheckDocument(IReadOnlyList{string}, CheckerOptions?, CancellationToken)" path="/param"/>
    public IReadOnlyList<ParagraphResult> CheckDocument(IReadOnlyList<string> paragraphs, CheckerOptions? options = null, CancellationToken cancellationToken = default) =>
        Checker.CheckDocument(paragraphs, options, cancellationToken, this);

    /// <summary>Checks consecutive paragraphs with ranges, reusing remembered results for those that have not changed.</summary>
    /// <inheritdoc cref="Checker.CheckDocument(IReadOnlyList{DocumentParagraph}, CheckerOptions?, CancellationToken)" path="/param"/>
    public IReadOnlyList<ParagraphResult> CheckDocument(IReadOnlyList<DocumentParagraph> paragraphs, CheckerOptions? options = null, CancellationToken cancellationToken = default) =>
        Checker.CheckDocument(paragraphs, options, cancellationToken, this);

    /// <summary>Forgets every remembered result.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
            _order.Clear();
        }
    }

    internal bool TryGet(Key key, [NotNullWhen(true)] out ParagraphResult? result)
    {
        lock (_gate)
        {
            DropStale();
            if (_entries.TryGetValue(key, out LinkedListNode<(Key Key, ParagraphResult Result)>? node))
            {
                _order.Remove(node);
                _order.AddFirst(node);
                result = node.Value.Result;
                return true;
            }
        }

        result = null;
        return false;
    }

    internal void Add(Key key, ParagraphResult result)
    {
        lock (_gate)
        {
            if (_entries.TryGetValue(key, out LinkedListNode<(Key Key, ParagraphResult Result)>? existing))
            {
                existing.Value = (key, result);
                return;
            }

            while (_entries.Count >= Capacity)
            {
                LinkedListNode<(Key Key, ParagraphResult Result)> oldest = _order.Last!;
                _order.RemoveLast();
                _entries.Remove(oldest.Value.Key);
            }

            _entries[key] = _order.AddFirst((key, result));
        }
    }

    // After a word set changes, results made with word sets that no longer exist can never match again.
    private void DropStale()
    {
        if (ReferenceEquals(_wordsVersion, Checker.WordsVersion))
        {
            return;
        }

        (object version, HashSet<long> live) = Checker.LiveStates();
        _wordsVersion = version;
        LinkedListNode<(Key Key, ParagraphResult Result)>? node = _order.First;
        while (node is not null)
        {
            LinkedListNode<(Key Key, ParagraphResult Result)>? next = node.Next;
            if (!live.Contains(node.Value.Key.StateId))
            {
                _entries.Remove(node.Value.Key);
                _order.Remove(node);
            }

            node = next;
        }
    }

    /// <summary>Everything a paragraph's result depends on.</summary>
    internal sealed class Key : IEquatable<Key>
    {
        private readonly string _text;
        private readonly string _options;
        private readonly TextSpan[] _dialogue;
        private readonly TextSpan[] _skip;
        private readonly bool _endsInDialogue;
        private readonly bool _continues;
        private readonly int _hash;

        public Key(string text, long stateId, string options, DialogueResult dialogue, IReadOnlyList<TextSpan>? skipRanges, bool continues = false)
        {
            _text = text;
            StateId = stateId;
            _options = options;
            _dialogue = [.. dialogue.Spans];
            _skip = skipRanges is null ? [] : [.. skipRanges];
            _endsInDialogue = dialogue.EndsInDialogue;
            _continues = continues;
            var hash = default(HashCode);
            hash.Add(text, StringComparer.Ordinal);
            hash.Add(stateId);
            hash.Add(options, StringComparer.Ordinal);
            hash.Add(_endsInDialogue);
            hash.Add(continues);
            foreach (TextSpan span in _dialogue)
            {
                hash.Add(span);
            }

            hash.Add(_skip.Length);
            _hash = hash.ToHashCode();
        }

        public long StateId { get; }

        public bool Equals(Key? other) =>
            other is not null
            && _hash == other._hash
            && StateId == other.StateId
            && _endsInDialogue == other._endsInDialogue
            && _continues == other._continues
            && string.Equals(_options, other._options, StringComparison.Ordinal)
            && string.Equals(_text, other._text, StringComparison.Ordinal)
            && _dialogue.AsSpan().SequenceEqual(other._dialogue)
            && _skip.AsSpan().SequenceEqual(other._skip);

        public override bool Equals(object? obj) => Equals(obj as Key);

        public override int GetHashCode() => _hash;
    }
}
