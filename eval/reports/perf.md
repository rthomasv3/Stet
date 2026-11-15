# Spelling checker speed

Measured on this machine, on the first 100,000 words of the longest book in each corpus. "No word list" is the worst case: every name is unknown and gets suggestions.

| Operation | Target | calibre | gutenberg |
|---|---:|---:|---:|
| Create a checker (first time in the process) | 500 ms | 41 ms | 41 ms |
| Create a checker (again) | 500 ms | 12 ms | 12 ms |
| 100,000-word novel, no word list | 3 s | 0.52 s | 0.19 s |
| 100,000-word novel, with word list | 3 s | 0.36 s | 0.15 s |
| 1,500-word scene, with word list (median / worst) | 50 ms | 4.1 ms / 14.1 ms | 1.5 ms / 11.9 ms |
| One paragraph, with word list (median / worst) | 5 ms / 20 ms | 0.03 ms / 5.7 ms | 0.01 ms / 6.6 ms |
| Suggestions for one word (median / worst) | 5 ms | 0.47 ms / 1.2 ms | 0.44 ms / 0.6 ms |
| Replace a 20,000-word word set | 50 ms | 10 ms | 17 ms |
| Recheck the novel through the cache, nothing changed |  | 2.7 ms | 1.6 ms |
| Recheck the novel through the cache, one paragraph edited (median / worst) |  | 2.4 ms / 12.1 ms | 1.4 ms / 2.6 ms |
| Recheck the novel through the cache, one word added to the word list |  | 67 ms | 56 ms |

Memory held by a loaded checker: about 17 MB (target under 100 MB).
