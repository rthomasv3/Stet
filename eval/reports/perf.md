# Spelling checker speed

Measured on this machine, on the first 100,000 words of the longest book in each corpus. "No word list" is the worst case: every name is unknown and gets suggestions.

| Operation | Target | calibre | gutenberg |
|---|---:|---:|---:|
| Create a checker (first time in the process) | 500 ms | 41 ms | 41 ms |
| Create a checker (again) | 500 ms | 12 ms | 12 ms |
| 100,000-word novel, no word list | 3 s | 0.49 s | 0.17 s |
| 100,000-word novel, with word list | 3 s | 0.32 s | 0.14 s |
| 1,500-word scene, with word list (median / worst) | 50 ms | 3.6 ms / 11.5 ms | 1.3 ms / 11.3 ms |
| One paragraph, with word list (median / worst) | 5 ms / 20 ms | 0.02 ms / 5.7 ms | 0.01 ms / 6.3 ms |
| Suggestions for one word (median / worst) | 5 ms | 0.42 ms / 0.7 ms | 0.43 ms / 4.9 ms |
| Replace a 20,000-word word set | 50 ms | 23 ms | 11 ms |
| Recheck the novel through the cache, nothing changed |  | 2.8 ms | 1.0 ms |
| Recheck the novel through the cache, one paragraph edited (median / worst) |  | 2.4 ms / 9.4 ms | 1.0 ms / 12.0 ms |
| Recheck the novel through the cache, one word added to the word list |  | 58 ms | 49 ms |

Memory held by a loaded checker: about 16 MB (target under 100 MB).
