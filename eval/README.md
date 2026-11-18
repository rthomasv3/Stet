# Evaluation harness

A command-line tool that measures a spelling and grammar checker on fiction. It answers two questions:

- **How often does the checker flag text that is correct?** It runs the checker over published books
  and counts flags per 1,000 words.
- **How many real mistakes does it catch?** It plants known errors in the same text and counts how
  many are flagged, and how often the suggested fix is the right one.

It is how the numbers in this repository were produced. It is not part of the NuGet package.

## What you need

The harness needs books, and none are included: **this repository holds no book text.** You supply
your own as one or more *prepared corpora*. Two commands need no books at all; see
[Without books](#without-books).

A prepared corpus is a folder with:

- one UTF-8 text file per book, holding the story only (no front or back matter), one paragraph per line
- a `manifest.json` that lists the books:

```json
{
  "name": "my-books",
  "books": [
    { "id": "book-one", "file": "book-one.txt", "split": "dev", "dialect": "US", "genre": "fantasy" },
    { "id": "book-two", "file": "book-two.txt", "split": "test", "dialect": "GB" }
  ]
}
```

| Field | Meaning |
|---|---|
| `id` | A name for the book, used in reports. |
| `file` | The book's text file, relative to the folder. |
| `split` | `dev` or `test`. See [Development and test books](#development-and-test-books). |
| `dialect` | The spelling the book uses, `US` or `GB`. Optional, `US` when left out. Each checker is told it. |
| `genre` | Optional label. |

Tell the harness where your corpora are by copying `eval.local.example.json` to `eval.local.json`
(which git ignores) and listing each one by a name of your choice:

```json
{
  "corpora": {
    "my-books": "/path/to/my-books"
  }
}
```

Reports give their numbers per corpus, under these names.

### Development and test books

Every book is on one side:

- **dev** books may be read, and the checker may be tuned against them.
- **test** books are only ever measured. Nobody looks at what was flagged in them, so their numbers
  show how the checker does on text it was not tuned for.

## What a run measures

The settings are in `eval.json` and are the same for every run, so runs can be compared.

- **Clean text:** from each book, one stretch of consecutive paragraphs of about 30,000 words.
- **Planted errors:** 500 per category, for each corpus and each side of the split, planted into those
  same paragraphs with one error per paragraph. The categories are `typo`, `confusable`, `a-an`,
  `repeated-word`, `agreement` and `dialogue-punctuation`.

A planted error counts as caught when a flag touches the planted text and the unchanged paragraph did
not already have that flag. A fix counts as right when applying it gives back the original text.

## Running it

From the repository root:

    dotnet run -c Release --project eval/Stet.Eval -- run --checker stet

`--checker` chooses what is measured:

| Checker | What it is |
|---|---|
| `stet` | Stet at its defaults, spelling and grammar. What an app gets. |
| `stet-all` | The same with every rule switched on, including any that are off by default. |
| `spelling` | Spelling only, with no custom words, so every name and invented word is flagged. The worst case. |
| `spelling-world` | Spelling only, with a stand-in for the writer's word list (see below). |
| `none` | Flags nothing. A check that the harness itself works. |
| `external` | Scores issues produced by another program. See [Other checkers](#other-checkers). |

**The stand-in word list.** A writer using Stet in an app adds their character and place names to a
word list. Books do not come with one, so `spelling-world`, `stet` and `stet-all` build a
stand-in: every unknown word a book uses twice or more is treated as a custom word for that book, on
the reasoning that a spelling repeated exactly is intended. Words used once stay flagged. This is a
simulation of a word list, not a real one.

When another checker is measured beside Stet it is given the same lists: `export --word-lists FILE`
writes them. Because the lists hold the words *Stet* does not know, the `names` command then adds the
names the other checker does not know and the lists lack, and `--extra-words FILE` gives those to
Stet's run and to the export alike. The [Harper readme](harper/README.md) walks through it.

Add `--compare <checker>` to put another checker's last report alongside, for example `--compare harper`.

More commands, each measuring one thing on whole books:

| Command | Measures |
|---|---|
| `perf` | Speed and memory, against the targets set for the project. Run it with `-c Release`. |
| `dialogue` | Text splitting: the share of words in speech, and quote marks that could not be paired. |
| `coverage` | How much of the books' vocabulary the spelling data knows. |

### Without books

    dotnet run -c Release --project eval/Stet.Eval -- sideset
    dotnet run -c Release --project eval/Stet.Eval -- confusable-explain "She was taller then her brother."

- `sideset` runs Stet over the small made-up set in [`sideset/`](sideset/README.md): sentences with one
  error each, and correct lines of the kind that trip checkers.
- `confusable-explain` shows the evidence behind a confusable-word decision for any sentence you give it.

### Other checkers

The harness can score any checker. `export` writes every paragraph to be checked to a file, the other
checker writes its issues to a second file, and `run --checker external` scores them. Two runners are
included:

- [`harper/`](harper/README.md) for [Harper](https://github.com/Automattic/harper)
- [`languagetool/`](languagetool/README.md) for a [LanguageTool](https://languagetool.org) server you run yourself

The exported file holds book text, so it is written to `reports/local/`, which git ignores.

## Reports

Each run writes `reports/<checker>.md` and `reports/<checker>.json`. They hold numbers only, never
text. If the same checker has been run before, the run also prints what changed since then.

Each run also writes a sample of its flags, with the text around each, to `reports/local/`, for reading
through the results: up to 40 per rule, from dev books only (`--review-flags N` changes the number). That
folder holds book text and is ignored by git.

Git keeps only the six published reports and ignores the rest of `reports/`, so your own runs of other
checkers stay on your machine.

**Real errors in the books.** Published books contain a few genuine mistakes, and a flag on one is
counted as a false alarm like any other. To leave reviewed ones out of the count, list them in
`eval/labels.json` (ignored by git): each entry gives the paragraph's `hash` as shown in the local
review file, the flag's `start`, `length` and `ruleId`, and `"verdict": "real-error"`. The published
numbers use no labels.

## The published numbers

[RESULTS.md](RESULTS.md) gives the results for Stet 0.0.1 and says what books and what machine they
came from. The reports behind it are in [`reports/`](reports/):

| Report | What it is |
|---|---|
| [`stet`](reports/stet.md) | Stet at its defaults, with each book's word list, and Harper alongside |
| [`harper-words`](reports/harper-words.md) | Harper at its defaults, with the same word lists |
| [`harper-words-inscope`](reports/harper-words-inscope.md) | The same, with Harper's style and readability flags left out |
| [`spelling`](reports/spelling.md) | Stet's spelling only, with no word list |
| [`harper`](reports/harper.md) | Harper at its defaults, with no word list |
| [`harper-inscope`](reports/harper-inscope.md) | The same, with Harper's style and readability flags left out |

## Known limits

- Typos are planted without a dictionary, so a few are accidentally real words.
- Suggestion quality is measured on planted typos only.
- Planted paragraphs are checked on their own, without the paragraphs before them.
