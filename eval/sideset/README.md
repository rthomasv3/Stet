# Side test set

A small made-up set for a quick look at breadth: 76 sentences with one error each (the error is between
`[[` and `]]`) and 36 correct fiction lines of the kind that trip checkers (`CLEAN`). No book text.

    dotnet run -c Release --project eval/FictionChecker.Eval -- sideset

It is a side check, not a target: ten sentences a category is too few to tune against. Changes are judged
on the book measurements. Results when it was written (2026-11-15), each checker at its defaults:

| | Library | Harper | LanguageTool |
|---|---:|---:|---:|
| Errors caught (of 76) | 45 | 48 | 59 |
| Correct lines flagged (of 36) | 2 | 15 | 7 |
