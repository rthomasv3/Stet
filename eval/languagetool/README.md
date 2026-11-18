# Comparing with LanguageTool

[LanguageTool](https://languagetool.org) is an open-source grammar checker that runs as a server. This
folder sends it the same paragraphs the [evaluation harness](../README.md) checks, so its results can
be put beside Stet's.

LanguageTool is only called here, over the network. None of its code or data is part of Stet.

## What you need

- Prepared books, set up as the [harness readme](../README.md#what-you-need) describes.
- [Node.js](https://nodejs.org) 18 or later. There is nothing to install.
- **A LanguageTool server of your own.** LanguageTool's
  [server instructions](https://dev.languagetool.org/http-server) explain how to run one.

**This sends the text of your books to the server you name.** Use a server you control, and do not
point it at a public service: the text would leave your machine, and for books under copyright that
is not yours to send.

## Running it

Three steps, all from the repository root.

**1. Export a small set of paragraphs.** LanguageTool answers one request per paragraph and is far
slower than a checker running in the same process, so this exports a sample, not everything:

    dotnet run -c Release --project eval/Stet.Eval -- export --out eval/reports/local/lt-items.jsonl --corpus my-books --clean-words 2000 --planted 100

- `--corpus` takes paragraphs from one corpus only, by the name you gave it.
- `--clean-words` is how many words of clean text to take from each book.
- `--planted` is how many planted errors to take per category.

**2. Send them to your server.** The address ends in `/v2`:

    node eval/languagetool/run.mjs eval/reports/local/lt-items.jsonl eval/reports/local/lt-issues.jsonl --url https://your-server/v2

**3. Score what it found.**

    dotnet run -c Release --project eval/Stet.Eval -- run --checker external --name languagetool --checker-version "LanguageTool 6.3" --issues eval/reports/local/lt-issues.jsonl

Step 3 writes `eval/reports/languagetool.md` and `languagetool.json`, in the same form as Stet's own
reports. Put your server's version in `--checker-version`; it is printed at the top of the report.

Both files in `eval/reports/local/` hold book text. Git ignores that folder; keep them there.

### Options for step 2

- `--minutes N`: stop after N minutes (6 by default). Running the same command again carries on from
  where it stopped.
- `--parallel N`: how many requests to send at once (4 by default).
- The server address can be given in the `LANGUAGETOOL_URL` environment variable in place of `--url`.

It gives up after 20 failed requests.

## How LanguageTool is set up

- Whatever rules the server has on by default.
- `en-US` or `en-GB`, matching the spelling each book uses.
- Up to five suggestions are kept for each flag.

## Reading the result

- **The sample is small.** Only paragraphs LanguageTool was actually sent are counted. The report's
  "Not checked" column shows how many were left out, and it will be most of them. Its numbers rest on
  far fewer words than Stet's or Harper's, so treat them as a rough guide.
- LanguageTool is a general-purpose checker and is not built for fiction. Like Harper, it is given no
  word list for a book's names and invented words.
