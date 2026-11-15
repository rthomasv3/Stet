# Spelling checker speed

Measured on this machine, on the first 100,000 words of the longest book in each corpus. "No word list" is the worst case: every name is unknown and gets suggestions.

| Operation | Target | calibre | gutenberg |
|---|---:|---:|---:|
| Create a checker (first time in the process) | 500 ms | 39 ms | 39 ms |
| Create a checker (again) | 500 ms | 12 ms | 12 ms |
| 100,000-word novel, no word list | 3 s | 0.44 s | 0.17 s |
| 100,000-word novel, with word list | 3 s | 0.28 s | 0.13 s |
| 1,500-word scene, with word list (median / worst) | 50 ms | 3.2 ms / 10.9 ms | 1.4 ms / 11.9 ms |
| One paragraph, with word list (median / worst) | 5 ms / 20 ms | 0.02 ms / 5.7 ms | 0.01 ms / 6.5 ms |
| Suggestions for one word (median / worst) | 5 ms | 0.44 ms / 5.1 ms | 0.44 ms / 0.6 ms |
| Replace a 20,000-word word set | 50 ms | 11 ms | 7 ms |
| Recheck the novel through the cache, nothing changed |  | 2.9 ms | 1.4 ms |
| Recheck the novel through the cache, one paragraph edited (median / worst) |  | 2.6 ms / 12.2 ms | 1.4 ms / 2.3 ms |
| Recheck the novel through the cache, one word added to the word list |  | 49 ms | 44 ms |

Memory held by a loaded checker: about 14 MB (target under 100 MB).
