# Evaluation: languagetool

Library 0.1.0. Seed 20261114, 30,000 clean words per book, 500 planted errors per category for each corpus and split.

No earlier run to compare with.

## False alarms on clean text

Target: under 1 per 1,000 words. Flags confirmed as real errors in the source are not counted.

| Corpus | Split | Books | Words | Flags | Per 1,000 words | Change | harper | Confirmed real errors | Not checked |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| calibre | dev | 16 | 3 | 0 | 0 |  | 43.365 | 0 | 14100 of 14103 paragraphs |
| calibre | test | 8 | 6 | 0 | 0 |  | 40.52 | 0 | 7513 of 7517 paragraphs |
| gutenberg | dev | 29 | 91,286 | 1,635 | 17.911 |  | 24.358 | 0 | 15748 of 17652 paragraphs |
| gutenberg | test | 11 | 57,728 | 803 | 13.91 |  | 19.94 | 0 | 4503 of 5481 paragraphs |
| all | dev | 45 | 91,289 | 1,635 | 17.91 |  | 30.933 | 0 | 29848 of 31755 paragraphs |
| all | test | 19 | 57,734 | 803 | 13.909 |  | 29.01 | 0 | 12016 of 12998 paragraphs |

## False alarms per rule

Flags per 1,000 words over all corpora. Target for a rule to be on by default: under 0.05. Rules under the bar on both sides are counted, not listed.

| Rule | Category | Dev | Test | Change (dev) |
|---|---|---:|---:|---:|
| lt.MORFOLOGIK_RULE_EN_GB | TYPOS | 8.194 | 6.443 |  |
| lt.MORFOLOGIK_RULE_EN_US | TYPOS | 4.031 | 1.992 |  |
| lt.EN_UNPAIRED_BRACKETS | PUNCTUATION | 0.876 | 1.23 |  |
| lt.UPPERCASE_SENTENCE_START | CASING | 0.515 | 0.433 |  |
| lt.COMMA_PARENTHESIS_WHITESPACE | TYPOGRAPHY | 0.405 | 0.087 |  |
| lt.ENGLISH_WORD_REPEAT_BEGINNING_RULE | STYLE | 0.285 | 0.485 |  |
| lt.COMMA_COMPOUND_SENTENCE | PUNCTUATION | 0.241 | 0.277 |  |
| lt.OXFORD_SPELLING_Z_NOT_S | BRE_STYLE_OXFORD_SPELLING | 0.219 | 0.017 |  |
| lt.MISSING_COMMA_AFTER_INTRODUCTORY_PHRASE | PUNCTUATION | 0.164 | 0.121 |  |
| lt.PRP_COMMA | PUNCTUATION | 0.132 | 0.156 |  |
| lt.ANY_BODY | TYPOS | 0.099 | 0.052 |  |
| lt.DOUBLE_NEGATIVE | GRAMMAR | 0.099 | 0.069 |  |
| lt.EXTREME_ADJECTIVES | STYLE | 0.099 | 0.017 |  |
| lt.NON3PRS_VERB | GRAMMAR | 0.099 | 0.052 |  |
| lt.SENT_START_CONJUNCTIVE_LINKING_ADVERB_COMMA | PUNCTUATION | 0.099 | 0.208 |  |
| lt.COMMA_COMPOUND_SENTENCE_2 | PUNCTUATION | 0.077 | 0.017 |  |
| lt.PERS_PRONOUN_AGREEMENT | GRAMMAR | 0.077 | 0.017 |  |
| lt.EN_UPPER_CASE_NGRAM | CASING | 0.066 | 0.017 |  |
| lt.CAFE_DIACRITIC | TYPOGRAPHY | 0.055 | 0 |  |
| lt.EN_COMPOUNDS_GOOD_BYE | MISC | 0.055 | 0.017 |  |
| lt.HE_VERB_AGR | GRAMMAR | 0.055 | 0.069 |  |
| lt.IT_VBZ | GRAMMAR | 0.055 | 0.035 |  |
| lt.ADVERB_WORD_ORDER | STYLE | 0.033 | 0.052 |  |
| lt.IT_IS_2 | TYPOS | 0.022 | 0.052 |  |
| lt.PRP_PAST_PART | GRAMMAR | 0.011 | 0.052 |  |
| lt.BEEN_PART_AGREEMENT | GRAMMAR | 0 | 0.104 |  |
| lt.EN_GB_SIMPLE_REPLACE_SWEATER | STYLE | 0 | 0.087 |  |

184 other rules flagged something but stayed under the bar.

## Planted errors

Caught: share of planted errors that were flagged. First fix right and fix in top 5: of the caught ones, how often a suggestion restores the original text.

### all, dev

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 100 | 96% |  | 90.2% | 88.5% | 97.9% |
| confusable | 1000 | 100 | 50% |  | 35.9% | 94% | 94% |
| a-an | 1000 | 100 | 99% |  | 90.9% | 100% | 100% |
| repeated-word | 1000 | 100 | 98% |  | 61.9% | 98% | 99% |
| agreement | 1000 | 100 | 60% |  | 65.5% | 53.3% | 90% |
| dialogue-punctuation | 1000 | 100 | 1% |  | 0.4% | 0% | 0% |

### all, test

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 1000 | 100 | 97% |  | 86.8% | 88.7% | 99% |
| confusable | 1000 | 100 | 44% |  | 32.8% | 97.7% | 97.7% |
| a-an | 1000 | 100 | 100% |  | 87.9% | 100% | 100% |
| repeated-word | 1000 | 100 | 98% |  | 59.9% | 96.9% | 96.9% |
| agreement | 1000 | 100 | 62% |  | 57.1% | 37.1% | 91.9% |
| dialogue-punctuation | 1000 | 100 | 1% |  | 0.2% | 0% | 0% |

### gutenberg, dev

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 100 | 96% |  | 86.6% | 88.5% | 97.9% |
| confusable | 500 | 100 | 50% |  | 34.6% | 94% | 94% |
| a-an | 500 | 100 | 99% |  | 85.4% | 100% | 100% |
| repeated-word | 500 | 100 | 98% |  | 59% | 98% | 99% |
| agreement | 500 | 100 | 60% |  | 61.4% | 53.3% | 90% |
| dialogue-punctuation | 500 | 100 | 1% |  | 0.4% | 0% | 0% |

### gutenberg, test

| Category | Planted | Checked | Caught | Change | harper caught | First fix right | Fix in top 5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| typo | 500 | 100 | 97% |  | 86.6% | 88.7% | 99% |
| confusable | 500 | 100 | 44% |  | 34.2% | 97.7% | 97.7% |
| a-an | 500 | 100 | 100% |  | 86.8% | 100% | 100% |
| repeated-word | 500 | 100 | 98% |  | 60% | 96.9% | 96.9% |
| agreement | 500 | 100 | 62% |  | 56.2% | 37.1% | 91.9% |
| dialogue-punctuation | 500 | 100 | 1% |  | 0.2% | 0% | 0% |
