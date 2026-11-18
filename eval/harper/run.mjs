// Runs harper.js over paragraphs exported by the evaluation harness and writes its lints in the
// format the harness's external checker reads.
//
//   node run.mjs <items.jsonl> <issues.jsonl> [--word-lists lists.json] [--minutes 30] [--shard 0/4]
//
// --word-lists gives Harper each book's word list (written by the harness's export), added to its
// dictionary before that book's paragraphs are checked.
//
// Stops cleanly when the time budget runs out; paragraphs it did not reach count as "not checked"
// in the report. Running it again continues where it stopped.
import fs from 'node:fs';
import readline from 'node:readline';
import * as harper from 'harper.js';
import { binary } from 'harper.js/binary';

const [itemsPath, outPath, ...rest] = process.argv.slice(2);
if (!itemsPath || !outPath) {
  console.error('usage: node run.mjs <items.jsonl> <issues.jsonl> [--word-lists FILE] [--minutes N] [--shard I/N]');
  process.exit(2);
}
const opt = (name, fallback) => {
  const at = rest.indexOf(name);
  return at >= 0 ? rest[at + 1] : fallback;
};
const minutes = Number(opt('--minutes', '30'));
const [shard, shards] = opt('--shard', '0/1').split('/').map(Number);

const done = new Set();
if (fs.existsSync(outPath)) {
  for (const line of fs.readFileSync(outPath, 'utf8').split('\n')) {
    if (line) done.add(JSON.parse(line).id);
  }
}

const wordListsPath = opt('--word-lists', null);
const wordLists = wordListsPath ? JSON.parse(fs.readFileSync(wordListsPath, 'utf8')) : null;

// One linter per dialect, or with word lists one per book: each item says which spelling its book uses
// and which book it is from.
const dialects = { US: harper.Dialect.American, GB: harper.Dialect.British };
const linters = {};
async function linterFor(code, book) {
  const dialect = code in dialects ? code : 'US';
  const key = wordLists ? `${dialect} ${book}` : dialect;
  if (!linters[key]) {
    linters[key] = new harper.LocalLinter({ binary, dialect: dialects[dialect] });
    await linters[key].setup();
    if (wordLists) await linters[key].importWords(wordLists[book] ?? []);
  }
  return linters[key];
}

const out = fs.createWriteStream(outPath, { flags: 'a' });
const deadline = Date.now() + minutes * 60_000;
let index = -1;
let checked = 0;
let stoppedEarly = false;

for await (const line of readline.createInterface({ input: fs.createReadStream(itemsPath) })) {
  if (!line) continue;
  index++;
  if (index % shards !== shard) continue;
  const { id, text, dialect, book } = JSON.parse(line);
  if (done.has(id)) continue;
  if (Date.now() > deadline) {
    stoppedEarly = true;
    break;
  }

  const issues = [];
  const linter = await linterFor(dialect, book);
  const organized = await linter.organizedLints(text, { language: 'plaintext' });
  for (const [ruleId, lints] of Object.entries(organized)) {
    for (const lint of lints) {
      const span = lint.span();
      const start = span.start;
      const length = span.end - span.start;
      // Harper's suggestions all act on the flagged span: replace it, remove it, or insert after it.
      const suggestions = lint.suggestions().map((s) => {
        const kind = s.kind();
        const replacement = s.get_replacement_text();
        if (kind === harper.SuggestionKind.Remove) return [{ start, length, text: '' }];
        if (kind === harper.SuggestionKind.InsertAfter) return [{ start: start + length, length: 0, text: replacement }];
        return [{ start, length, text: replacement }];
      });
      issues.push({ start, length, ruleId: `harper.${ruleId}`, category: lint.lint_kind(), suggestions });
    }
  }
  issues.sort((a, b) => a.start - b.start);
  out.write(JSON.stringify({ id, issues }) + '\n');
  checked++;
}

await new Promise((resolve) => out.end(resolve));
console.log(`shard ${shard}/${shards}: checked ${checked}${stoppedEarly ? ' (time budget reached; run again to continue)' : ''}`);
