# Evaluation: none

Library 0.1.0. Seed 20261114, 30,000 clean words per book, 500 planted errors per category for each corpus and split.

Changes are against the previous run of this checker.

## False alarms on clean text

Target: under 1 per 1,000 words. Flags confirmed as real errors in the source are not counted.

| Corpus | Split | Books | Words | Flags | Per 1,000 words | Change | harper | Confirmed real errors | Not checked |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| calibre | dev | 16 | 454,434 | 0 | 0 | 0 | 40.089 | 0 | 0 |
| calibre | test | 8 | 240,253 | 0 | 0 | 0 | 40.52 | 0 | 0 |
| gutenberg | dev | 29 | 859,568 | 0 | 0 | 0 | 24.358 | 0 | 0 |
| gutenberg | test | 11 | 304,864 | 0 | 0 | 0 | 19.94 | 0 | 0 |
| all | dev | 45 | 1,314,002 | 0 | 0 | 0 | 29.798 | 0 | 0 |
| all | test | 19 | 545,117 | 0 | 0 | 0 | 29.01 | 0 | 0 |

## False alarms per rule

Flags per 1,000 words over all corpora. Target for a rule to be on by default: under 0.05. Rules under the bar on both sides are counted, not listed.

No flags.

## Planted errors

Caught: share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion restores the original text.

### all, dev

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 0% | 0 | 90.7% | 0% | 0% |
| confusable | 1000 | 1000 | 0% | 0 | 35.8% | 0% | 0% |
| a-an | 1000 | 1000 | 0% | 0 | 90.7% | 0% | 0% |
| repeated-word | 1000 | 1000 | 0% | 0 | 63.8% | 0% | 0% |
| agreement | 1000 | 1000 | 0% | 0 | 66.5% | 0% | 0% |
| dialogue-punctuation | 1000 | 1000 | 0% | 0 | 0.2% | 0% | 0% |

### all, test

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 0% | 0 | 86.8% | 0% | 0% |
| confusable | 1000 | 1000 | 0% | 0 | 32.8% | 0% | 0% |
| a-an | 1000 | 1000 | 0% | 0 | 87.9% | 0% | 0% |
| repeated-word | 1000 | 1000 | 0% | 0 | 59.9% | 0% | 0% |
| agreement | 1000 | 1000 | 0% | 0 | 57.1% | 0% | 0% |
| dialogue-punctuation | 1000 | 1000 | 0% | 0 | 0.2% | 0% | 0% |

### calibre, dev

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 0% | 0 | 94.8% | 0% | 0% |
| confusable | 500 | 500 | 0% | 0 | 37% | 0% | 0% |
| a-an | 500 | 500 | 0% | 0 | 96% | 0% | 0% |
| repeated-word | 500 | 500 | 0% | 0 | 68.6% | 0% | 0% |
| agreement | 500 | 500 | 0% | 0 | 71.6% | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% | 0 | 0% | 0% | 0% |

### calibre, test

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 0% | 0 | 87% | 0% | 0% |
| confusable | 500 | 500 | 0% | 0 | 31.4% | 0% | 0% |
| a-an | 500 | 500 | 0% | 0 | 89% | 0% | 0% |
| repeated-word | 500 | 500 | 0% | 0 | 59.8% | 0% | 0% |
| agreement | 500 | 500 | 0% | 0 | 58% | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% | 0 | 0.2% | 0% | 0% |

### gutenberg, dev

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 0% | 0 | 86.6% | 0% | 0% |
| confusable | 500 | 500 | 0% | 0 | 34.6% | 0% | 0% |
| a-an | 500 | 500 | 0% | 0 | 85.4% | 0% | 0% |
| repeated-word | 500 | 500 | 0% | 0 | 59% | 0% | 0% |
| agreement | 500 | 500 | 0% | 0 | 61.4% | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% | 0 | 0.4% | 0% | 0% |

### gutenberg, test

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 0% | 0 | 86.6% | 0% | 0% |
| confusable | 500 | 500 | 0% | 0 | 34.2% | 0% | 0% |
| a-an | 500 | 500 | 0% | 0 | 86.8% | 0% | 0% |
| repeated-word | 500 | 500 | 0% | 0 | 60% | 0% | 0% |
| agreement | 500 | 500 | 0% | 0 | 56.2% | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% | 0 | 0.2% | 0% | 0% |
