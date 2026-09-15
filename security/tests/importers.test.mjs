import assert from 'node:assert/strict';
import { mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { execFileSync as run, spawnSync } from 'node:child_process';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import test from 'node:test';

const root = resolve(import.meta.dirname, '../..');
const sonarMapping = resolve(root, 'security/asvs/applicability/sonar-mapping.json');
const zapMapping = resolve(root, 'security/asvs/applicability/zap-mapping.json');
const env = { ...process.env, GITHUB_SHA: 'c'.repeat(40) };

function lines(path) {
  return readFileSync(path, 'utf8').split(/\r?\n/).filter(Boolean).map(JSON.parse);
}

test('ZAP imports mapped alerts across sites once and rejects unsafe or malformed input', () => {
  const dir = mkdtempSync(join(tmpdir(), 'admisiones-zap-'));
  const input = join(dir, 'zap.json');
  const output = join(dir, 'evidence.jsonl');
  writeFileSync(output, '');
  writeFileSync(input, JSON.stringify({
    '@version': '2.16.1',
    site: [
      { alerts: [{ pluginid: '10010', name: 'Cookie flag', riskdesc: 'High (3)' }, { pluginid: '99999', name: 'Unknown' }] },
      { alerts: [{ pluginid: '10010', name: 'Cookie flag duplicate', riskdesc: 'High (3)' }] }
    ]
  }));
  run(process.execPath, ['security/tools/import-zap.mjs', input, 'testing', zapMapping, output], { cwd: root, env });
  const imported = lines(output);
  assert.equal(imported.length, 1);
  assert.equal(imported[0].controlId, 'v5.0.0-3.3.4');
  assert.equal(imported[0].scannerSeverity, 'High');
  run(process.execPath, ['security/tools/import-zap.mjs', input, 'testing', zapMapping, output], { cwd: root, env });
  assert.equal(lines(output).length, 1);

  writeFileSync(input, '{');
  assert.match(spawnSync(process.execPath, ['security/tools/import-zap.mjs', input, 'testing', zapMapping, output], { cwd: root, env, encoding: 'utf8' }).stderr, /Malformed ZAP/);
  assert.notEqual(spawnSync(process.execPath, ['security/tools/import-zap.mjs', input, 'production', zapMapping, output], { cwd: root, env }).status, 0);
  assert.notEqual(spawnSync(process.execPath, ['security/tools/import-zap.mjs', input, 'invalid', zapMapping, output], { cwd: root, env }).status, 0);
});

test('ZAP accepts an empty report', () => {
  const dir = mkdtempSync(join(tmpdir(), 'admisiones-zap-empty-'));
  const input = join(dir, 'zap.json');
  const output = join(dir, 'evidence.jsonl');
  writeFileSync(input, JSON.stringify({ site: [] }));
  writeFileSync(output, '');
  run(process.execPath, ['security/tools/import-zap.mjs', input, 'development', zapMapping, output], { cwd: root, env });
  assert.equal(readFileSync(output, 'utf8'), '');
});

test('Sonar imports known frontend/backend issues, severities and skips unknown rules', () => {
  const dir = mkdtempSync(join(tmpdir(), 'admisiones-sonar-'));
  const input = join(dir, 'sonar.json');
  const output = join(dir, 'evidence.jsonl');
  writeFileSync(output, '');
  writeFileSync(input, JSON.stringify({ issues: [
    { rule: 'javascript:S1523', component: 'admisiones:src/app.ts', line: 7, severity: 'CRITICAL', message: 'not persisted' },
    { rule: 'csharpsquid:S2068', component: 'api:Program.cs', line: 8, severity: 'MAJOR', message: 'not persisted' },
    { rule: 'unknown:S1', component: 'api:Other.cs', severity: 'INFO' }
  ] }));
  run(process.execPath, ['security/tools/import-sonar.mjs', input, sonarMapping, output], { cwd: root, env });
  const imported = lines(output);
  assert.deepEqual(imported.map(item => [item.component, item.scannerSeverity, item.controlId]), [
    ['frontend', 'CRITICAL', 'v5.0.0-2.2.2'],
    ['backend', 'MAJOR', 'v5.0.0-13.3.1']
  ]);
  assert.ok(imported.every(item => !item.notes.includes('not persisted')));
});

test('Sonar rejects malformed reports and invalid environments', () => {
  const dir = mkdtempSync(join(tmpdir(), 'admisiones-sonar-bad-'));
  const input = join(dir, 'sonar.json');
  const output = join(dir, 'evidence.jsonl');
  writeFileSync(input, '{');
  assert.match(spawnSync(process.execPath, ['security/tools/import-sonar.mjs', input, sonarMapping, output], { cwd: root, env, encoding: 'utf8' }).stderr, /Malformed Sonar/);
  writeFileSync(input, JSON.stringify({ issues: [] }));
  const invalid = { ...env, SONAR_ENVIRONMENT: 'invalid' };
  assert.notEqual(spawnSync(process.execPath, ['security/tools/import-sonar.mjs', input, sonarMapping, output], { cwd: root, env: invalid }).status, 0);
});
