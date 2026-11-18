# Evaluation: harper-words

harper-words (harper.js 3.2.0, given each book's word list). 30,000 words of clean text per book, and 500 planted errors per category for each corpus and each side of the split (seed 20261114).

Every book is on one side of a split. **Dev** books could be read and tuned against while Stet was built. **Test** books were only ever measured, so their numbers show how a checker does on text nobody tuned it for.

## False alarms on clean text

Flags on published text, which is taken to be correct.

| Corpus | Split | Books | Words | Flags | Per 1,000 words |
|---|---|---:|---:|---:|---:|
| classic | dev | 29 | 857,917 | 12,784 | 14.901 |
| classic | test | 11 | 303,609 | 3,742 | 12.325 |
| modern | dev | 16 | 470,887 | 5,308 | 11.272 |
| modern | test | 8 | 240,683 | 3,361 | 13.964 |
| all | dev | 45 | 1,328,804 | 18,092 | 13.615 |
| all | test | 19 | 544,292 | 7,103 | 13.05 |

## False alarms per rule

Flags per 1,000 words over all the books. Rules under 0.05 on both sides are counted, not listed.

| Rule | Category | Dev | Test |
|---|---|---:|---:|
| harper.SpellCheck | Spelling | 4.186 | 4.448 |
| harper.LongSentences | Readability | 2.544 | 3.096 |
| harper.Dashes | Formatting | 1.699 | 1.255 |
| harper.SplitWords | Typo | 0.563 | 0.636 |
| harper.UnclosedQuotes | Formatting | 0.486 | 0.39 |
| harper.Spaces | Formatting | 0.482 | 0.125 |
| harper.SentenceCapitalization | Capitalization | 0.476 | 0.209 |
| harper.PronounVerbAgreement | Agreement | 0.303 | 0.281 |
| harper.OxfordComma | Style | 0.278 | 0.243 |
| harper.CompoundNouns | WordChoice | 0.256 | 0.211 |
| harper.AnA | Miscellaneous | 0.245 | 0.061 |
| harper.UseEllipsisCharacter | Formatting | 0.244 | 0.083 |
| harper.MassNouns | Grammar | 0.237 | 0.195 |
| harper.MissingPreposition | Miscellaneous | 0.155 | 0.143 |
| harper.DiscourseMarkers | Punctuation | 0.114 | 0.088 |
| harper.DisjointPrefixes | Spelling | 0.102 | 0.085 |
| harper.EllipsisLength | Formatting | 0.099 | 0.037 |
| harper.AvoidCurses | WordChoice | 0.072 | 0.219 |
| harper.OrthographicConsistency | Capitalization | 0.067 | 0.123 |
| harper.QuantifierNumeralConflict | Grammar | 0.055 | 0.017 |
| harper.PossessiveYour | WordChoice | 0.053 | 0.064 |
| harper.RepeatedWords | Repetition | 0.043 | 0.061 |
| harper.YeaToYeah | Spelling | 0.011 | 0.079 |
| harper.MassNouns | Agreement | 0.237 | 0.195 |

160 other rules flagged something but stayed under 0.05.

## Planted errors

Over all the books. Caught: the share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion gives back the original text. The same figures for each corpus are in the JSON file beside this one.

### Test books

| Category | Planted | Caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|
| typo | 1,000 | 87.6% | 73.3% | 89.6% |
| confusable | 1,000 | 36.2% | 96.7% | 96.7% |
| a-an | 1,000 | 86.8% | 99.9% | 99.9% |
| repeated-word | 1,000 | 63.4% | 96.5% | 97.3% |
| agreement | 1,000 | 58.1% | 69% | 69% |
| dialogue-punctuation | 1,000 | 0.2% | 0% | 0% |

### Dev books

| Category | Planted | Caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|
| typo | 1,000 | 91.3% | 73.4% | 88.3% |
| confusable | 1,000 | 36.6% | 95.6% | 95.6% |
| a-an | 1,000 | 91.2% | 99.7% | 99.7% |
| repeated-word | 1,000 | 64.2% | 98.8% | 98.8% |
| agreement | 1,000 | 65.8% | 69.5% | 69.5% |
| dialogue-punctuation | 1,000 | 0% | 0% | 0% |
