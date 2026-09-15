import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { mkdirSync, mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import test from 'node:test';
import { evidenceFingerprint, validateEvidence, validateException } from '../tools/security-model.mjs';

const root = resolve(import.meta.dirname, '../..');
const controls = JSON.parse(readFileSync(resolve(root, 'security/asvs/controls/pilot.json'), 'utf8'));
const controlIds = new Set(controls.map(control => control.id));
const sha = 'a'.repeat(40);

function validEvidence(overrides = {}) {
  const item = {
    controlId: 'v5.0.0-6.8.2', application: 'admisiones', component: 'backend',
    repository: 'DesarrolloORT/admisiones-mono', commitSha: sha, source: 'automated-test',
    environment: 'testing', timestamp: '2026-09-15T12:00:00Z', result: 'PASS',
    test: 'signature is rejected', ...overrides
  };
  item.fingerprint = evidenceFingerprint(item);
  return item;
}

function validException(overrides = {}) {
  return {
    control: 'v5.0.0-2.2.2', reason: 'Temporary mitigation', risk: 'high',
    approvedBy: 'Security owner', createdAt: '2026-01-01', expiresAt: '2026-12-31',
    ticket: 'SEC-1', ...overrides
  };
}

test('evidence validates fields and derives a stable fingerprint', () => {
  const first = validEvidence();
  const second = { ...first, timestamp: '2026-09-15T13:00:00Z' };
  delete second.fingerprint;
  assert.equal(evidenceFingerprint(first), evidenceFingerprint(second));
  assert.equal(validateEvidence([second], controls)[0].fingerprint, evidenceFingerprint(first));
  for (const bad of [
    validEvidence({ environment: undefined }),
    validEvidence({ component: 'database' }),
    validEvidence({ controlId: 'v5.0.0-3.3.4', component: 'backend' }),
    validEvidence({ result: 'OK' }),
    validEvidence({ commitSha: 'not-a-sha' }),
    validEvidence({ timestamp: '2026-09-15' }),
    validEvidence({ timestamp: '2026-02-30T12:00:00Z' }),
    validEvidence({ controlId: 'v5.0.0-99.99.99' }),
    validEvidence({ expiresAt: '2026-09-14T12:00:00Z' })
  ]) {
    delete bad.fingerprint;
    assert.throws(() => validateEvidence([bad], controls));
  }
});

test('duplicate evidence fingerprints are rejected', () => {
  const item = validEvidence();
  assert.throws(() => validateEvidence([item, { ...item }], controls), /duplicate fingerprint/);
});

test('exceptions require approval fields, existing controls, risk and ordered dates', () => {
  assert.equal(validateException(validException(), 'valid.json', controlIds).control, 'v5.0.0-2.2.2');
  for (const bad of [
    validException({ approvedBy: '' }),
    validException({ expiresAt: '' }),
    validException({ control: 'v5.0.0-99.99.99' }),
    validException({ risk: 'extreme' }),
    validException({ createdAt: '2026-02-30' }),
    validException({ expiresAt: '2025-12-31' })
  ]) assert.throws(() => validateException(bad, 'bad.json', controlIds));
});

test('evaluation binds evidence to the commit, reports exceptions and writes deterministic findings', () => {
  const dir = mkdtempSync(join(tmpdir(), 'admisiones-model-'));
  const exceptions = join(dir, 'exceptions');
  const evidence = join(dir, 'evidence.jsonl');
  const summary = join(dir, 'summary.md');
  const findings = join(dir, 'findings.json');
  mkdirSync(exceptions);
  writeFileSync(join(exceptions, 'active.json'), JSON.stringify(validException()));
  writeFileSync(join(exceptions, 'expired.json'), JSON.stringify(validException({
    control: 'v5.0.0-3.3.4', createdAt: '2024-01-01', expiresAt: '2025-01-01', ticket: 'SEC-2'
  })));
  writeFileSync(evidence, [
    JSON.stringify(validEvidence()),
    JSON.stringify(validEvidence({ controlId: 'v5.0.0-16.5.1', commitSha: 'b'.repeat(40), test: 'old commit' }))
  ].join('\n'));
  const args = [
    'security/tools/evaluate.mjs', '--evidence', evidence, '--exceptions', exceptions,
    '--summary', summary, '--findings', findings, '--commit', sha,
    '--now', '2026-09-15T18:00:00Z', '--environment', 'preproduction'
  ];
  execFileSync(process.execPath, args, { cwd: root });
  const first = readFileSync(findings, 'utf8');
  execFileSync(process.execPath, args, { cwd: root });
  assert.equal(readFileSync(findings, 'utf8'), first);
  const document = JSON.parse(first);
  const schema = JSON.parse(readFileSync(resolve(root, 'security/reports/findings.schema.json'), 'utf8'));
  assert.deepEqual(schema.required, ['schemaVersion', 'application', 'repository', 'commitSha', 'generatedAt', 'summary', 'findings']);
  assert.deepEqual(document.summary, { pass: 1, fail: 0, pending: 6, needsReview: 0, acceptedRisk: 1 });
  assert.ok(document.findings.every(item => item.environment === 'preproduction'));
  assert.ok(document.findings.every(item => ['id', 'fingerprint', 'controlId', 'component', 'severity', 'status', 'title', 'description', 'environment', 'evidence'].every(field => field in item)));
  assert.match(readFileSync(summary, 'utf8'), /Active exceptions[\s\S]*v5\.0\.0-2\.2\.2/);
  assert.match(readFileSync(summary, 'utf8'), /Expired exceptions[\s\S]*v5\.0\.0-3\.3\.4/);
  assert.match(readFileSync(summary, 'utf8'), /v5\.0\.0-16\.5\.1[^\n]*\| 0 \| 1 \|/);
});

test('high or critical FAIL evidence activates the release gate', () => {
  const dir = mkdtempSync(join(tmpdir(), 'admisiones-gate-'));
  const exceptions = join(dir, 'exceptions');
  const evidence = join(dir, 'evidence.jsonl');
  const summary = join(dir, 'summary.md');
  const findings = join(dir, 'findings.json');
  mkdirSync(exceptions);
  writeFileSync(evidence, JSON.stringify(validEvidence({
    controlId: 'v5.0.0-2.2.2', result: 'FAIL', test: 'negative validation'
  })));
  const result = spawnSync(process.execPath, [
    'security/tools/evaluate.mjs', '--evidence', evidence, '--exceptions', exceptions,
    '--summary', summary, '--findings', findings, '--commit', sha, '--now', '2026-09-15T18:00:00Z'
  ], { cwd: root, encoding: 'utf8' });
  assert.equal(result.status, 2);
  assert.equal(JSON.parse(readFileSync(findings, 'utf8')).summary.fail, 1);
});

test('catalog statuses cannot bypass exception requirements', () => {
  const dir = mkdtempSync(join(tmpdir(), 'admisiones-status-'));
  const exceptions = join(dir, 'exceptions');
  const evidence = join(dir, 'evidence.jsonl');
  const summary = join(dir, 'summary.md');
  const findings = join(dir, 'findings.json');
  const catalog = join(dir, 'controls.json');
  mkdirSync(exceptions);
  writeFileSync(evidence, '');
  const run = status => {
    writeFileSync(catalog, JSON.stringify(controls.map((control, index) => index ? control : { ...control, status })));
    return spawnSync(process.execPath, [
      'security/tools/evaluate.mjs', '--controls', catalog, '--evidence', evidence, '--exceptions', exceptions,
      '--summary', summary, '--findings', findings, '--commit', sha, '--now', '2026-09-15T18:00:00Z'
    ], { cwd: root, encoding: 'utf8' });
  };
  assert.match(run('NOT_APPLICABLE').stderr, /requires justification/);
  assert.match(run('ACCEPTED_RISK').stderr, /requires an active approved exception/);
});
