# LanguageTool comparison

A second point of comparison beside Harper, using a LanguageTool server you run yourself.
LanguageTool is a reference for measuring only: none of its code or data is used in the library.

It is much slower than Harper, so it checks a small set:

    dotnet run --project ../Stet.Eval -- export --out ../reports/local/lt-items.jsonl --corpus gutenberg --clean-words 2000 --planted 100
    node run.mjs ../reports/local/lt-items.jsonl ../reports/local/lt-issues.jsonl --url https://your-server/v2 --minutes 6
    dotnet run --project ../Stet.Eval -- run --checker external --name languagetool --issues eval/reports/local/lt-issues.jsonl

Only paragraphs LanguageTool actually checked are counted, so its numbers rest on far fewer words than
the other reports; the "Not checked" column shows how much was left out. The export above sends
public-domain text only. Needs Node 18 or later; no packages.
