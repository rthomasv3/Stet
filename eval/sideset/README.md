# Side set

A small set of made-up sentences for a quick look at what Stet catches. It needs no books:

    dotnet run -c Release --project eval/Stet.Eval -- sideset

Add `--detail` to list every line with what was flagged on it.

## What is in it

[`set.tsv`](set.tsv) has 112 lines, each a category and a sentence. None of it is from a book.

- **76 sentences with one error each.** The error is marked with `[[` and `]]`, as in
  `He waited [[untill]] the moon rose.` The marks are taken out before checking.
- **36 correct lines** in the category `CLEAN`, of the kind that trip checkers: invented names, a
  character's dialect, sentence fragments, unusual punctuation. Nothing on these should be flagged.

The error categories:

| Category | Sentences | What it holds |
|---|---:|---|
| `spelling` | 10 | Common misspellings |
| `confusable-listed` | 10 | Confusable words Stet has a rule for |
| `confusable-other` | 10 | More confusable words |
| `agreement` | 10 | Subject and verb that do not agree |
| `verb-form` | 8 | The wrong form of a verb |
| `articles-repeats` | 6 | a / an, and repeated words |
| `punctuation-caps` | 10 | Punctuation and capital letters |
| `dialogue-punct` | 8 | Punctuation around speech |
| `error-in-voice` | 4 | A real slip inside a character's dialect |

## What Stet 0.0.1 does on it

It catches 66 of the 76 errors and flags 2 of the 36 correct lines. Both of those lines are flagged
for invented names, which a writer's word list would hold.

## What this is not

This is a quick check, not a measurement. Ten sentences to a category is too few to draw anything
from, and some of Stet's rules were written with these sentences in view, so its score here flatters
it. The numbers that count are the ones measured on books, in [RESULTS.md](../RESULTS.md).

To add a line of your own, append it to `set.tsv` in the same form.
