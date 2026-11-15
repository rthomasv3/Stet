# FictionChecker (working title)

An offline spelling and grammar checker for fiction, written in pure managed C#.
Early development. Spelling and the first grammar rules (mechanics, agreement, dialogue punctuation) work through the public API below. Confusable words (their/there, its/it's) come next. The API can still change before 1.0.

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

## Rules

| ID | Finds | Default |
|---|---|---|
| `spelling.misspelling` | words not in the dictionary or your word lists | on |
| `spelling.unknown-name` | unknown capitalized words that look like names | on |
| `spelling.other-dialect` | spellings from another dialect ("colour" in American English) | on |
| `spelling.capitalization` | names written without their capital | on |
| `mechanics.repeated-word` | "the the" | on |
| `mechanics.a-an` | "a apple", "an unicorn", judged by sound | on |
| `mechanics.could-of` | "could of gone" (formal) | on |
| `mechanics.doubled-space` | two spaces inside a sentence | on |
| `mechanics.punctuation-spacing` | "the door , and", "up,, then", "dry . They" | on |
| `mechanics.sentence-capital` | "He left. she stayed." | on |
| `mechanics.lowercase-i` | "then i saw" | on |
| `agreement.pronoun-verb` | "I has", "they is" | on |
| `agreement.dialect-verb-form` | "they was", "he don't" (formal) | on |
| `agreement.verb-form` | "have went", "did not went", "should has" (formal) | on |
| `agreement.noun-verb` | "the children was", "there is many" (formal) | on |
| `dialogue.full-stop-before-tag` | "Hello." she said. | on |
| `dialogue.capitalized-tag` | "Hello," She said. | on |
| `dialogue.missing-comma` | "Hello" she said. | on |
| `dialogue.comma-outside-quote` | "Hello", she said. | on |
| `confusable.their-there` | their, there, they're mixed up | on |
| `confusable.its-its` | its, it's mixed up | on |
| `confusable.your-youre` | your, you're mixed up | on |
| `confusable.whose-whos` | whose, who's mixed up | on |
| `confusable.then-than` | then, than mixed up | on |
| `confusable.lose-loose` | lose, loose mixed up | on |
| `confusable.affect-effect` | affect, effect mixed up | on |
| `confusable.accept-except` | accept, except mixed up | on |
| `confusable.to-too` | to, too mixed up | on |
| `confusable.of-off` | of, off mixed up | on |
| `confusable.lead-led` | lead, led mixed up | on |
| `confusable.past-passed` | past, passed mixed up | on |
| `confusable.breath-breathe` | breath, breathe mixed up | on |
| `confusable.quite-quiet` | quite, quiet mixed up | on |
| `confusable.peek-peak` | peek, peak, pique mixed up | on |
| `confusable.wander-wonder` | wander, wonder mixed up | on |
| `confusable.desert-dessert` | desert, dessert mixed up | on |
| `confusable.brake-break` | brake, break mixed up | on |
| `confusable.principal-principle` | principal, principle mixed up | on |
| `confusable.compliment-complement` | compliment, complement mixed up | on |
| `confusable.whether-weather` | whether, weather mixed up | on |
| `confusable.here-hear` | here, hear mixed up | on |
| `confusable.bear-bare` | bear, bare mixed up | on |
| `confusable.advice-advise` | advice, advise mixed up | on |
| `confusable.week-weak` | week, weak mixed up | on |
| `confusable.know-no` | know, no mixed up | on |
| `confusable.piece-peace` | piece, peace mixed up | on |

The confusable-word rules flag a word only when the words on either side show strongly that another was
meant ("taller then her", "their was"), judged from how often each word is seen in those surroundings in
published fiction. They are tuned to stay quiet when unsure, so they miss swaps that only the wider
sentence gives away ("a matter of principal").

Formal rules are skipped inside dialogue unless dialogue strictness is set to strict, because writers break
them on purpose for a character's voice. `checker.Rules` gives the same list with descriptions and examples.

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
