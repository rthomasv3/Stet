# Where Stet's data comes from

Stet carries its word lists and statistics inside the assembly. This file records every outside source
that data was built from, the license each is under, and how to rebuild the data from scratch.

The notices those licenses require are in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md), which also
ships in the NuGet package.

Stet only takes data under licenses that put no conditions on an app that uses it beyond keeping a
notice: MIT, BSD, Apache-2.0, ISC, public domain and CC0, CC BY, and permissive word-list licenses
such as SCOWL's. Nothing under non-commercial, share-alike or copyleft terms (CC BY-NC, CC BY-SA, GPL,
AGPL, LGPL, MPL) is used.

## The sources

| Source | Version | License | What Stet takes from it |
|---|---|---|---|
| [SCOWL](https://github.com/en-wl/wordlist) Hunspell dictionaries: `en_US`, `en_GB-ise`, `en_GB-ize`, `en_CA`, `en_AU` | 2026.02.25 | SCOWL's own (like MIT) | The word list for each dialect. |
| The same, in the `large` size: `en_US-large`, `en_GB-large`, `en_CA-large`, `en_AU-large` | 2026.02.25 | SCOWL's own (like MIT) | Rarer words beyond the normal list, marked as rarer. |
| [Google Books Ngram](https://storage.googleapis.com/books/ngrams/books/datasetsv3.html), English Fiction, 1-grams | 20200217 | CC BY 3.0 | How common each word is in fiction published from 1950 on, reduced to one number per word, and how likely each word is to be a noun, a verb and so on. |
| Google Books Ngram, English Fiction, 2-grams | 20200217 | CC BY 3.0 | Statistics about which words follow which, from 1950 on. See below. |
| [CMU Pronouncing Dictionary](https://github.com/cmusphinx/cmudict) | commit `74790861` (2025-10-24) | BSD 2-clause | Which words start with a vowel sound, for choosing "a" or "an". Only the words whose first sound disagrees with their first letter are kept, about 400. No pronunciations are included. |
| [System Reference Document](https://www.dndbeyond.com/srd) (PDF) | 5.2.1 | CC BY 4.0 | A starting point for the fantasy word list: words it uses that SCOWL lacks were reviewed by hand and some were added. The data builder does not read it. |

**What the 2-grams are used for.** A 2-gram is a pair of words seen next to each other, with a count.
From those counts Stet keeps:

- how often one type of word (noun, verb and so on) follows another, and follows each common function
  word, which is how it works out what part of speech a word is in a sentence
- which nouns follow "much", "many" and similar words, to tell counted nouns from measured ones
- how often each confusable word ("their", "there") is seen beside other words, which is what the
  confusable-word rules judge by

## Written by this project

These lists are written by hand and are part of Stet's own MIT-licensed source.

| File | What it holds |
|---|---|
| `src/Stet.DataBuilder/Lists/genre.txt` | Fantasy, science-fiction and adventure words. Started from the SRD and the fiction counts above. |
| `src/Stet.DataBuilder/Lists/general.txt` | Everyday fiction words SCOWL lacks, found through the fiction counts. |
| `src/Stet.DataBuilder/Lists/interjections.txt` | Interjections and sound words. |
| `src/Stet.DataBuilder/Lists/irregular.txt` | Irregular verb, noun and adjective forms. |
| `src/Stet.DataBuilder/Lists/focus.txt`, `focus-more.txt` | The confusable words whose neighbours are kept from the 2-grams. |
| `src/Stet/Data/Lists/informal.txt` | Informal speech (gonna, dunno), accepted in dialogue unless dialogue strictness is set to strict. |
| `src/Stet/Data/Lists/archaic.txt` | Archaic forms (hath, dost). |
| `src/Stet/Data/Lists/misspellings.txt` | Common misspellings and their corrections. Not taken from any published list. |
| `src/Stet/Data/Lists/closed.txt` | Function words (pronouns, determiners, auxiliaries, prepositions) with the facts the rules need. |

## Rebuilding the data

You do not need to do this to use or build Stet: the built data files are in the repository, in
`src/Stet/Data`. Rebuilding is for checking where the data came from, or for changing it.

**What it costs.** The data builder downloads about 32 GB from Google (the 1-gram file is about 1 GB
and the 47 2-gram files about 31 GB) and takes about 20 minutes per pass on a fast connection. The
downloads are read as they arrive and are not stored whole. A filtered copy of about 780 MB is kept in
the `downloads/` folder, which git ignores, so that later passes read from disk.

Raw downloads are never committed. Each file the builder reads is checked against the SHA-256 recorded
below, so a rebuild either uses exactly the same input or stops.

Run these from the repository root, in this order:

    dotnet run -c Release --project src/Stet.DataBuilder -- ngram
    dotnet run -c Release --project src/Stet.DataBuilder -- lexicon
    dotnet run -c Release --project src/Stet.DataBuilder -- bigrams
    dotnet run -c Release --project src/Stet.DataBuilder -- bigrams-closed
    dotnet run -c Release --project src/Stet.DataBuilder -- wordinfo
    dotnet run -c Release --project src/Stet.DataBuilder -- bigrams-tagged
    dotnet run -c Release --project src/Stet.DataBuilder -- bigrams-all
    dotnet run -c Release --project src/Stet.DataBuilder -- bigrams-more
    dotnet run -c Release --project src/Stet.DataBuilder -- confusables

| Command | What it does |
|---|---|
| `ngram` | Reads the 1-gram file into `downloads/`. |
| `lexicon` | Writes `src/Stet/Data/lexicon.bin`, the word lists. |
| `bigrams`, `bigrams-closed` | Each reads the 47 2-gram files into small tables in `downloads/`. |
| `wordinfo` | Writes `src/Stet/Data/wordinfo.bin`, the part-of-speech statistics. |
| `bigrams-tagged`, `bigrams-all` | Two more passes over the 2-gram files. `bigrams-all` keeps the filtered local copy. |
| `bigrams-more` | Fills in the words of `focus-more.txt` from that local copy, in seconds. |
| `confusables` | Writes `src/Stet/Data/confusables.bin`, the confusable-word statistics. |

Rebuild the word info and the confusable-word data whenever the lexicon is rebuilt. Both are tied to
the exact word list, and Stet refuses data built for another one.

### Downloads and checksums

SCOWL's dictionaries all come from [release 2026.02.25](https://github.com/en-wl/wordlist/releases/tag/rel-2026.02.25).

| File | SHA-256 |
|---|---|
| SCOWL Hunspell `en_US` | `ac8e73310e951d88c52c2cf2ba54ceaca34f8486a81630ac8a75dc5f931179f9` |
| SCOWL Hunspell `en_GB-ise` | `d6fbb91ae7824c52fb02f74d7bc2cd9092f130faec60f42326a59437fa7247a3` |
| SCOWL Hunspell `en_GB-ize` | `c5bdd92fc1e21da7503939fe4139f36f28f01c18ac35e2fc90b526a1b7bfb099` |
| SCOWL Hunspell `en_CA` | `29fe0050408c35614f22bea84338b4256962c89f5ef25dac5038325bcb86f424` |
| SCOWL Hunspell `en_AU` | `f85471593b9744f64298c02d07fb62d58926bf81fb5e4552e2bac645bfa401e0` |
| SCOWL Hunspell `en_US-large` | `06ab5a2a12c29033f100988d3b0a5e53dcad40bf2473ccb78270619d1da99321` |
| SCOWL Hunspell `en_GB-large` | `704a49438a3a9d4f11cd76db433a6b230b9ce8770ee13fa28700052c4517ddc1` |
| SCOWL Hunspell `en_CA-large` | `b4b7bf4a7885de1c35fa1b3aa68317209afa58f04d60e8a07e124693485c59e3` |
| SCOWL Hunspell `en_AU-large` | `46a265883d1d01c889b7173ba1cabae0ca915aa403d6a2f93cee4cc661087db4` |
| Google 1-grams: [`1-00000-of-00001.gz`](https://storage.googleapis.com/books/ngrams/books/20200217/eng-fiction/1-00000-of-00001.gz) | `8640fb65e15484592665237df129de132032e25354d5f984f61c32100d901461` |
| Google 2-grams: `2-00000-of-00047.gz` to `2-00046-of-00047.gz`, from the same folder | one for each file, in `src/Stet.DataBuilder/ngram-2gram-checksums.txt` |
| CMU Pronouncing Dictionary: [`cmudict.dict`](https://github.com/cmusphinx/cmudict/blob/74790861f652b15e4ac49015a90074ad62a27690/cmudict.dict) | `81917843c7f44ce2b094ac63873c2c7a4cf802040792c455ba3ca406891c3d22` |
| System Reference Document: [`SRD_CC_v5.2.1.pdf`](https://media.dndbeyond.com/compendium-images/srd/5.2/SRD_CC_v5.2.1.pdf) | `8974902d109d6e63672d7c490bde9ccf052410503d9cfa768237154fbc5e3d87` |

## Sources used only for measuring

The books and the other checkers used to measure Stet's accuracy are described in
[eval/RESULTS.md](eval/RESULTS.md). None of them is in the data Stet ships.
