# Harper baseline

Runs [harper.js](https://github.com/Automattic/harper) (Apache-2.0, dev-only, nothing ships) over the
same paragraphs the harness checks, so there is a fixed "better than Harper on fiction" line.

    npm install
    dotnet run --project ../FictionChecker.Eval -- export --out ../reports/local/items.jsonl
    node run.mjs ../reports/local/items.jsonl ../reports/local/harper-issues.jsonl --minutes 30
    dotnet run --project ../FictionChecker.Eval -- run --checker external --name harper --issues eval/reports/local/harper-issues.jsonl

The exported items and Harper's output contain book text and stay in `reports/local/`.
`--shard 0/4` … `3/4` splits the work across processes (write each to its own file, then concatenate).

Harper runs with its default rules, plain-text mode and the American dialect.
