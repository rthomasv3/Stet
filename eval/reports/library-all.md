# Evaluation: library-all

Library 0.1.0. Seed 20261114, 30,000 clean words per book, 500 planted errors per category for each corpus and split.

No earlier run to compare with.

## False alarms on clean text

Target: under 1 per 1,000 words. Flags confirmed as real errors in the source are not counted.

| Corpus | Split | Books | Words | Flags | Per 1,000 words | Change | harper | Confirmed real errors | Not checked |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| calibre | dev | 16 | 454,434 | 682 | 1.501 |  | 40.089 | 5 | 0 |
| calibre | test | 8 | 240,253 | 626 | 2.606 |  | 40.52 | 0 | 0 |
| gutenberg | dev | 29 | 859,568 | 1,533 | 1.784 |  | 24.358 | 1 | 0 |
| gutenberg | test | 11 | 304,864 | 519 | 1.702 |  | 19.94 | 0 | 0 |
| all | dev | 45 | 1,314,002 | 2,215 | 1.686 |  | 29.798 | 6 | 0 |
| all | test | 19 | 545,117 | 1,145 | 2.101 |  | 29.01 | 0 | 0 |

## False alarms per rule

Flags per 1,000 words over all corpora. Target for a rule to be on by default: under 0.05. Rules under the bar on both sides are counted, not listed.

| Rule | Category | Dev | Test | Change (dev) |
|---|---|---:|---:|---:|
| spelling.misspelling | Spelling | 1.108 | 1.469 |  |
| spelling.unknown-name | Spelling | 0.316 | 0.358 |  |
| spelling.other-dialect | Spelling | 0.124 | 0.099 |  |
| spelling.capitalization | Spelling | 0.091 | 0.061 |  |
| mechanics.sentence-capital | Mechanics | 0.006 | 0.062 |  |

7 other rules flagged something but stayed under the bar.

## Planted errors

Caught: share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion restores the original text.

### all, dev

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 95.9% |  | 90.7% | 92.3% | 99% |
| confusable | 1000 | 1000 | 0% |  | 35.8% | 0% | 0% |
| a-an | 1000 | 1000 | 93.1% |  | 90.7% | 100% | 100% |
| repeated-word | 1000 | 1000 | 81% |  | 63.8% | 100% | 100% |
| agreement | 1000 | 1000 | 63.9% |  | 66.5% | 100% | 100% |
| dialogue-punctuation | 1000 | 1000 | 91.3% |  | 0.2% | 99.8% | 99.8% |

### all, test

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 97.3% |  | 86.8% | 90.5% | 98.8% |
| confusable | 1000 | 1000 | 0% |  | 32.8% | 0% | 0% |
| a-an | 1000 | 1000 | 94.1% |  | 87.9% | 100% | 100% |
| repeated-word | 1000 | 1000 | 78.8% |  | 59.9% | 100% | 100% |
| agreement | 1000 | 1000 | 64.5% |  | 57.1% | 100% | 100% |
| dialogue-punctuation | 1000 | 1000 | 94.1% |  | 0.2% | 100% | 100% |

### calibre, dev

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 95.4% |  | 94.8% | 92.7% | 99.2% |
| confusable | 500 | 500 | 0% |  | 37% | 0% | 0% |
| a-an | 500 | 500 | 92.6% |  | 96% | 100% | 100% |
| repeated-word | 500 | 500 | 80.2% |  | 68.6% | 100% | 100% |
| agreement | 500 | 500 | 60.2% |  | 71.6% | 100% | 100% |
| dialogue-punctuation | 500 | 500 | 91.8% |  | 0% | 100% | 100% |

### calibre, test

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 97.2% |  | 87% | 90.1% | 98.8% |
| confusable | 500 | 500 | 0% |  | 31.4% | 0% | 0% |
| a-an | 500 | 500 | 94.2% |  | 89% | 100% | 100% |
| repeated-word | 500 | 500 | 76.4% |  | 59.8% | 100% | 100% |
| agreement | 500 | 500 | 65.6% |  | 58% | 100% | 100% |
| dialogue-punctuation | 500 | 500 | 96.2% |  | 0.2% | 100% | 100% |

### gutenberg, dev

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 96.4% |  | 86.6% | 91.9% | 98.8% |
| confusable | 500 | 500 | 0% |  | 34.6% | 0% | 0% |
| a-an | 500 | 500 | 93.6% |  | 85.4% | 100% | 100% |
| repeated-word | 500 | 500 | 81.8% |  | 59% | 100% | 100% |
| agreement | 500 | 500 | 67.6% |  | 61.4% | 100% | 100% |
| dialogue-punctuation | 500 | 500 | 90.8% |  | 0.4% | 99.6% | 99.6% |

### gutenberg, test

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 97.4% |  | 86.6% | 91% | 98.8% |
| confusable | 500 | 500 | 0% |  | 34.2% | 0% | 0% |
| a-an | 500 | 500 | 94% |  | 86.8% | 100% | 100% |
| repeated-word | 500 | 500 | 81.2% |  | 60% | 100% | 100% |
| agreement | 500 | 500 | 63.4% |  | 56.2% | 100% | 100% |
| dialogue-punctuation | 500 | 500 | 92% |  | 0.2% | 100% | 100% |
