# Evaluation: spelling-world

Library 0.1.0. Seed 20261114, 30,000 clean words per book, 500 planted errors per category for each corpus and split.

Changes are against the previous run of this checker.

## False alarms on clean text

Target: under 1 per 1,000 words. Flags confirmed as real errors in the source are not counted.

| Corpus | Split | Books | Words | Flags | Per 1,000 words | Change | harper | Confirmed real errors | Not checked |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| calibre | dev | 16 | 454,679 | 967 | 2.127 | 0 | 43.365 | 0 | 0 |
| calibre | test | 8 | 240,253 | 578 | 2.406 | 0 | 40.52 | 0 | 0 |
| gutenberg | dev | 29 | 859,568 | 1,532 | 1.782 | 0 | 24.358 | 0 | 0 |
| gutenberg | test | 11 | 304,864 | 510 | 1.673 | 0 | 19.94 | 0 | 0 |
| all | dev | 45 | 1,314,247 | 2,499 | 1.902 | 0 | 30.933 | 0 | 0 |
| all | test | 19 | 545,117 | 1,088 | 1.996 | 0 | 29.01 | 0 | 0 |

## False alarms per rule

Flags per 1,000 words over all corpora. Target for a rule to be on by default: under 0.05. Rules under the bar on both sides are counted, not listed.

| Rule | Category | Dev | Test | Change (dev) |
|---|---|---:|---:|---:|
| spelling.misspelling | Spelling | 1.252 | 1.477 | -0.431 |
| spelling.unknown-name | Spelling | 0.431 | 0.36 | +0.431 |
| spelling.other-dialect | Spelling | 0.121 | 0.099 | 0 |
| spelling.capitalization | Spelling | 0.097 | 0.061 | 0 |

0 other rules flagged something but stayed under the bar.

## Planted errors

Caught: share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion restores the original text.

### all, dev

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 96% | 0 | 90.2% | 87.8% | 98.9% |
| confusable | 1000 | 1000 | 0% | 0 | 35.9% | 0% | 0% |
| a-an | 1000 | 1000 | 0% | 0 | 90.9% | 0% | 0% |
| repeated-word | 1000 | 1000 | 0% | 0 | 61.9% | 0% | 0% |
| agreement | 1000 | 1000 | 0% | 0 | 65.5% | 0% | 0% |
| dialogue-punctuation | 1000 | 1000 | 0% | 0 | 0.4% | 0% | 0% |

### all, test

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 97.3% | 0 | 86.8% | 86.7% | 98.6% |
| confusable | 1000 | 1000 | 0% | 0 | 32.8% | 0% | 0% |
| a-an | 1000 | 1000 | 0% | 0 | 87.9% | 0% | 0% |
| repeated-word | 1000 | 1000 | 0% | 0 | 59.9% | 0% | 0% |
| agreement | 1000 | 1000 | 0% | 0 | 57.1% | 0% | 0% |
| dialogue-punctuation | 1000 | 1000 | 0% | 0 | 0.2% | 0% | 0% |

### calibre, dev

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 95.6% | 0 | 93.8% | 88.9% | 99.2% |
| confusable | 500 | 500 | 0% | 0 | 37.2% | 0% | 0% |
| a-an | 500 | 500 | 0% | 0 | 96.4% | 0% | 0% |
| repeated-word | 500 | 500 | 0% | 0 | 64.8% | 0% | 0% |
| agreement | 500 | 500 | 0% | 0 | 69.6% | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% | 0 | 0.4% | 0% | 0% |

### calibre, test

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 97.2% | 0 | 87% | 86.4% | 98.6% |
| confusable | 500 | 500 | 0% | 0 | 31.4% | 0% | 0% |
| a-an | 500 | 500 | 0% | 0 | 89% | 0% | 0% |
| repeated-word | 500 | 500 | 0% | 0 | 59.8% | 0% | 0% |
| agreement | 500 | 500 | 0% | 0 | 58% | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% | 0 | 0.2% | 0% | 0% |

### gutenberg, dev

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 96.4% | 0 | 86.6% | 86.7% | 98.6% |
| confusable | 500 | 500 | 0% | 0 | 34.6% | 0% | 0% |
| a-an | 500 | 500 | 0% | 0 | 85.4% | 0% | 0% |
| repeated-word | 500 | 500 | 0% | 0 | 59% | 0% | 0% |
| agreement | 500 | 500 | 0% | 0 | 61.4% | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% | 0 | 0.4% | 0% | 0% |

### gutenberg, test

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 97.4% | 0 | 86.6% | 87.1% | 98.6% |
| confusable | 500 | 500 | 0% | 0 | 34.2% | 0% | 0% |
| a-an | 500 | 500 | 0% | 0 | 86.8% | 0% | 0% |
| repeated-word | 500 | 500 | 0% | 0 | 60% | 0% | 0% |
| agreement | 500 | 500 | 0% | 0 | 56.2% | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% | 0 | 0.2% | 0% | 0% |
