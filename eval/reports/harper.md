# Evaluation: harper

Library 0.1.0. Seed 20261114, 30,000 clean words per book, 500 planted errors per category for each corpus and split.

Changes are against the previous run of this checker.

## False alarms on clean text

Target: under 1 per 1,000 words. Flags confirmed as real errors in the source are not counted.

| Corpus | Split | Books | Words | Flags | Per 1,000 words | Change | Confirmed real errors | Not checked |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| calibre | dev | 16 | 454,434 | 18,218 | 40.089 | -3.275 | 0 | 0 |
| calibre | test | 8 | 240,253 | 9,735 | 40.52 | 0 | 0 | 0 |
| gutenberg | dev | 29 | 859,568 | 20,937 | 24.358 | 0 | 0 | 0 |
| gutenberg | test | 11 | 304,864 | 6,079 | 19.94 | 0 | 0 | 0 |
| all | dev | 45 | 1,314,002 | 39,155 | 29.798 | -1.135 | 0 | 0 |
| all | test | 19 | 545,117 | 15,814 | 29.01 | 0 | 0 | 0 |

## False alarms per rule

Flags per 1,000 words over all corpora. Target for a rule to be on by default: under 0.05. Rules under the bar on both sides are counted, not listed.

| Rule | Category | Dev | Test | Change (dev) |
|---|---|---:|---:|---:|
| harper.SpellCheck | Spelling | 20.272 | 20.054 | -1.167 |
| harper.LongSentences | Readability | 2.662 | 3.029 | +0.041 |
| harper.Dashes | Formatting | 1.734 | 1.099 | 0 |
| harper.SplitWords | Typo | 0.746 | 0.976 | -0.004 |
| harper.UnclosedQuotes | Formatting | 0.473 | 0.565 | 0 |
| harper.SentenceCapitalization | Capitalization | 0.439 | 0.257 | +0.015 |
| harper.Spaces | Formatting | 0.439 | 0.053 | +0.002 |
| harper.OxfordComma | Style | 0.305 | 0.295 | -0.001 |
| harper.PronounVerbAgreement | Agreement | 0.304 | 0.211 | +0.008 |
| harper.UseEllipsisCharacter | Formatting | 0.253 | 0.081 | -0.015 |
| harper.MassNouns | Agreement | 0.243 | 0.213 | +0.008 |
| harper.CompoundNouns | WordChoice | 0.232 | 0.235 | +0.006 |
| harper.MissingPreposition | Miscellaneous | 0.147 | 0.152 | -0.002 |
| harper.DisjointPrefixes | Spelling | 0.113 | 0.108 | +0.005 |
| harper.EllipsisLength | Formatting | 0.104 | 0.042 | -0.002 |
| harper.DiscourseMarkers | Punctuation | 0.104 | 0.083 | +0.002 |
| harper.AvoidCurses | WordChoice | 0.101 | 0.242 | +0.001 |
| harper.AnA | Miscellaneous | 0.096 | 0.072 | -0.011 |
| harper.PossessiveYour | WordChoice | 0.06 | 0.053 | -0.001 |
| harper.Excellent | Enhancement | 0.03 | 0.053 | -0.001 |
| harper.OrthographicConsistency | Capitalization | 0.012 | 0.125 | -0.002 |
| harper.YeaToYeah | Spelling | 0.008 | 0.053 | +0.001 |

167 other rules flagged something but stayed under the bar.

## Planted errors

Caught: share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion restores the original text.

### all, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 90.7% | +0.5 pts | 73.5% | 88.9% |
| confusable | 1000 | 1000 | 35.8% | -0.1 pts | 96.1% | 96.1% |
| a-an | 1000 | 1000 | 90.7% | -0.2 pts | 99.6% | 99.6% |
| repeated-word | 1000 | 1000 | 63.8% | +1.9 pts | 97.8% | 98.3% |
| agreement | 1000 | 1000 | 66.5% | +1 pts | 68.4% | 68.4% |
| dialogue-punctuation | 1000 | 1000 | 0.2% | -0.2 pts | 0% | 0% |

### all, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 86.8% | 0 | 74.2% | 89.5% |
| confusable | 1000 | 1000 | 32.8% | 0 | 97.6% | 97.6% |
| a-an | 1000 | 1000 | 87.9% | 0 | 99.8% | 99.8% |
| repeated-word | 1000 | 1000 | 59.9% | 0 | 97.7% | 98% |
| agreement | 1000 | 1000 | 57.1% | 0 | 70.2% | 70.2% |
| dialogue-punctuation | 1000 | 1000 | 0.2% | 0 | 0% | 0% |

### calibre, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 94.8% | +1 pts | 72.4% | 89% |
| confusable | 500 | 500 | 37% | -0.2 pts | 96.2% | 96.2% |
| a-an | 500 | 500 | 96% | -0.4 pts | 99.4% | 99.4% |
| repeated-word | 500 | 500 | 68.6% | +3.8 pts | 98.5% | 98.5% |
| agreement | 500 | 500 | 71.6% | +2 pts | 69.6% | 69.6% |
| dialogue-punctuation | 500 | 500 | 0% | -0.4 pts | 0% | 0% |

### calibre, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 87% | 0 | 72.9% | 88.1% |
| confusable | 500 | 500 | 31.4% | 0 | 98.7% | 98.7% |
| a-an | 500 | 500 | 89% | 0 | 99.6% | 99.6% |
| repeated-word | 500 | 500 | 59.8% | 0 | 98% | 98% |
| agreement | 500 | 500 | 58% | 0 | 65.5% | 65.5% |
| dialogue-punctuation | 500 | 500 | 0.2% | 0 | 0% | 0% |

### gutenberg, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 86.6% | 0 | 74.8% | 88.7% |
| confusable | 500 | 500 | 34.6% | 0 | 96% | 96% |
| a-an | 500 | 500 | 85.4% | 0 | 99.8% | 99.8% |
| repeated-word | 500 | 500 | 59% | 0 | 97% | 98% |
| agreement | 500 | 500 | 61.4% | 0 | 67.1% | 67.1% |
| dialogue-punctuation | 500 | 500 | 0.4% | 0 | 0% | 0% |

### gutenberg, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 86.6% | 0 | 75.5% | 91% |
| confusable | 500 | 500 | 34.2% | 0 | 96.5% | 96.5% |
| a-an | 500 | 500 | 86.8% | 0 | 100% | 100% |
| repeated-word | 500 | 500 | 60% | 0 | 97.3% | 98% |
| agreement | 500 | 500 | 56.2% | 0 | 75.1% | 75.1% |
| dialogue-punctuation | 500 | 500 | 0.2% | 0 | 0% | 0% |
