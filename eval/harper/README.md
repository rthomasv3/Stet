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

Three steps, all from the repository root.

**1. Export the paragraphs to check.**

    dotnet run -c Release --project eval/Stet.Eval -- export --out eval/reports/local/items.jsonl

**2. Run Harper over them.**

    node eval/harper/run.mjs eval/reports/local/items.jsonl eval/reports/local/harper-issues.jsonl

**3. Score what it found.**

    dotnet run -c Release --project eval/Stet.Eval -- run --checker external --name harper --checker-version "harper.js 3.2.0" --issues eval/reports/local/harper-issues.jsonl

Step 3 writes `eval/reports/harper.md` and `harper.json`, in the same form as Stet's own reports.
`--checker-version` is printed at the top of the report, so the numbers always say what they are for.

Both files in `eval/reports/local/` hold book text. Git ignores that folder; keep them there.

### If it takes too long

Step 2 stops after 30 minutes by default and says so. Running the same command again carries on from
where it stopped. Paragraphs it never reached are shown in the report's "Not checked" column and are
left out of the numbers.

- `--minutes N` changes the time limit.
- `--shard 0/4`, `1/4`, `2/4`, `3/4` splits the work so four copies can run at once. Give each its own
  output file, then join the files into one before step 3.

## How Harper is set up

- Its default rules, with nothing switched on or off.
- Plain-text mode.
- The American or British dialect, matching the spelling each book uses.

## The "in scope" report

Harper checks more kinds of thing than Stet does. It also flags style and readability, such as long
sentences and a missing Oxford comma, which Stet does not attempt. Counting those flags as false alarms would
make the comparison unfair to Harper.

So there is a second report, `harper-inscope`, made from the same run with those kinds of flag left
out: readability, style, formatting, enhancement, redundancy and regionalism. It needs steps 1 and 2
only once:

    dotnet run -c Release --project eval/Stet.Eval -- run --checker external --source-name harper --name harper-inscope --checker-version "harper.js 3.2.0, style and readability flags left out" --issues eval/reports/local/harper-issues.jsonl --exclude-categories Readability,Style,Formatting,Enhancement,Redundancy,Regionalism

## Reading the result

- Harper is a general-purpose checker and is not built for fiction.
- In the published numbers about half of Harper's flags are spelling flags. Harper is given no word
  list, so it has no way to know a book's names and invented words.
- Stet's `stet` report is given a stand-in for the writer's word list (see the
  [harness readme](../README.md#running-it)). Stet's `spelling` report is given none, as Harper is, so
  compare spelling with that one.
