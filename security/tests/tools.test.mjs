import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import test from 'node:test';

const root = resolve(import.meta.dirname, '../..');
const evidencePath = resolve(root, 'security/evidence/evidence.jsonl');

test('baseline evaluator runs', () => {
  const output = execFileSync(process.execPath, ['security/tools/evaluate.mjs'], { cwd: root, encoding: 'utf8' });
  assert.match(output, /Evaluated 8 controls/);
});

test('exception validator runs', () => {
  const output = execFileSync(process.execPath, ['security/tools/validate-exceptions.mjs'], { cwd: root, encoding: 'utf8' });
  assert.match(output, /expired: 0/);
});

test('Sonar and ZAP normalize findings while production ZAP is rejected', () => {
  const dir = mkdtempSync(join(tmpdir(), 'admisiones-security-'));
  const sonar = join(dir, 'sonar.json');
  const zap = join(dir, 'zap.json');
  const original = readFileSync(evidencePath, 'utf8');
  writeFileSync(sonar, JSON.stringify({ issues: [{ rule: 'javascript:S1523', component: 'admisiones:src/app.ts', line: 7, message: 'dynamic code' }] }));
  writeFileSync(zap, JSON.stringify({ '@version': '2.16.1', site: [{ alerts: [{ pluginid: '10010', name: 'Cookie without HttpOnly' }] }] }));
  try {
    execFileSync(process.execPath, ['security/tools/import-sonar.mjs', sonar], { cwd: root });
    execFileSync(process.execPath, ['security/tools/import-zap.mjs', zap, 'testing'], { cwd: root });
    const added = readFileSync(evidencePath, 'utf8').slice(original.length).trim().split(/\r?\n/).map(JSON.parse);
    assert.deepEqual(added.map(item => [item.source, item.result, item.controlId]), [
      ['sonar', 'FAIL', 'v5.0.0-2.2.2'],
      ['zap', 'FAIL', 'v5.0.0-3.3.4']
    ]);
    const rejected = spawnSync(process.execPath, ['security/tools/import-zap.mjs', zap, 'production'], { cwd: root, encoding: 'utf8' });
    assert.notEqual(rejected.status, 0);
  } finally {
    writeFileSync(evidencePath, original);
  }
});

