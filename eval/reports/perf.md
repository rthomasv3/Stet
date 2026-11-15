# Spelling checker speed

Measured on this machine, on the first 100,000 words of the longest book in each corpus. "No word list" is the worst case: every name is unknown and gets suggestions.

| Operation | Target | calibre | gutenberg |
|---|---:|---:|---:|
| Create a checker (first time in the process) | 500 ms | 37 ms | 37 ms |
| Create a checker (again) | 500 ms | 11 ms | 11 ms |
| 100,000-word novel, no word list | 3 s | 0.89 s | 0.27 s |
| 100,000-word novel, with word list | 3 s | 0.24 s | 0.18 s |
| 1,500-word scene, with word list (median / worst) | 50 ms | 2.8 ms / 13.0 ms | 1.6 ms / 14.9 ms |
| One paragraph, with word list (median / worst) | 5 ms / 20 ms | 0.01 ms / 5.6 ms | 0.01 ms / 8.2 ms |
| Suggestions for one word (median / worst) | 5 ms | 0.57 ms / 1.0 ms | 0.55 ms / 5.1 ms |
| Replace a 20,000-word word set | 50 ms | 19 ms | 7 ms |
| Recheck the novel through the cache, nothing changed |  | 2.5 ms | 1.4 ms |
| Recheck the novel through the cache, one paragraph edited (median / worst) |  | 1.7 ms / 11.0 ms | 1.4 ms / 2.8 ms |
| Recheck the novel through the cache, one word added to the word list |  | 210 ms | 172 ms |

Memory held by a loaded checker: about 14 MB (target under 100 MB).
