# Spelling checker speed

Measured on this machine, on the first 100,000 words of the longest book in each corpus. "No word list" is the worst case: every name is unknown and gets suggestions.

| Operation | Target | calibre | gutenberg |
|---|---:|---:|---:|
| Create a checker (first time in the process) | 500 ms | 19 ms | 19 ms |
| Create a checker (again) | 500 ms | 10 ms | 10 ms |
| 100,000-word novel, no word list | 3 s | 0.79 s | 0.24 s |
| 100,000-word novel, with word list | 3 s | 0.19 s | 0.15 s |
| 1,500-word scene, with word list (median / worst) | 50 ms | 2.2 ms / 23.6 ms | 1.2 ms / 14.4 ms |
| One paragraph, with word list (median / worst) | 5 ms / 20 ms | 0.00 ms / 11.3 ms | 0.00 ms / 8.0 ms |
| Suggestions for one word (median / worst) | 5 ms | 0.56 ms / 1.0 ms | 0.53 ms / 0.9 ms |
| Replace a 20,000-word word set | 50 ms | 12 ms | 19 ms |

Memory held by a loaded checker: about 12 MB (target under 100 MB).
