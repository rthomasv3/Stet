# Side test set

A small made-up set for a quick look at breadth: 76 sentences with one error each (the error is between
`[[` and `]]`) and 36 correct fiction lines of the kind that trip checkers (`CLEAN`). No book text.

    dotnet run -c Release --project eval/Stet.Eval -- sideset

It is a side check, not a target: ten sentences a category is too few to tune against. Changes are judged
on the book measurements. Results when it was written (2026-11-15), each checker at its defaults:

| | Library | Harper | LanguageTool |
|---|---:|---:|---:|
| Errors caught (of 76) | 45 | 48 | 59 |
| Correct lines flagged (of 36) | 2 | 15 | 7 |

The library's gaps on this set were then worked through (new rules for verb forms, plural subjects,
lowercase "i", comma spacing, and seven more confusable pairs), each judged on the book measurements.
After that work, and a second round of small rules, the library catches 66 of 76, with the same 2 correct lines flagged (invented names).
That figure is not comparable with the other two: the rules were written knowing these sentences.
