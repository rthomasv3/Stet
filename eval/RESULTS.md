# Results

How Stet 0.0.1 does on published fiction, measured with the [evaluation harness](README.md) in this
folder. The full reports are in [`reports/`](reports/).

## What was measured

Two things, on 64 novels:

- **False alarms.** Published text is taken to be correct, so every flag on it counts as a false
  alarm. The figure is flags per 1,000 words. A novel of 100,000 words at 2 per 1,000 has about 200.
- **Planted errors.** Known mistakes are put into the same text, one to a paragraph, and the figure is
  the share that were flagged.

The books:

| Name in the reports | Books |
|---|---|
| `classic` | 40 novels from Project Gutenberg, across adventure, fantasy, science fiction, mystery and general fiction. Every author died before 1956. |
| `modern` | 24 modern novels, at most two per author. They are under copyright, so they were used for measuring only: nothing in Stet's data is built from them, and none of their text is in this repository. |

Every book is on one side of a split. **Dev** books could be read and tuned against while Stet was
built. **Test** books were only ever measured. The split is by author, with about 30% of authors on
the test side, so no author has books on both.

**The figures below are for the 19 test books**, 544,292 words of clean text and 1,000 planted errors
of each kind. The dev figures are in the reports and are close to these.

## False alarms

| | Flags per 1,000 words |
|---|---:|
| Stet | 1.92 |
| Harper | 28.23 |
| Harper, style and readability flags left out | 22.85 |

Almost all of Stet's flags are spelling flags: words it does not know (1.34 per 1,000) and names it
does not know (0.35).

Stet was given a stand-in for the word list a writer would keep: every unknown word a book uses twice
or more was treated as one of that book's own words. Harper was given no word list, and about two
thirds of its flags are spelling flags. So this table shows a checker used the way a fiction writer
would use it beside one that has no way to learn a book's names.

With no word list either, and checking spelling only, Stet flags 18.33 words per 1,000. Harper's
spelling rule alone flags 19.28. On unknown names and invented words the two are about level; the
word list is what makes the difference.

## Planted errors caught

| Kind of error | Example | Stet | Harper |
|---|---|---:|---:|
| Typo | "recieve" | 95.4% | 87.2% |
| Confusable word | "taller then her" | 67.1% | 36.2% |
| a / an | "a apple" | 93.1% | 86.8% |
| Repeated word | "the the" | 80.6% | 63.4% |
| Agreement | "they was" | 63.0% | 58.1% |
| Dialogue punctuation | "Hello." she said. | 92.6% | 0.3% |

When Stet catches a typo, its first suggestion is the right word 89.1% of the time, and the right word
is in the first five 98.5% of the time. For Harper the first suggestion is right 73.1% of the time.

Harper caught almost none of the last kind.

## About the comparison

[Harper](https://github.com/Automattic/harper) is an open-source, offline grammar checker. It was run
as `harper.js` 3.2.0 with its default rules, in plain-text mode, with the dialect matched to each book.
These figures are for that version on these books and say nothing about any other.

**Why Harper.** It is the nearest thing to Stet that already exists: a light, offline checker that runs
inside an app with no server behind it. Stet's author had considered it for another project, which is
how it became the yardstick. It is written in Rust and usually used from JavaScript, so it is not an
alternative for a .NET app, only a measure of what a checker of this kind does on fiction.

Harper is a general-purpose checker and is not built for fiction. It also checks things Stet does not
attempt, such as style and readability; the second Harper row above leaves those flags out. How to
repeat the run is in [`harper/`](harper/README.md).

## Speed and size

| | |
|---|---:|
| Create a checker, first time in a process | 42 ms |
| Create a checker, after that | 12 ms |
| Check a 100,000-word novel | 0.2 to 0.4 s |
| Check a 1,500-word scene | 2 to 5 ms |
| Check one paragraph | under 0.1 ms |
| Suggestions for one word | about 0.5 ms |
| Recheck a novel after one paragraph changed, using the cache | 2 to 3 ms |
| Memory held by a loaded checker | about 17 MB |
| Size of the assembly, data included | 3.5 MB |

Scene, paragraph and suggestion times are medians. Everything was measured on the first 100,000 words of the longest book in each set, on:

- AMD Ryzen 9 9950X (16 cores, 32 threads), 64 GB of memory
- Fedora Linux 44
- .NET 10

Stet checks on a single thread, so the number of cores does not help. Expect slower times on a laptop.

## Limits of these numbers

- Planted errors are made by rule, and real mistakes are more varied. A catch rate here is a guide to
  what Stet looks for, not a promise about any one manuscript.
- Typos are planted without a dictionary, so a few are accidentally real words.
- Published books contain a few genuine mistakes. A flag on one is counted here as a false alarm.
- The word list is a stand-in. A real writer's list would be more complete.
- Planted paragraphs are checked on their own, without the paragraphs before them.
