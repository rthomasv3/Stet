// Runs a LanguageTool server over paragraphs exported by the evaluation harness and writes its
// matches in the format the harness's external checker reads.
//
//   node run.mjs <items.jsonl> <issues.jsonl> --url https://your-server/v2 [--minutes 6] [--parallel 4]
//
// LanguageTool is slow next to an in-process checker, so export a small set (see README). Stops
// cleanly at the time budget; paragraphs not reached count as "not checked". Run again to continue.
import fs from 'node:fs';

const [itemsPath, outPath, ...rest] = process.argv.slice(2);
const opt = (name, fallback) => {
  const at = rest.indexOf(name);
  return at >= 0 ? rest[at + 1] : fallback;
};
const url = opt('--url', process.env.LANGUAGETOOL_URL);
if (!itemsPath || !outPath || !url) {
  console.error('usage: node run.mjs <items.jsonl> <issues.jsonl> --url <server>/v2 [--minutes N] [--parallel N]');
  process.exit(2);
}
const minutes = Number(opt('--minutes', '6'));
const parallel = Number(opt('--parallel', '4'));
const languages = { US: 'en-US', GB: 'en-GB' };

const done = new Set();
if (fs.existsSync(outPath)) {
  for (const line of fs.readFileSync(outPath, 'utf8').split('\n')) {
    if (line) done.add(JSON.parse(line).id);
  }
}
const items = fs.readFileSync(itemsPath, 'utf8').split('\n').filter(Boolean).map((l) => JSON.parse(l)).filter((i) => !done.has(i.id));
const out = fs.createWriteStream(outPath, { flags: 'a' });
const deadline = Date.now() + minutes * 60_000;
let next = 0;
let checked = 0;
let failed = 0;

async function check(item) {
  const body = new URLSearchParams({ text: item.text, language: languages[item.dialect] ?? 'en-US' });
  const response = await fetch(`${url.replace(/\/$/, '')}/check`, { method: 'POST', body, signal: AbortSignal.timeout(30_000) });
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  const result = await response.json();
  // LanguageTool offsets are UTF-16 units, the same as the harness uses.
  return result.matches.map((m) => ({
    start: m.offset,
    length: m.length,
    ruleId: `lt.${m.rule.id}`,
    category: m.rule.category?.id ?? '',
    suggestions: m.replacements.slice(0, 5).map((r) => [{ start: m.offset, length: m.length, text: r.value }]),
  }));
}

async function worker() {
  while (next < items.length && Date.now() < deadline && failed < 20) {
    const item = items[next++];
    try {
      const issues = await check(item);
      out.write(JSON.stringify({ id: item.id, issues }) + '\n');
      checked++;
    } catch (error) {
      failed++;
      if (failed <= 3) console.error(`request failed: ${error.message}`);
    }
  }
}

await Promise.all(Array.from({ length: parallel }, worker));
await new Promise((resolve) => out.end(resolve));
const left = items.length - checked;
console.log(`checked ${checked}, failed ${failed}, not reached ${Math.max(0, left - failed)}`);
