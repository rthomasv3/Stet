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

Both checkers were given the same word list for each book, standing in for the names and invented
words a writer would add.

| | Flags per 1,000 words |
|---|---:|
| Stet | 1.89 |
| Harper | 13.05 |
| Harper, style and readability flags left out | 7.67 |

Harper checks more than Stet does, including style and readability, which is what the third row leaves
out. Spelling is the part both do, and there the figures are 1.80 for Stet and 4.66 for Harper.

On the dev books, most of what Harper's spelling still flags is of three kinds: the possessive of a name on the list
("Maer's" when "Maer" was added), a listed name written in capitals, and ordinary words its dictionary
does not hold.

**With no word list**, as either checker is when first installed:

| | Flags per 1,000 words |
|---|---:|
| Stet, spelling only | 18.23 |
| Harper, spelling flags only | 19.49 |
| Harper, everything | 28.23 |

Without a list the two are about level on spelling: nearly every flag is a name or an invented word.
The word list is what brings both down, and it brings Stet down further.

### The word list

Books do not come with a writer's word list, so one is made for each book by rule:

1. Every word Stet does not know that the book uses twice or more.
2. Every capitalized word Harper flags as misspelled twice or more in the measured text, unless the
   list already has it. This catches made-up names that Stet happens to accept because they are also
   ordinary words, which the first step alone would leave Harper flagged for every time.

The second step cannot tell a made-up name from a real one that Harper's dictionary lacks, so it
forgives both. That favours Harper. It also counts only within the 30,000 words measured for each
book, so a name seen once there is not added even if the book uses it often. That costs Harper a few
flags. The lists average 142 words a book.

## Planted errors caught

Both checkers with the word list.

| Kind of error | Example | Stet | Harper |
|---|---|---:|---:|
| Typo | "recieve" | 95.5% | 87.6% |
| Confusable word | "taller then her" | 67.1% | 36.2% |
| a / an | "a apple" | 93.1% | 86.8% |
| Repeated word | "the the" | 80.6% | 63.4% |
| Agreement | "they was" | 63.0% | 58.1% |
| Dialogue punctuation | "Hello." she said. | 92.6% | 0.2% |

When Stet catches a typo, its first suggestion is the right word 89.0% of the time, and the right word
is in the first five 98.5% of the time. For Harper the first suggestion is right 73.3% of the time.

Harper caught almost none of the last kind.

## About the comparison

[Harper](https://github.com/Automattic/harper) is an open-source, offline grammar checker. It was run
as `harper.js` 3.2.0 with its default rules, in plain-text mode, with the dialect matched to each book.
These figures are for that version on these books and say nothing about any other.

**Why Harper.** It is the nearest thing to Stet that already exists: a light, offline checker that runs
inside an app with no server behind it. Stet's author had considered it for another project, which is
how it became the yardstick. It is written in Rust and usually used from JavaScript, so it is not an
alternative for a .NET app, only a measure of what a checker of this kind does on fiction.

Harper is a general-purpose checker and is not built for fiction, and it checks things Stet does not
attempt. How to repeat the run is in [`harper/`](harper/README.md).

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

These times come from the harness's `perf` command, which checks each real book once and times it,
on the normal .NET runtime, before the runtime has finished optimizing the code. They are on the slow
side for that reason. The [benchmarks](../bench/README.md) time the same operations repeatedly, on
both the normal runtime and Native AOT, and are the better guide to what an app will see.

## Limits of these numbers

- Planted errors are made by rule, and real mistakes are more varied. A catch rate here is a guide to
  what Stet looks for, not a promise about any one manuscript.
- Typos are planted without a dictionary, so a few are accidentally real words.
- Published books contain a few genuine mistakes. A flag on one is counted here as a false alarm.
- The word list is a stand-in. A real writer's list would be more complete.
- Planted paragraphs are checked on their own, without the paragraphs before them.
