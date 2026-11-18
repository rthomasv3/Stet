# Evaluation: harper-inscope

harper-inscope (harper.js 3.2.0, style and readability flags left out). 30,000 words of clean text per book, and 500 planted errors per category for each corpus and each side of the split (seed 20261114).

Every book is on one side of a split. **Dev** books could be read and tuned against while Stet was built. **Test** books were only ever measured, so their numbers show how a checker does on text nobody tuned it for.

## False alarms on clean text

Flags on published text, which is taken to be correct.

| Corpus | Split | Books | Words | Flags | Per 1,000 words |
|---|---|---:|---:|---:|---:|
| classic | dev | 29 | 857,917 | 15,365 | 17.91 |
| classic | test | 11 | 303,609 | 3,987 | 13.132 |
| modern | dev | 16 | 470,887 | 17,022 | 36.149 |
| modern | test | 8 | 240,683 | 8,451 | 35.113 |
| all | dev | 45 | 1,328,804 | 32,387 | 24.373 |
| all | test | 19 | 544,292 | 12,438 | 22.852 |

## False alarms per rule

Flags per 1,000 words over all the books. Rules under 0.05 on both sides are counted, not listed.

| Rule | Category | Dev | Test |
|---|---|---:|---:|
| harper.SpellCheck | Spelling | 20.669 | 19.282 |
| harper.SplitWords | Typo | 0.838 | 1.007 |
| harper.SentenceCapitalization | Capitalization | 0.476 | 0.209 |
| harper.PronounVerbAgreement | Agreement | 0.303 | 0.281 |
| harper.CompoundNouns | WordChoice | 0.256 | 0.211 |
| harper.AnA | Miscellaneous | 0.245 | 0.061 |
| harper.MassNouns | Grammar | 0.236 | 0.191 |
| harper.MissingPreposition | Miscellaneous | 0.155 | 0.143 |
| harper.DiscourseMarkers | Punctuation | 0.114 | 0.088 |
| harper.DisjointPrefixes | Spelling | 0.104 | 0.085 |
| harper.AvoidCurses | WordChoice | 0.072 | 0.219 |
| harper.QuantifierNumeralConflict | Grammar | 0.055 | 0.017 |
| harper.PossessiveYour | WordChoice | 0.053 | 0.064 |
| harper.RepeatedWords | Repetition | 0.043 | 0.061 |
| harper.OrthographicConsistency | Capitalization | 0.02 | 0.096 |
| harper.YeaToYeah | Spelling | 0.011 | 0.079 |
| harper.MassNouns | Agreement | 0.236 | 0.191 |

139 other rules flagged something but stayed under 0.05.

## Planted errors

Over all the books. Caught: the share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion gives back the original text. The same figures for each corpus are in the JSON file beside this one.

### Test books

| Category | Planted | Caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|
| typo | 1,000 | 87.2% | 73.1% | 89.5% |
| confusable | 1,000 | 36.1% | 97% | 97% |
| a-an | 1,000 | 86.8% | 99.9% | 99.9% |
| repeated-word | 1,000 | 62.4% | 98.1% | 98.9% |
| agreement | 1,000 | 58.1% | 69% | 69% |
| dialogue-punctuation | 1,000 | 0.3% | 0% | 0% |

### Dev books

| Category | Planted | Caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|
| typo | 1,000 | 91% | 73.3% | 88% |
| confusable | 1,000 | 36.6% | 95.6% | 95.6% |
| a-an | 1,000 | 91.2% | 99.7% | 99.7% |
| repeated-word | 1,000 | 64% | 99.1% | 99.1% |
| agreement | 1,000 | 65.8% | 69.5% | 69.5% |
| dialogue-punctuation | 1,000 | 0% | 0% | 0% |
