# Evaluation: harper

Library 0.1.0. Seed 20261114, 30,000 clean words per book, 500 planted errors per category for each corpus and split.

No earlier run to compare with.

## False alarms on clean text

Target: under 1 per 1,000 words. Flags confirmed as real errors in the source are not counted.

| Corpus | Split | Books | Words | Flags | Per 1,000 words | Change | Confirmed real errors | Not checked |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| calibre | dev | 16 | 454,679 | 19,717 | 43.365 |  | 0 | 0 |
| calibre | test | 8 | 240,253 | 9,735 | 40.52 |  | 0 | 0 |
| gutenberg | dev | 29 | 859,568 | 20,937 | 24.358 |  | 0 | 0 |
| gutenberg | test | 11 | 304,864 | 6,079 | 19.94 |  | 0 | 0 |
| all | dev | 45 | 1,314,247 | 40,654 | 30.933 |  | 0 | 0 |
| all | test | 19 | 545,117 | 15,814 | 29.01 |  | 0 | 0 |

## False alarms per rule

Flags per 1,000 words over all corpora. Target for a rule to be on by default: under 0.05. Rules under the bar on both sides are counted, not listed.

| Rule | Category | Dev | Test | Change (dev) |
|---|---|---:|---:|---:|
| harper.SpellCheck | Spelling | 21.439 | 20.054 |  |
| harper.LongSentences | Readability | 2.621 | 3.029 |  |
| harper.Dashes | Formatting | 1.733 | 1.099 |  |
| harper.SplitWords | Typo | 0.75 | 0.976 |  |
| harper.UnclosedQuotes | Formatting | 0.473 | 0.565 |  |
| harper.Spaces | Formatting | 0.438 | 0.053 |  |
| harper.SentenceCapitalization | Capitalization | 0.425 | 0.257 |  |
| harper.OxfordComma | Style | 0.306 | 0.295 |  |
| harper.PronounVerbAgreement | Agreement | 0.296 | 0.211 |  |
| harper.UseEllipsisCharacter | Formatting | 0.268 | 0.081 |  |
| harper.MassNouns | Agreement | 0.235 | 0.213 |  |
| harper.CompoundNouns | WordChoice | 0.226 | 0.235 |  |
| harper.MissingPreposition | Miscellaneous | 0.149 | 0.152 |  |
| harper.DisjointPrefixes | Spelling | 0.108 | 0.108 |  |
| harper.AnA | Miscellaneous | 0.107 | 0.072 |  |
| harper.EllipsisLength | Formatting | 0.107 | 0.042 |  |
| harper.DiscourseMarkers | Punctuation | 0.102 | 0.083 |  |
| harper.AvoidCurses | WordChoice | 0.1 | 0.242 |  |
| harper.PossessiveYour | WordChoice | 0.061 | 0.053 |  |
| harper.Excellent | Enhancement | 0.03 | 0.053 |  |
| harper.OrthographicConsistency | Capitalization | 0.015 | 0.125 |  |
| harper.YeaToYeah | Spelling | 0.007 | 0.053 |  |

167 other rules flagged something but stayed under the bar.

## Planted errors

Caught: share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion restores the original text.

### all, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 90.2% |  | 75.2% | 89.7% |
| confusable | 1000 | 1000 | 35.9% |  | 96.7% | 96.7% |
| a-an | 1000 | 1000 | 90.9% |  | 99.5% | 99.5% |
| repeated-word | 1000 | 1000 | 61.9% |  | 97.9% | 98.4% |
| agreement | 1000 | 1000 | 65.5% |  | 68.2% | 68.2% |
| dialogue-punctuation | 1000 | 1000 | 0.4% |  | 0% | 0% |

### all, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 86.8% |  | 74.2% | 89.5% |
| confusable | 1000 | 1000 | 32.8% |  | 97.6% | 97.6% |
| a-an | 1000 | 1000 | 87.9% |  | 99.8% | 99.8% |
| repeated-word | 1000 | 1000 | 59.9% |  | 97.7% | 98% |
| agreement | 1000 | 1000 | 57.1% |  | 70.2% | 70.2% |
| dialogue-punctuation | 1000 | 1000 | 0.2% |  | 0% | 0% |

### calibre, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 93.8% |  | 75.5% | 90.6% |
| confusable | 500 | 500 | 37.2% |  | 97.3% | 97.3% |
| a-an | 500 | 500 | 96.4% |  | 99.2% | 99.2% |
| repeated-word | 500 | 500 | 64.8% |  | 98.8% | 98.8% |
| agreement | 500 | 500 | 69.6% |  | 69.3% | 69.3% |
| dialogue-punctuation | 500 | 500 | 0.4% |  | 0% | 0% |

### calibre, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 87% |  | 72.9% | 88.1% |
| confusable | 500 | 500 | 31.4% |  | 98.7% | 98.7% |
| a-an | 500 | 500 | 89% |  | 99.6% | 99.6% |
| repeated-word | 500 | 500 | 59.8% |  | 98% | 98% |
| agreement | 500 | 500 | 58% |  | 65.5% | 65.5% |
| dialogue-punctuation | 500 | 500 | 0.2% |  | 0% | 0% |

### gutenberg, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 86.6% |  | 74.8% | 88.7% |
| confusable | 500 | 500 | 34.6% |  | 96% | 96% |
| a-an | 500 | 500 | 85.4% |  | 99.8% | 99.8% |
| repeated-word | 500 | 500 | 59% |  | 97% | 98% |
| agreement | 500 | 500 | 61.4% |  | 67.1% | 67.1% |
| dialogue-punctuation | 500 | 500 | 0.4% |  | 0% | 0% |

### gutenberg, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 86.6% |  | 75.5% | 91% |
| confusable | 500 | 500 | 34.2% |  | 96.5% | 96.5% |
| a-an | 500 | 500 | 86.8% |  | 100% | 100% |
| repeated-word | 500 | 500 | 60% |  | 97.3% | 98% |
| agreement | 500 | 500 | 56.2% |  | 75.1% | 75.1% |
| dialogue-punctuation | 500 | 500 | 0.2% |  | 0% | 0% |
