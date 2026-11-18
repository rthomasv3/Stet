# Evaluation: stet

Stet 0.0.1. 30,000 words of clean text per book, and 500 planted errors per category for each corpus and each side of the split (seed 20261114).

Spelling and grammar at Stet's defaults. Each book has a stand-in for the writer's word list: every unknown word the book uses twice or more.

Shown alongside: harper-words (harper.js 3.2.0, given each book's word list).

Every book is on one side of a split. **Dev** books could be read and tuned against while Stet was built. **Test** books were only ever measured, so their numbers show how a checker does on text nobody tuned it for.

## False alarms on clean text

Flags on published text, which is taken to be correct.

| Corpus | Split | Books | Words | Flags | Per 1,000 words | harper-words |
|---|---|---:|---:|---:|---:|---:|
| classic | dev | 29 | 857,917 | 1,420 | 1.655 | 14.901 |
| classic | test | 11 | 303,609 | 451 | 1.486 | 12.325 |
| modern | dev | 16 | 470,887 | 736 | 1.563 | 11.272 |
| modern | test | 8 | 240,683 | 578 | 2.402 | 13.964 |
| all | dev | 45 | 1,328,804 | 2,156 | 1.623 | 13.615 |
| all | test | 19 | 544,292 | 1,029 | 1.891 | 13.05 |

## False alarms per rule

Flags per 1,000 words over all the books. Rules under 0.05 on both sides are counted, not listed.

| Rule | Category | Dev | Test |
|---|---|---:|---:|
| spelling.misspelling | Spelling | 0.981 | 1.306 |
| spelling.unknown-name | Spelling | 0.345 | 0.347 |
| spelling.other-dialect | Spelling | 0.144 | 0.074 |
| spelling.capitalization | Spelling | 0.071 | 0.07 |

27 other rules flagged something but stayed under 0.05.

## Planted errors

Over all the books. Caught: the share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion gives back the original text. The same figures for each corpus are in the JSON file beside this one.

### Test books

| Category | Planted | Caught | harper-words caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|
| typo | 1,000 | 95.5% | 87.6% | 89% | 98.5% |
| confusable | 1,000 | 67.1% | 36.2% | 99.9% | 99.9% |
| a-an | 1,000 | 93.1% | 86.8% | 100% | 100% |
| repeated-word | 1,000 | 80.6% | 63.4% | 100% | 100% |
| agreement | 1,000 | 63% | 58.1% | 100% | 100% |
| dialogue-punctuation | 1,000 | 92.6% | 0.2% | 99.9% | 99.9% |

### Dev books

| Category | Planted | Caught | harper-words caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|
| typo | 1,000 | 95.6% | 91.3% | 89.6% | 99.1% |
| confusable | 1,000 | 63.4% | 36.6% | 98.7% | 98.7% |
| a-an | 1,000 | 93.6% | 91.2% | 100% | 100% |
| repeated-word | 1,000 | 81.6% | 64.2% | 99.9% | 100% |
| agreement | 1,000 | 64.9% | 65.8% | 99.9% | 99.9% |
| dialogue-punctuation | 1,000 | 93.2% | 0% | 99.5% | 99.5% |
