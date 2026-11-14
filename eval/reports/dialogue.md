# Text splitting and dialogue detection

Whole books, not the evaluation sample. The quote style is guessed per book from how its paragraphs open.

| Corpus | Split | Books | Single-quote books | Words | In dialogue | Paragraphs | Unmatched marks per 1,000 paragraphs | Paragraphs continuing speech | Words per sentence | Worst book: unmatched per 1,000 |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| calibre | dev | 16 | 4 | 3,630,106 | 34.8% | 93,734 | 5.4 | 1.8% | 11.3 | 53.4 |
| calibre | test | 8 | 2 | 1,676,349 | 31.9% | 56,767 | 1.4 | 0.9% | 12.2 | 6.1 |
| gutenberg | dev | 29 | 2 | 2,780,523 | 38.2% | 54,812 | 2.6 | 2.9% | 17.0 | 66.2 |
| gutenberg | test | 11 | 2 | 898,750 | 40.5% | 15,123 | 5.5 | 3.5% | 20.0 | 28.7 |

