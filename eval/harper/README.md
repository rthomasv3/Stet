# Comparing with Harper

[Harper](https://github.com/Automattic/harper) is an open-source, offline grammar checker. This folder
runs it over the same paragraphs the [evaluation harness](../README.md) checks, so its results can be
put beside Stet's.

Harper was chosen because it is the nearest thing to Stet that already exists: a light, offline
checker that runs inside an app with no server behind it. It is written in Rust and usually used from
JavaScript, so it is a yardstick here, not an alternative for a .NET app.

Harper is only run here, as a separate program. None of its code or data is part of Stet.

## What you need

- Prepared books, set up as the [harness readme](../README.md#what-you-need) describes.
- [Node.js](https://nodejs.org). Tested with Node 24.
- Harper itself, installed once from this folder:

      cd eval/harper
      npm install

  This fetches `harper.js` 3.2.0, the version the published numbers used.

## Running it

All from the repository root. Harper is run twice: once to find the names it does not know, and once
with the finished word lists.

**1. Export the paragraphs to check, with each book's word list.**

    dotnet run -c Release --project eval/Stet.Eval -- export --out eval/reports/local/items.jsonl --word-lists eval/reports/local/word-lists.json

**2. Run Harper over them with those lists.**

    node eval/harper/run.mjs eval/reports/local/items.jsonl eval/reports/local/harper-first.jsonl --word-lists eval/reports/local/word-lists.json

**3. Find the names the lists are missing.** The lists hold the words Stet does not know. A made-up
name that Stet happens to accept is not on them, so Harper is flagged for it every time. This adds
any capitalized word Harper flagged as misspelled twice or more in a book:

    dotnet run -c Release --project eval/Stet.Eval -- names --issues eval/reports/local/harper-first.jsonl --out eval/reports/local/extra-names.json

**4. Export again with the finished lists, and run Harper again.**

    dotnet run -c Release --project eval/Stet.Eval -- export --out eval/reports/local/items.jsonl --word-lists eval/reports/local/word-lists.json --extra-words eval/reports/local/extra-names.json
    node eval/harper/run.mjs eval/reports/local/items.jsonl eval/reports/local/harper-words-issues.jsonl --word-lists eval/reports/local/word-lists.json

**5. Score what it found.**

    dotnet run -c Release --project eval/Stet.Eval -- run --checker external --name harper-words --checker-version "harper.js 3.2.0, given each book's word list" --issues eval/reports/local/harper-words-issues.jsonl

Step 5 writes `eval/reports/harper-words.md` and `harper-words.json`, in the same form as Stet's own
reports. `--checker-version` is printed at the top of the report, so the numbers always say what they
are for.

To measure Stet with the same finished lists, give its run the same extra names:

    dotnet run -c Release --project eval/Stet.Eval -- run --checker stet --compare harper-words --extra-words eval/reports/local/extra-names.json

Everything in `eval/reports/local/` holds text or words from the books. Git ignores that folder; keep
it there.

### Without a word list

To see Harper as it is when first installed, leave `--word-lists` off step 2 and score that:

    node eval/harper/run.mjs eval/reports/local/items.jsonl eval/reports/local/harper-issues.jsonl
    dotnet run -c Release --project eval/Stet.Eval -- run --checker external --name harper --checker-version "harper.js 3.2.0" --issues eval/reports/local/harper-issues.jsonl

### If it takes too long

A Harper run stops after 30 minutes by default and says so. Running the same command again carries on from
where it stopped. Paragraphs it never reached are shown in the report's "Not checked" column and are
left out of the numbers.

- `--minutes N` changes the time limit.
- `--shard 0/4`, `1/4`, `2/4`, `3/4` splits the work so four copies can run at once. Give each its own
  output file, then join the files into one before scoring.

## How Harper is set up

- Its default rules, with nothing switched on or off.
- Each book's word list added to its dictionary with `importWords`, for the reports named `harper-words`.
  None for the reports named `harper`.
- Plain-text mode.
- The American or British dialect, matching the spelling each book uses.

## The "in scope" report

Harper checks more kinds of thing than Stet does. It also flags style and readability, such as long
sentences and a missing Oxford comma, which Stet does not attempt. Counting those flags as false alarms would
make the comparison unfair to Harper.

So each Harper report has a second one made from the same run with those kinds of flag left out:
readability, style, formatting, enhancement, redundancy and regionalism. `harper-words-inscope` goes
with `harper-words`, and `harper-inscope` with `harper`. No new Harper run is needed:

    dotnet run -c Release --project eval/Stet.Eval -- run --checker external --source-name harper-words --name harper-words-inscope --checker-version "harper.js 3.2.0, given each book's word list, style and readability flags left out" --issues eval/reports/local/harper-words-issues.jsonl --exclude-categories Readability,Style,Formatting,Enhancement,Redundancy,Regionalism

## Reading the result

- Harper is a general-purpose checker and is not built for fiction.
- Compare like with like. `stet` and `harper-words` both have each book's word list. `spelling` and
  `harper` both have none.
- Adding a name to Harper does not cover its possessive or the same name in capitals, so those are
  still flagged after the lists are given. That is how Harper behaves, and it is counted.
- The lists favour Harper slightly: step 3 forgives any capitalized word Harper flags twice, whether
  it is a made-up name or a real one Harper's dictionary lacks.
