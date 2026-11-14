// Runs harper.js over paragraphs exported by the evaluation harness and writes its lints in the
// format the harness's external checker reads.
//
//   node run.mjs <items.jsonl> <issues.jsonl> [--minutes 30] [--shard 0/4] [--dialect American]
//
// Stops cleanly when the time budget runs out; paragraphs it did not reach count as "not checked"
// in the report. Running it again continues where it stopped.
import fs from 'node:fs';
import readline from 'node:readline';
import * as harper from 'harper.js';
import { binary } from 'harper.js/binary';

const [itemsPath, outPath, ...rest] = process.argv.slice(2);
if (!itemsPath || !outPath) {
  console.error('usage: node run.mjs <items.jsonl> <issues.jsonl> [--minutes N] [--shard I/N] [--dialect NAME]');
  process.exit(2);
}
const opt = (name, fallback) => {
  const at = rest.indexOf(name);
  return at >= 0 ? rest[at + 1] : fallback;
};
const minutes = Number(opt('--minutes', '30'));
const [shard, shards] = opt('--shard', '0/1').split('/').map(Number);
const dialect = harper.Dialect[opt('--dialect', 'American')];

const done = new Set();
if (fs.existsSync(outPath)) {
  for (const line of fs.readFileSync(outPath, 'utf8').split('\n')) {
    if (line) done.add(JSON.parse(line).id);
  }
}

const linter = new harper.LocalLinter({ binary, dialect });
await linter.setup();

const out = fs.createWriteStream(outPath, { flags: 'a' });
const deadline = Date.now() + minutes * 60_000;
let index = -1;
let checked = 0;
let stoppedEarly = false;

for await (const line of readline.createInterface({ input: fs.createReadStream(itemsPath) })) {
  if (!line) continue;
  index++;
  if (index % shards !== shard) continue;
  const { id, text } = JSON.parse(line);
  if (done.has(id)) continue;
  if (Date.now() > deadline) {
    stoppedEarly = true;
    break;
  }

  const issues = [];
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
