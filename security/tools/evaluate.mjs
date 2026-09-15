import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { environments, evidenceSources, readEvidence, readExceptions, sameCommit, stableFingerprint } from './security-model.mjs';

const root = resolve(import.meta.dirname, '../..');
const security = resolve(root, 'security');
function option(name, fallback) { const index = process.argv.indexOf(name); return index < 0 ? fallback : process.argv[index + 1]; }

const controlsPath = option('--controls', resolve(security, 'asvs/controls/pilot.json'));
const evidencePath = option('--evidence', resolve(security, 'evidence/evidence.jsonl'));
const exceptionsPath = option('--exceptions', resolve(security, 'asvs/exceptions'));
const summaryPath = option('--summary', resolve(security, 'reports/asvs-summary.md'));
const findingsPath = option('--findings', resolve(security, 'reports/findings.json'));
const environment = option('--environment', 'testing');
const policy = JSON.parse(readFileSync(resolve(security, 'policies/evaluation.json'), 'utf8'));
const catalog = JSON.parse(readFileSync(controlsPath, 'utf8'));
const controlIdsPath = option('--control-ids-file');
const selectedIds = controlIdsPath ? new Set(readFileSync(controlIdsPath, 'utf8').split(/\r?\n/).filter(Boolean)) : null;
const controls = selectedIds ? catalog.filter(control => selectedIds.has(control.id)) : catalog;
const ids = new Set(controls.map(control => control.id));
const scopes = new Set(['frontend', 'backend', 'system', 'infrastructure']);
const now = new Date(option('--now', new Date().toISOString()));
if (!Number.isFinite(now.valueOf())) throw new Error('Invalid --now date');
if (!environments.includes(environment)) throw new Error(`Invalid evaluated environment: ${environment}`);

let commit = option('--commit', process.env.GITHUB_SHA || process.env.BUILD_SOURCEVERSION);
if (!commit) { try { commit = execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(); } catch { commit = 'unknown'; } }
if (!/^[0-9a-f]{7,40}$/i.test(commit)) throw new Error(`Invalid evaluated commit: ${commit}`);

for (const control of controls) {
  if (!/^v5\.0\.0-\d+\.\d+\.\d+$/.test(control.id)) throw new Error(`Invalid control id: ${control.id}`);
  if (!control.scope?.length || control.scope.some(scope => !scopes.has(scope))) throw new Error(`Invalid scope: ${control.id}`);
  if (!control.verification?.methods?.length || control.verification.methods.some(method => !evidenceSources.includes(method))) throw new Error(`Invalid verification method: ${control.id}`);
  if (!policy.statuses.includes(control.status)) throw new Error(`Invalid status: ${control.id}`);
  if (control.status === 'NOT_APPLICABLE' && !control.justification) throw new Error(`NOT_APPLICABLE requires justification: ${control.id}`);
}
if (ids.size !== controls.length) throw new Error('Duplicate control id');
if (selectedIds && controls.length !== selectedIds.size) throw new Error('Affected controls include an unknown control');

const evidence = readEvidence(evidencePath, catalog);
const exceptions = readExceptions(exceptionsPath, catalog);
const activeExceptions = exceptions.filter(item => Date.parse(`${item.expiresAt}T00:00:00Z`) > now.valueOf());
const expiredExceptions = exceptions.filter(item => !activeExceptions.includes(item));
for (const control of controls.filter(item => item.status === 'ACCEPTED_RISK')) {
  if (!activeExceptions.some(item => item.control === control.id)) throw new Error(`ACCEPTED_RISK requires an active approved exception: ${control.id}`);
}

const maxAge = policy.evidenceMaxAgeDays * 86400000;
const rows = controls.map(control => {
  const linked = evidence.filter(item => item.controlId === control.id);
  const matchingCommit = linked.filter(item => sameCommit(item.commitSha, commit));
  const current = matchingCommit.filter(item => Date.parse(item.timestamp) <= now && (item.expiresAt ? Date.parse(item.expiresAt) > now : now - Date.parse(item.timestamp) <= maxAge));
  const activeException = activeExceptions.find(item => item.control === control.id);
  const trustedPass = current.some(item => item.result === 'PASS' && policy.passTrustSources.includes(item.source));
  const failed = current.some(item => item.result === 'FAIL');
  const reviewPass = current.some(item => item.result === 'PASS');
  const status = control.status === 'NOT_APPLICABLE' ? 'NOT_APPLICABLE'
    : activeException ? 'ACCEPTED_RISK'
      : failed ? 'FAIL' : trustedPass ? 'PASS' : reviewPass ? 'NEEDS_REVIEW' : 'PENDING';
  return { ...control, status, linked, current, otherCommit: linked.length - matchingCommit.length, expired: matchingCommit.length - current.length };
});

const counts = Object.fromEntries(policy.statuses.map(status => [status, rows.filter(row => row.status === status).length]));
const critical = rows.filter(row => ['critical', 'high'].includes(row.risk.severity) && row.status === 'FAIL');
const missing = rows.filter(row => row.status === 'PENDING');
const exceptionLine = item => `- ${item.control}: ${item.file} (expires ${item.expiresAt})`;
const report = `# ASVS Summary\n\n- ASVS version: 5.0.0\n- Target level: L2\n- Application: admisiones\n- Commit: ${commit}\n- Date: ${now.toISOString()}\n\n## Controls\n\n${policy.statuses.map(s => `- ${s}: ${counts[s]}`).join('\n')}\n\n| Control | Scope | Severity | Status | Evidence | Other commit |\n|---|---|---|---|---:|---:|\n${rows.map(r => `| ${r.id} | ${r.scope.join(', ')} | ${r.risk.severity} | ${r.status} | ${r.current.length} | ${r.otherCommit} |`).join('\n')}\n\n## Critical and high failures\n\n${critical.length ? critical.map(r => `- ${r.id}`).join('\n') : 'None.'}\n\n## Missing evidence\n\n${missing.length ? missing.map(r => `- ${r.id}`).join('\n') : 'None.'}\n\n## Expired evidence\n\n${rows.some(r => r.expired) ? rows.filter(r => r.expired).map(r => `- ${r.id}: ${r.expired}`).join('\n') : 'None.'}\n\n## Active exceptions\n\n${activeExceptions.length ? activeExceptions.map(exceptionLine).join('\n') : 'None.'}\n\n## Expired exceptions\n\n${expiredExceptions.length ? expiredExceptions.map(exceptionLine).join('\n') : 'None.'}\n`;
writeFileSync(summaryPath, report);

const findings = rows.filter(row => !['PASS', 'NOT_APPLICABLE'].includes(row.status)).flatMap(row => row.scope.map(component => {
  const fingerprint = stableFingerprint(['admisiones', row.id, component]);
  return {
    id: `ASVS-${fingerprint.slice(0, 12)}`, fingerprint, controlId: row.id, component,
    severity: row.risk.severity, status: row.status, title: row.title, description: row.asvsRequirement,
    environment,
    evidence: row.current.map(item => ({ fingerprint: item.fingerprint, source: item.source, tool: item.tool, rule: item.rule, file: item.file, test: item.test })).sort((a, b) => a.fingerprint.localeCompare(b.fingerprint))
  };
})).sort((a, b) => a.fingerprint.localeCompare(b.fingerprint));
const findingsDocument = {
  schemaVersion: '1.0', application: 'admisiones', repository: 'DesarrolloORT/admisiones-mono', commitSha: commit,
  generatedAt: now.toISOString(),
  summary: { pass: counts.PASS, fail: counts.FAIL, pending: counts.PENDING, needsReview: counts.NEEDS_REVIEW, acceptedRisk: counts.ACCEPTED_RISK },
  findings
};
writeFileSync(findingsPath, `${JSON.stringify(findingsDocument, null, 2)}\n`);
console.log('Evaluated ' + rows.length + ' controls; reports: security/reports/asvs-summary.md, security/reports/findings.json');
if (critical.length) process.exitCode = 2;
