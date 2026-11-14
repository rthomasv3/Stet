# Evaluation harness

Measures a checker on fiction: false alarms on clean text, and how many planted errors it catches
and fixes. It exists before the checker does, so every change can be compared with the last run.

## Corpora

The harness reads *prepared* corpora: a folder with `manifest.json` (book ids, split, word counts)
and one text file per book with one paragraph per line. Preparing them (downloading, parsing ePubs,
skipping front and back matter) is done by local tooling that is not part of this repository, and
**no book text is ever committed**.

| Corpus | What | Use |
|---|---|---|
| `gutenberg` | 40 Project Gutenberg novels across adventure, fantasy, science fiction, mystery and general fiction. Every author died before 1956, so the texts are public domain worldwide. Fetched from a mirror; Gutenberg headers and footers removed. | Measuring. May also be used to build data, when recorded in `SOURCES.md`. |
| `calibre` | A fixed sample of 24 modern, copyrighted novels from a local library (at most two per author). | Measuring only, on this machine. |

Corpus folders are set in `eval/eval.local.json` (not committed; copy `eval.local.example.json`).

### Development and test split

Each book is on one side: **dev** (may be looked at and tuned against) or **test** (measured only).
The split is by author, so one author's books are never on both sides, and about 30% of authors are
test. For `gutenberg` the test authors are picked within each genre so every genre is on both sides.

## What a run checks

Set in `eval/eval.json`, and the same every run:

- **Clean text:** one run of consecutive paragraphs per book, about 30,000 words.
- **Planted errors:** 500 per category for each corpus and split, planted into those same paragraphs,
  one error per paragraph. Categories: `typo`, `confusable`, `a-an`, `repeated-word`, `agreement`,
  `dialogue-punctuation`.

A planted error counts as caught when a flag touches the planted region and the clean paragraph did
not already have that flag. A fix counts as right when applying it gives back the original text.

## Running

    dotnet run --project eval/FictionChecker.Eval -- run --checker none
    dotnet run --project eval/FictionChecker.Eval -- run --checker none --compare harper

Each book carries the spelling it uses (US or GB, detected from the text), and checkers are told it.

Checkers:

- `spelling`: the library's spelling checker with no custom words, so every name and coined word is
  flagged. The worst case, and the view that shows what the checker itself does not know.
- `spelling-world`: the same, with a stand-in for the word list a writer would have. Every unknown word
  a book uses twice or more is treated as a custom word for that book, on the reasoning that a spelling
  repeated exactly is intended. Words used once stay flagged. This is a simulation, not a real word list.
- `none` (flags nothing), `demo` (a naive repeated-word check that exercises the harness), and
  `external` (scores issues produced outside the process; see `harper/` and `languagetool/`).

Speed against the targets in the requirements (run in Release):

    dotnet run -c Release --project eval/FictionChecker.Eval -- perf

To measure the library's text splitting on whole books (share of words in speech, quote marks that
could not be paired, speed):

    dotnet run --project eval/FictionChecker.Eval -- dialogue

To measure how much of the books' vocabulary the spelling data knows:

    dotnet run --project eval/FictionChecker.Eval -- coverage

## Reports

- `reports/dialogue.md`: text splitting and dialogue detection numbers.
- `reports/coverage.md`: unknown words per 1,000 words with the current spelling data.
- `reports/perf.md`: speed and memory of the spelling checker.

- `reports/<checker>.json` and `.md`: numbers only, committed. The Markdown shows changes against the
  previous run of the same checker and, with `--compare`, another checker's numbers alongside.
- `reports/local/`: flags and missed errors with text snippets, for review. Never committed.
- `labels.json` (optional): review verdicts, stored by paragraph hash and position rather than text.
  A flag labelled `real-error` is a genuine mistake in the published book and is not counted as a
  false alarm.

## Known limits

- Typos are planted without a dictionary, so a few are accidentally real words.
- Suggestion quality is measured on planted typos only; the curated misspelling list arrives with the
  spelling data.
- Planted paragraphs are checked on their own, without the paragraphs before them.
