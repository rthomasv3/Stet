# Evaluation: spelling

Stet 0.0.1. 30,000 words of clean text per book, and 500 planted errors per category for each corpus and each side of the split (seed 20261114).

Spelling only, with no word list, so every name and invented word in a book is flagged. Grammar rules are off, which is why only typos are caught among the planted errors.

Shown alongside: harper (harper.js 3.2.0).

Every book is on one side of a split. **Dev** books could be read and tuned against while Stet was built. **Test** books were only ever measured, so their numbers show how a checker does on text nobody tuned it for.

## False alarms on clean text

Flags on published text, which is taken to be correct.

| Corpus | Split | Books | Words | Flags | Per 1,000 words | harper |
|---|---|---:|---:|---:|---:|---:|
| classic | dev | 29 | 857,917 | 11,888 | 13.857 | 25.596 |
| classic | test | 11 | 303,609 | 2,408 | 7.931 | 19.186 |
| modern | dev | 16 | 470,887 | 14,601 | 31.007 | 39.035 |
| modern | test | 8 | 240,683 | 7,571 | 31.456 | 39.637 |
| all | dev | 45 | 1,328,804 | 26,489 | 19.935 | 30.358 |
| all | test | 19 | 544,292 | 9,979 | 18.334 | 28.229 |

## False alarms per rule

Flags per 1,000 words over all the books. Rules under 0.05 on both sides are counted, not listed.

| Rule | Category | Dev | Test |
|---|---|---:|---:|
| spelling.unknown-name | Spelling | 10.937 | 11.275 |
| spelling.misspelling | Spelling | 8.805 | 6.939 |
| spelling.other-dialect | Spelling | 0.144 | 0.074 |

1 other rules flagged something but stayed under 0.05.

## Planted errors

Over all the books. Caught: the share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion gives back the original text. The same figures for each corpus are in the JSON file beside this one.

### Test books

| Category | Planted | Caught | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|
| typo | 1,000 | 94.6% | 87.2% | 89.2% | 98.7% |
| confusable | 1,000 | 0% | 36.2% | 0% | 0% |
| a-an | 1,000 | 0% | 86.8% | 0% | 0% |
| repeated-word | 1,000 | 0% | 63.4% | 0% | 0% |
| agreement | 1,000 | 0% | 58.1% | 0% | 0% |
| dialogue-punctuation | 1,000 | 0.3% | 0.3% | 0% | 0% |

### Dev books

| Category | Planted | Caught | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|
| typo | 1,000 | 94.7% | 91% | 89.7% | 99.1% |
| confusable | 1,000 | 0% | 36.6% | 0% | 0% |
| a-an | 1,000 | 0% | 91.2% | 0% | 0% |
| repeated-word | 1,000 | 0% | 64.2% | 0% | 0% |
| agreement | 1,000 | 0% | 65.8% | 0% | 0% |
| dialogue-punctuation | 1,000 | 1.3% | 0% | 0% | 0% |
