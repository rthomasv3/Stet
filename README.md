# Stet

A spelling and grammar checker built for fiction. Offline, pure managed C#, no native code, Native AOT safe.

"Stet" is the proofreader's mark for "let it stand". The library is tuned to stay quiet on correct prose:
it accepts a character's voice in dialogue, takes the writer's own names and coined words, checks the
punctuation around speech, and flags a word only when the evidence that it is wrong is strong.

Early development. Spelling and the grammar rules listed below (mechanics, agreement, dialogue punctuation,
confusable words) work through the public API. The API can still change before 1.0.

## Using it

```csharp
using Stet;

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

- **One paragraph at a time:** `CheckParagraph(text, context)`. Pass `ParagraphResult.EndsInDialogue` on as the next paragraph's `ParagraphContext.StartsInDialogue`. Do the same with `EndsMidSentence` and `StartsMidSentence`, so a paragraph that carries on an unfinished one (after a line of verse or an inserted letter) is not flagged for starting without a capital. The context also takes ranges to skip and ranges to treat as dialogue.
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
| `mechanics.double-comparative` | "more stronger", "most quickest" (formal) | on |
| `mechanics.missing-apostrophe` | "wont tell", "cant have" | on |
| `mechanics.doubled-space` | two spaces inside a sentence | on |
| `mechanics.punctuation-spacing` | "the door , and", "up,, then", "dry . They" | on |
| `mechanics.sentence-capital` | "He left. she stayed." | on |
| `mechanics.lowercase-i` | "then i saw" | on |
| `agreement.pronoun-verb` | "I has", "they is" | on |
| `agreement.dialect-verb-form` | "they was", "he don't" (formal) | on |
| `agreement.verb-form` | "have went", "did not went", "didn't had", "should has" (formal) | on |
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
| `confusable.allowed-aloud` | allowed, aloud mixed up | on |
| `confusable.throne-thrown` | throne, thrown mixed up | on |
| `confusable.stake-steak` | stake, steak mixed up | on |
| `confusable.waist-waste` | waist, waste mixed up | on |
| `confusable.bored-board` | bored, board mixed up | on |
| `confusable.whole-hole` | whole, hole mixed up | on |
| `confusable.weight-wait` | weight, wait mixed up | on |
| `confusable.write-right` | write, right mixed up | on |
| `confusable.plain-plane` | plain, plane mixed up | on |
| `confusable.role-roll` | role, roll mixed up | on |
| `confusable.sight-site` | sight, site mixed up | on |
| `confusable.soul-sole` | soul, sole mixed up | on |
| `confusable.tail-tale` | tail, tale mixed up | on |
| `confusable.steal-steel` | steal, steel mixed up | on |
| `confusable.heal-heel` | heal, heel mixed up | on |
| `confusable.pour-pore` | pour, pore mixed up | on |
| `confusable.rein-reign` | rein, reign, rain mixed up | on |

The confusable-word rules flag a word only when the words on either side show strongly that another was
meant ("taller then her", "their was"), judged from how often each word is seen in those surroundings in
published fiction. They are tuned to stay quiet when unsure, so they miss swaps that only the wider
sentence gives away ("a matter of principal").

Formal rules are skipped inside dialogue unless dialogue strictness is set to strict, because writers break
them on purpose for a character's voice. `checker.Rules` gives the same list with descriptions and examples.

## Layout

- `src/Stet` - the library (no runtime dependencies, Native AOT safe)
- `src/Stet.DataBuilder` - builds the embedded data files from recorded sources
- `tests/Stet.Tests` - unit tests
- `tests/Stet.AotTest` - small app published with Native AOT as a check
- `eval/` - evaluation harness (corpora stay outside the repo)
- `bench/` - benchmarks

## Commands

    dotnet build
    dotnet test
    dotnet publish tests/Stet.AotTest -c Release -o aot-out && ./aot-out/Stet.AotTest
    dotnet run -c Release --project bench/Stet.Bench -- --filter '*'

## Licensing

MIT. Data sources are recorded in `SOURCES.md` and credited in `THIRD-PARTY-NOTICES.md`.
