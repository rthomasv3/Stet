# Stet

[![NuGet](https://img.shields.io/nuget/v/Stet.svg)](https://www.nuget.org/packages/Stet)
[![CI](https://github.com/rthomasv3/Stet/actions/workflows/ci.yml/badge.svg)](https://github.com/rthomasv3/Stet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/rthomasv3/Stet/blob/main/LICENSE)

**A spelling and grammar checker for fiction, as a .NET library.** It runs inside your app, offline,
with nothing to install beside it.

General-purpose checkers are built for emails and reports. On a novel they raise far more false alarms,
and they have no idea what dialogue is. Stet is built for the text novelists actually write. "Stet" is
the proofreader's mark for "let it stand".

```csharp
var checker = new Checker();
ParagraphResult result = checker.CheckParagraph("“I dont beleive it.” she said, and than she left.");
```

```
dont -> don't        “dont” is not a known word.
beleive -> believe   “beleive” is not a known word.
. -> ,               Speech followed by its tag ends with a comma, not a full stop.
than -> then         “than” looks like it should be “then”.
```

## Why Stet

- **It understands fiction.** It knows speech from narration. Inside dialogue it accepts "gonna",
  "runnin'" and "'em" while still catching real typos, and it checks the punctuation around speech.
  Harper, the checker it was measured against, caught almost none of those errors.
- **It handles a writer's own words properly.** Add a name and its possessive is covered too. Mark a
  word as valid only in dialogue, for a character's slang.
- **Few false alarms.** A checker that flags correct prose gets switched off. On published novels Stet
  raises about 2 flags per 1,000 words, where a general-purpose checker given the same word list
  raised 13. On those same books Stet caught 95% of planted typos and 93% of planted dialogue
  punctuation errors.
  [See the results.](https://github.com/rthomasv3/Stet/blob/main/eval/RESULTS.md)
- **Private.** Everything happens in your app's own process. Stet makes no network calls, reads and
  writes no files, and sends the writer's text nowhere.
- **Fast.** It loads in milliseconds and checks a whole novel in about a tenth of a second.
  [See below.](#speed)
- **Self-contained.** One 3.5 MB assembly with its data inside. No dependencies, no native code, no
  dictionary files to ship, and safe for Native AOT.
- **Dialects and quote styles.** American, British (-ise or -ize), Canadian and Australian spelling.
  Double or single quotation marks for speech.

## Install

```
dotnet add package Stet
```

Stet needs .NET 10 or later.

## Quick start

```csharp
using Stet;

// Create one checker and keep it. It is safe to share between threads.
var checker = new Checker();

ParagraphResult result = checker.CheckParagraph("“I dont beleive it.” she said, and than she left.");

foreach (Issue issue in result.Issues)
{
    Console.WriteLine($"{issue.Text} -> {issue.Suggestions[0].Text}   ({issue.Message})");
}
```

Each `Issue` says where it is (`Start` and `Length`, counted in `char`s from the start of the
paragraph), which rule found it, a short `Message`, a longer `Explanation`, and its `Suggestions`.

## Using it in an app

### Names and invented words

Your app keeps the writer's words; Stet is handed them as named sets. Use as many sets as you like, for
example one per book and one for the writer's own dictionary.

```csharp
checker.SetWords("world", ["Kaelith Vorn", "Drathmoor"]);   // replaces the whole set
checker.AddWords("world", ["Ysolde"]);                      // adds to it

// Words that are only right when a character says them.
checker.SetWords("slang", ["wicious"], WordScope.DialogueOnly);
```

A name of several words covers each word in it, so "Vorn" on its own is known too. Sets can change
while checks are running.

### A whole document

Give `CheckDocument` the paragraphs in order. Speech that carries over from one paragraph to the next
is followed, so a paragraph that continues a speech is still treated as dialogue.

```csharp
IReadOnlyList<ParagraphResult> results = checker.CheckDocument(paragraphs);
```

To check one paragraph at a time, pass on what the last one ended with:

```csharp
var context = new ParagraphContext();
foreach (string paragraph in paragraphs)
{
    ParagraphResult result = checker.CheckParagraph(paragraph, context);
    context = context with
    {
        StartsInDialogue = result.EndsInDialogue,
        StartsMidSentence = result.EndsMidSentence,
    };
}
```

### Dialect, quote marks and dialogue

Every check can take its own options, so one checker serves books with different settings.

```csharp
var options = new CheckerOptions
{
    Dialect = Dialect.British,                        // American, British, BritishIze, Canadian, Australian
    QuoteStyle = QuoteStyle.Single,                   // ‘Hello,’ she said. Leave unset to follow the dialect.
    DialogueStrictness = DialogueStrictness.Lenient,  // Strict, Lenient or Off
};

var results = checker.CheckDocument(paragraphs, options);
```

`DialogueStrictness` decides how speech is treated:

| | Inside dialogue |
|---|---|
| `Lenient` (the default) | Real typos are caught. Informal speech and a character's grammar are accepted. |
| `Strict` | Checked exactly like narration. |
| `Off` | Not checked at all. |

### Checking while the writer types

Wrap the checker in a `CheckCache` and call the same methods on it. Paragraphs that have not changed
return the result they had, so rechecking a whole novel after one edit takes about 2 ms.

```csharp
var cache = new CheckCache(checker);

var results = cache.CheckDocument(paragraphs);   // checks everything
paragraphs[12] = edited;
results = cache.CheckDocument(paragraphs);       // checks only what changed
```

Checks do no I/O and do not wait on anything, so run them on a background thread.

### Applying a fix

A suggestion says which text to replace and what with:

```csharp
Suggestion fix = issue.Suggestions[0];
string corrected = text[..fix.Start] + fix.Text + text[(fix.Start + fix.Length)..];
```

For a single word, as in a right-click menu:

```csharp
bool known = checker.IsValidWord("teh");                 // false
IReadOnlyList<string> choices = checker.Suggest("teh");  // the, ten, tea, ...
```

`issue.Kind` tells a misspelling from an unknown name, a spelling from another dialect, a missing
capital and a grammar problem, so your app can show each differently.

### Turning rules on and off

```csharp
var options = new CheckerOptions()
    .WithRule(RuleIds.UnknownName, false)
    .WithCategory(RuleCategory.DialoguePunctuation, false);
```

`checker.Rules` lists every rule with its ID, name, description and examples, ready for a settings
screen. Rule IDs that Stet does not recognize are ignored, so settings saved by an older or newer
version keep working.

### Text to leave alone

Skip a whole paragraph, such as a chapter title, or a range inside one, such as a web address:

```csharp
DocumentParagraph[] document =
[
    new("Chapter One") { Skip = true },
    new(text) { SkipRanges = [new TextSpan(start, length)] },
];

var results = checker.CheckDocument(document);
```

`DialogueRanges` does the opposite job for speech your app already knows about: it marks ranges as
dialogue whatever the quote marks say.

## What it checks

65 rules in five groups. Every one can be switched off.

| Group | Examples of what is flagged |
|---|---|
| **Spelling** | "beleive", an unknown name, "colour" in American English, "london" |
| **Mechanics** | "the the", "a apple", "could of gone", "then i saw", a sentence starting in lowercase |
| **Agreement** | "I has", "they was", "have went", "the children was" |
| **Dialogue punctuation** | “Hello.” she said. · “Hello,” She said. · “Hello” she said. · “Hello”, she said. |
| **Confusable words** | their / there / they're, its / it's, then / than, and 41 more sets |

[The full list of rules](https://github.com/rthomasv3/Stet/blob/main/RULES.md), with an example for each.

## Speed

On a public-domain novel, with a word list for its names:

| | .NET 10 | Native AOT |
|---|---:|---:|
| Create the first checker in a process | 44 ms | 13 ms |
| Check a 1,500-word scene | 4.3 ms | 1.8 ms |
| Check a 5,000-word chapter | 11 ms | 4.6 ms |
| Check a 100,000-word novel | 80 ms | 87 ms |

Measured on an AMD Ryzen 9 9950X under Fedora Linux 44, on a single thread. A loaded checker holds
about 17 MB. [All the benchmarks, and how to run
them.](https://github.com/rthomasv3/Stet/blob/main/bench/README.md)

## Accuracy

Measured on 19 novels that Stet was never tuned against, beside
[Harper](https://github.com/Automattic/harper) (`harper.js` 3.2.0), another offline checker:

| | Stet | Harper |
|---|---:|---:|
| False alarms per 1,000 words of published text | 1.89 | 13.05 |
| The same, counting spelling flags only | 1.80 | 4.66 |
| Planted typos caught | 95.5% | 87.6% |
| Planted confusable words caught | 67.1% | 36.2% |
| Planted dialogue punctuation errors caught | 92.6% | 0.2% |

Both were given the same word list for each book, standing in for the names a writer would add.
Harper also checks style and readability, which Stet does not attempt; with those flags left out its
first figure is 7.67. With no word list at all, the two are about level on spelling.
[The full results, the method and its limits.](https://github.com/rthomasv3/Stet/blob/main/eval/RESULTS.md)

## What it does not do

- **English only.**
- **No style advice.** Stet does not comment on long sentences, passive voice, adverbs or word choice.
  It looks for mistakes, not for taste.
- **It prefers silence to a guess.** A confusable word is flagged only when the words beside it make
  the mistake clear, so "a matter of principal" gets through. About a third of planted confusable-word
  and agreement errors are missed.
- **This is an early release.** The API may still change before 1.0.

## Where Stet is going

Stet is a library today. The goal is for it to become a lightweight checker you can run anywhere.

- **Better results, still lightweight.** Larger and better word lists, and three-word context alongside
  the two-word context used now, trimmed the same way so the data stays small.
- **A server you can host yourself.** An HTTP service around the library, far smaller and simpler to run
  than the alternatives, so tools not written in .NET can use it.
- **A browser extension**, running Stet directly in the browser and/or through a self-hosted server.

The project is still fairly early, so treat these more as goals than as a concrete plan with dates.

## Building from source

```
dotnet build
dotnet test
```

| Folder | What is in it |
|---|---|
| `src/Stet` | The library |
| `src/Stet.DataBuilder` | Rebuilds the built-in data from its sources |
| `tests` | Unit tests, and a small app published with Native AOT as a check |
| `eval` | The evaluation harness and its results |
| `bench` | Benchmarks |

## Data and license

Stet is released under the [MIT license](https://github.com/rthomasv3/Stet/blob/main/LICENSE).

Its built-in word lists and statistics are derived from SCOWL, the Google Books Ngram data, the CMU
Pronouncing Dictionary and the System Reference Document 5.2.1, all under permissive licenses.
[SOURCES.md](https://github.com/rthomasv3/Stet/blob/main/SOURCES.md) records each one, and
[THIRD-PARTY-NOTICES.md](https://github.com/rthomasv3/Stet/blob/main/THIRD-PARTY-NOTICES.md) holds the
notices they require. An app that ships Stet should include those notices.
