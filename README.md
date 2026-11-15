# FictionChecker (working title)

An offline spelling and grammar checker for fiction, written in pure managed C#.
Early development. Spelling works through the public API below; grammar rules come next. The API can still change before 1.0.

## Using it

```csharp
using FictionChecker;

// Create once and keep: this loads the built-in data (about 10 ms). Safe to share between threads.
var checker = new Checker(new CheckerOptions { Dialect = Dialect.American });

// The app owns its word lists and hands them over as named sets.
checker.SetWords("world", ["Kaelith Vorn", "Drathmoor"]);
checker.SetWords("slang", ["wicious"], WordScope.DialogueOnly);

// A scene, paragraph by paragraph. Open quotes carry from one paragraph to the next.
IReadOnlyList<ParagraphResult> results = checker.CheckDocument(paragraphs);
foreach (Issue issue in results[0].Issues)
{
    // Positions are UTF-16 units from the start of the paragraph.
    Console.WriteLine($"{issue.Start}+{issue.Length} {issue.RuleId}: {issue.Message}");
    foreach (Suggestion fix in issue.Suggestions)
    {
        Console.WriteLine($"  replace {fix.Start}+{fix.Length} with {fix.Text}");
    }
}
```

- **One paragraph at a time:** `CheckParagraph(text, context)`. Pass `ParagraphResult.EndsInDialogue` on as the next paragraph's `ParagraphContext.StartsInDialogue`. The context also takes ranges to skip and ranges to treat as dialogue.
- **Options per check:** every check accepts its own `CheckerOptions` (dialect, quote style, dialogue strictness, rules on or off, which word sets apply), so one checker serves books with different settings.
- **Rules:** `checker.Rules` lists every rule with its ID, name, description and examples. Turn one off with `options.WithRule(RuleIds.UnknownName, false)`, or a whole category with `options.WithCategory(RuleCategory.Spelling, false)`. Unknown IDs are ignored, so saved settings survive upgrades.
- **Single words:** `IsValidWord(word)` and `Suggest(word)`, for a right-click menu.
- **Checking while typing:** wrap the checker in a `CheckCache` and call the same methods on it. Unchanged paragraphs return their remembered result, so rechecking a whole document after an edit costs about as much as checking the edited paragraph.
- **Threads:** checks are synchronous and do no I/O. Run them on a background thread. Word sets can change while checks run.

## Layout

- `src/FictionChecker` - the library (no runtime dependencies, Native AOT safe)
- `src/FictionChecker.DataBuilder` - builds the embedded data files from recorded sources
- `tests/FictionChecker.Tests` - unit tests
- `tests/FictionChecker.AotTest` - small app published with Native AOT as a check
- `eval/` - evaluation harness (corpora stay outside the repo)
- `bench/` - benchmarks

## Commands

    dotnet build
    dotnet test
    dotnet publish tests/FictionChecker.AotTest -c Release -o aot-out && ./aot-out/FictionChecker.AotTest
    dotnet run -c Release --project bench/FictionChecker.Bench -- --filter '*'

## Licensing

MIT. Data sources are recorded in `SOURCES.md` and credited in `THIRD-PARTY-NOTICES.md`.
