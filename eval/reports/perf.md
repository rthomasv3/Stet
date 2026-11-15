# Spelling checker speed

Measured on this machine, on the first 100,000 words of the longest book in each corpus. "No word list" is the worst case: every name is unknown and gets suggestions.

| Operation | Target | calibre | gutenberg |
|---|---:|---:|---:|
| Create a checker (first time in the process) | 500 ms | 20 ms | 20 ms |
| Create a checker (again) | 500 ms | 9 ms | 9 ms |
| 100,000-word novel, no word list | 3 s | 0.81 s | 0.24 s |
| 100,000-word novel, with word list | 3 s | 0.20 s | 0.15 s |
| 1,500-word scene, with word list (median / worst) | 50 ms | 2.4 ms / 16.1 ms | 1.3 ms / 14.7 ms |
| One paragraph, with word list (median / worst) | 5 ms / 20 ms | 0.00 ms / 11.7 ms | 0.00 ms / 11.7 ms |
| Suggestions for one word (median / worst) | 5 ms | 0.60 ms / 4.7 ms | 0.55 ms / 0.9 ms |
| Replace a 20,000-word word set | 50 ms | 11 ms | 8 ms |
| Recheck the novel through the cache, nothing changed |  | 2.7 ms | 1.5 ms |
| Recheck the novel through the cache, one paragraph edited (median / worst) |  | 2.3 ms / 15.6 ms | 1.5 ms / 7.3 ms |
| Recheck the novel through the cache, one word added to the word list |  | 186 ms | 151 ms |

Memory held by a loaded checker: about 12 MB (target under 100 MB).
