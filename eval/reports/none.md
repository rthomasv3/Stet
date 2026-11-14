# Evaluation: none

Library 0.1.0. Seed 20261114, 30,000 clean words per book, 500 planted errors per category for each corpus and split.

No earlier run to compare with.

## False alarms on clean text

Target: under 1 per 1,000 words. Flags confirmed as real errors in the source are not counted.

| Corpus | Split | Books | Words | Flags | Per 1,000 words | Change | Confirmed real errors | Not checked |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| calibre | dev | 16 | 454,679 | 0 | 0 |  | 0 | 0 |
| calibre | test | 8 | 240,253 | 0 | 0 |  | 0 | 0 |
| gutenberg | dev | 29 | 859,568 | 0 | 0 |  | 0 | 0 |
| gutenberg | test | 11 | 304,864 | 0 | 0 |  | 0 | 0 |
| all | dev | 45 | 1,314,247 | 0 | 0 |  | 0 | 0 |
| all | test | 19 | 545,117 | 0 | 0 |  | 0 | 0 |

## False alarms per rule

Flags per 1,000 words over all corpora. Target for a rule to be on by default: under 0.05.

No flags.

## Planted errors

Caught: share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion restores the original text.

### calibre, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 0% |  | 0% | 0% |
| confusable | 500 | 500 | 0% |  | 0% | 0% |
| a-an | 500 | 500 | 0% |  | 0% | 0% |
| repeated-word | 500 | 500 | 0% |  | 0% | 0% |
| agreement | 500 | 500 | 0% |  | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% |  | 0% | 0% |

### calibre, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 0% |  | 0% | 0% |
| confusable | 500 | 500 | 0% |  | 0% | 0% |
| a-an | 500 | 500 | 0% |  | 0% | 0% |
| repeated-word | 500 | 500 | 0% |  | 0% | 0% |
| agreement | 500 | 500 | 0% |  | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% |  | 0% | 0% |

### gutenberg, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 0% |  | 0% | 0% |
| confusable | 500 | 500 | 0% |  | 0% | 0% |
| a-an | 500 | 500 | 0% |  | 0% | 0% |
| repeated-word | 500 | 500 | 0% |  | 0% | 0% |
| agreement | 500 | 500 | 0% |  | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% |  | 0% | 0% |

### gutenberg, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 500 | 500 | 0% |  | 0% | 0% |
| confusable | 500 | 500 | 0% |  | 0% | 0% |
| a-an | 500 | 500 | 0% |  | 0% | 0% |
| repeated-word | 500 | 500 | 0% |  | 0% | 0% |
| agreement | 500 | 500 | 0% |  | 0% | 0% |
| dialogue-punctuation | 500 | 500 | 0% |  | 0% | 0% |

### all, dev

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 0% |  | 0% | 0% |
| confusable | 1000 | 1000 | 0% |  | 0% | 0% |
| a-an | 1000 | 1000 | 0% |  | 0% | 0% |
| repeated-word | 1000 | 1000 | 0% |  | 0% | 0% |
| agreement | 1000 | 1000 | 0% |  | 0% | 0% |
| dialogue-punctuation | 1000 | 1000 | 0% |  | 0% | 0% |

### all, test

| Category | Planted | Checked | Caught | Change | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 1000 | 0% |  | 0% | 0% |
| confusable | 1000 | 1000 | 0% |  | 0% | 0% |
| a-an | 1000 | 1000 | 0% |  | 0% | 0% |
| repeated-word | 1000 | 1000 | 0% |  | 0% | 0% |
| agreement | 1000 | 1000 | 0% |  | 0% | 0% |
| dialogue-punctuation | 1000 | 1000 | 0% |  | 0% | 0% |
