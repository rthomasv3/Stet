# Source record

Every source used to build or tune data that ships in the package is recorded here before it is used.
Raw downloads are never committed; this record plus the data builder is enough to rebuild the data
exactly. The builder checks each download against the SHA-256 below.

Allowed licenses: MIT, BSD, Apache-2.0, ISC, public domain / CC0, CC BY, and permissive word-list
licenses such as SCOWL's. Not allowed: non-commercial terms, GPL, AGPL, and for now share-alike
(CC BY-SA), LGPL and MPL.

## Shipped data

| Name | Version | License | Link | SHA-256 | How used |
|---|---|---|---|---|---|
| SCOWL Hunspell `en_US` | 2026.02.25 | SCOWL (MIT-like) | https://github.com/en-wl/wordlist/releases/tag/rel-2026.02.25 | `ac8e73310e951d88c52c2cf2ba54ceaca34f8486a81630ac8a75dc5f931179f9` | Word list for the dialect, expanded to flat words. |
| SCOWL Hunspell `en_GB-ise` | 2026.02.25 | SCOWL (MIT-like) | https://github.com/en-wl/wordlist/releases/tag/rel-2026.02.25 | `d6fbb91ae7824c52fb02f74d7bc2cd9092f130faec60f42326a59437fa7247a3` | Word list for the dialect, expanded to flat words. |
| SCOWL Hunspell `en_GB-ize` | 2026.02.25 | SCOWL (MIT-like) | https://github.com/en-wl/wordlist/releases/tag/rel-2026.02.25 | `c5bdd92fc1e21da7503939fe4139f36f28f01c18ac35e2fc90b526a1b7bfb099` | Word list for the dialect, expanded to flat words. |
| SCOWL Hunspell `en_CA` | 2026.02.25 | SCOWL (MIT-like) | https://github.com/en-wl/wordlist/releases/tag/rel-2026.02.25 | `29fe0050408c35614f22bea84338b4256962c89f5ef25dac5038325bcb86f424` | Word list for the dialect, expanded to flat words. |
| SCOWL Hunspell `en_AU` | 2026.02.25 | SCOWL (MIT-like) | https://github.com/en-wl/wordlist/releases/tag/rel-2026.02.25 | `f85471593b9744f64298c02d07fb62d58926bf81fb5e4552e2bac645bfa401e0` | Word list for the dialect, expanded to flat words. |
| SCOWL Hunspell `en_US-large` | 2026.02.25 | SCOWL (MIT-like) | same release | `06ab5a2a12c29033f100988d3b0a5e53dcad40bf2473ccb78270619d1da99321` | Rarer words beyond the normal list; marked as rarer in the data. |
| SCOWL Hunspell `en_GB-large` | 2026.02.25 | SCOWL (MIT-like) | same release | `704a49438a3a9d4f11cd76db433a6b230b9ce8770ee13fa28700052c4517ddc1` | Rarer words beyond the normal list; marked as rarer in the data. |
| SCOWL Hunspell `en_CA-large` | 2026.02.25 | SCOWL (MIT-like) | same release | `b4b7bf4a7885de1c35fa1b3aa68317209afa58f04d60e8a07e124693485c59e3` | Rarer words beyond the normal list; marked as rarer in the data. |
| SCOWL Hunspell `en_AU-large` | 2026.02.25 | SCOWL (MIT-like) | same release | `46a265883d1d01c889b7173ba1cabae0ca915aa403d6a2f93cee4cc661087db4` | Rarer words beyond the normal list; marked as rarer in the data. |
| Google Books Ngram, English Fiction, 1-grams | 20200217 | CC BY 3.0 | http://storage.googleapis.com/books/ngrams/books/20200217/eng-fiction/1-00000-of-00001.gz | `8640fb65e15484592665237df129de132032e25354d5f984f61c32100d901461` | Word counts from 1950 on, reduced to one log-scale frequency per word. Streamed; the 0.94 GB file is not stored. |
| Google Books Ngram, English Fiction, 2-grams | 20200217 | CC BY 3.0 | http://storage.googleapis.com/books/ngrams/books/20200217/eng-fiction/2-00000-of-00047.gz to `2-00046-of-00047.gz` | one per file, in `src/Stet.DataBuilder/ngram-2gram-checksums.txt` | Counts from 1950 on of one word type following another, and of the word type following each function word (the tagger's statistics); and which nouns follow "much", "many" and similar words (counted or measured nouns). Also how often each confusable word ("their", "there", ...) is seen beside other words, and what follows the 's that stands for "is" (the confusable-word statistics, `confusables.bin`). The 31 GB of files are not stored. A filtered local copy is kept in the ignored `downloads/` folder (about 780 MB: every pair of plain words, punctuation marks or word types seen 40 times or more since 1950), so that word lists added later (`Lists/focus-more.txt`) are filled from disk in seconds instead of another 20-minute download. |
| CMU Pronouncing Dictionary | commit `74790861` (2025-10-24) | BSD 2-clause | https://github.com/cmusphinx/cmudict/blob/74790861f652b15e4ac49015a90074ad62a27690/cmudict.dict | `81917843c7f44ce2b094ac63873c2c7a4cf802040792c455ba3ca406891c3d22` | Which words start with a vowel sound. Only the words whose first sound disagrees with their first letter are kept (about 400), as positions in the word list. |
| System Reference Document 5.2.1 (PDF) | 5.2.1 | CC BY 4.0 | https://media.dndbeyond.com/compendium-images/srd/5.2/SRD_CC_v5.2.1.pdf | `8974902d109d6e63672d7c490bde9ccf052410503d9cfa768237154fbc5e3d87` | Seed for the genre word list: words it uses that SCOWL lacks were reviewed by hand. Not read by the builder. |

## Written by this project

These lists are hand-written and are part of the project's own MIT-licensed source:

| File | What |
|---|---|
| `src/Stet.DataBuilder/Lists/genre.txt` | Fantasy, science-fiction and adventure words. Seeded from the SRD and the fiction counts above. |
| `src/Stet.DataBuilder/Lists/general.txt` | Everyday fiction words SCOWL lacks, found through the fiction counts. |
| `src/Stet.DataBuilder/Lists/interjections.txt` | Interjections and sound words. |
| `src/Stet/Data/Lists/informal.txt` | Informal speech accepted in lenient dialogue. |
| `src/Stet/Data/Lists/archaic.txt` | Archaic forms (hath, dost). |
| `src/Stet/Data/Lists/misspellings.txt` | Common misspellings and corrections. Not taken from any published list. |
| `src/Stet/Data/Lists/closed.txt` | Function words (pronouns, determiners, auxiliaries, prepositions) with the facts rules need. |
| `src/Stet.DataBuilder/Lists/irregular.txt` | Irregular verb, noun and adjective forms. Regular forms are found by spelling rules checked against the Google Books counts. |
| `src/Stet.DataBuilder/Lists/focus.txt` | The words whose neighbours are kept from the 2-grams. |
| `src/Stet.DataBuilder/Lists/focus-more.txt` | Confusable words added later, whose neighbours a further pass keeps. |

## Rebuilding

    dotnet run -c Release --project src/Stet.DataBuilder -- ngram
    dotnet run -c Release --project src/Stet.DataBuilder -- lexicon
    dotnet run -c Release --project src/Stet.DataBuilder -- bigrams
    dotnet run -c Release --project src/Stet.DataBuilder -- bigrams-closed
    dotnet run -c Release --project src/Stet.DataBuilder -- wordinfo
    dotnet run -c Release --project src/Stet.DataBuilder -- bigrams-tagged
    dotnet run -c Release --project src/Stet.DataBuilder -- bigrams-all
    dotnet run -c Release --project src/Stet.DataBuilder -- bigrams-more
    dotnet run -c Release --project src/Stet.DataBuilder -- confusables

The first command streams the Ngram file once (about 1 GB) into `downloads/`; the second writes
`src/Stet/Data/lexicon.bin`. The two `bigrams` commands each stream the 47 2-gram files (about 31 GB,
20 minutes on a fast connection) into small tables in `downloads/`, and `wordinfo` writes
`src/Stet/Data/wordinfo.bin`. `bigrams-tagged` and `bigrams-all` are further passes over the same files (`bigrams-all` keeps the local copy that `bigrams-more` reads), and `confusables`
writes `src/Stet/Data/confusables.bin`. Rebuild the word info and the confusable-word data whenever
the lexicon is rebuilt: both are tied to the exact word list and the library refuses data built for another one.

## Evaluation only (nothing ships)

Sources used only to measure accuracy are listed in `eval/README.md`.
