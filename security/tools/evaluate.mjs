import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const root = resolve(import.meta.dirname, '../..');
const security = resolve(root, 'security');
const controls = JSON.parse(readFileSync(resolve(security, 'asvs/controls/pilot.json'), 'utf8'));
const policy = JSON.parse(readFileSync(resolve(security, 'policies/evaluation.json'), 'utf8'));
const lines = readFileSync(resolve(security, 'evidence/evidence.jsonl'), 'utf8').split(/\r?\n/).filter(Boolean);
const evidence = lines.map((line, index) => {
  try { return JSON.parse(line); } catch { throw new Error(`Invalid JSONL at evidence.jsonl:${index + 1}`); }
});
const ids = new Set(controls.map(control => control.id));
const scopes = new Set(['frontend', 'backend', 'system', 'infrastructure']);
const methods = new Set(['sonar', 'zap', 'automated-test', 'static-rule', 'configuration', 'agent-review', 'human-review']);

for (const control of controls) {
  if (!/^v5\.0\.0-\d+\.\d+\.\d+$/.test(control.id)) throw new Error(`Invalid control id: ${control.id}`);
  if (!control.scope?.length || control.scope.some(scope => !scopes.has(scope))) throw new Error(`Invalid scope: ${control.id}`);
  if (!control.verification?.methods?.length || control.verification.methods.some(method => !methods.has(method))) throw new Error(`Invalid verification method: ${control.id}`);
  if (!policy.statuses.includes(control.status)) throw new Error(`Invalid status: ${control.id}`);
  if (control.status === 'NOT_APPLICABLE' && !control.justification) throw new Error(`NOT_APPLICABLE requires justification: ${control.id}`);
  if (control.status === 'ACCEPTED_RISK') {
    const missing = policy.acceptedRiskRequires.filter(field => !control.exception?.[field]);
    if (missing.length) throw new Error(`ACCEPTED_RISK missing ${missing.join(', ')}: ${control.id}`);
    if (Date.parse(control.exception.expiresAt) <= Date.now()) throw new Error(`Expired ACCEPTED_RISK: ${control.id}`);
  }
}
for (const item of evidence) {
  if (!ids.has(item.controlId)) throw new Error(`Unknown evidence control: ${item.controlId}`);
  for (const field of ['application', 'component', 'repository', 'commitSha', 'source', 'timestamp', 'result']) {
    if (item[field] === undefined || item[field] === '') throw new Error(`Evidence missing ${field}: ${item.controlId}`);
  }
  if (!methods.has(item.source)) throw new Error(`Invalid evidence source: ${item.source}`);
}

const now = new Date();
const maxAge = policy.evidenceMaxAgeDays * 86400000;
const rows = controls.map(control => {
  const linked = evidence.filter(item => item.controlId === control.id);
  const current = linked.filter(item => !item.expiresAt ? now - Date.parse(item.timestamp) <= maxAge : Date.parse(item.expiresAt) > now);
  const trustedPass = current.some(item => item.result === 'PASS' && policy.passTrustSources.includes(item.source));
  const failed = current.some(item => item.result === 'FAIL');
  const reviewPass = current.some(item => item.result === 'PASS');
  const status = control.status === 'NOT_APPLICABLE' || control.status === 'ACCEPTED_RISK'
    ? control.status : failed ? 'FAIL' : trustedPass ? 'PASS' : reviewPass ? 'NEEDS_REVIEW' : 'PENDING';
  return { ...control, status, linked, expired: linked.length - current.length };
});
const counts = Object.fromEntries(policy.statuses.map(status => [status, rows.filter(row => row.status === status).length]));
let commit = process.env.GITHUB_SHA || process.env.BUILD_SOURCEVERSION;
if (!commit) { try { commit = execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(); } catch { commit = 'unknown'; } }
const critical = rows.filter(row => row.risk.severity === 'critical' && row.status === 'FAIL');
const missing = rows.filter(row => row.status === 'PENDING');
const report = `# ASVS Summary\n\n- ASVS version: 5.0.0\n- Target level: L2\n- Application: admisiones\n- Commit: ${commit}\n- Date: ${now.toISOString()}\n\n## Controls\n\n${policy.statuses.map(s => `- ${s}: ${counts[s]}`).join('\n')}\n\n| Control | Scope | Severity | Status | Evidence |\n|---|---|---|---|---:|\n${rows.map(r => `| ${r.id} | ${r.scope.join(', ')} | ${r.risk.severity} | ${r.status} | ${r.linked.length} |`).join('\n')}\n\n## Critical failures\n\n${critical.length ? critical.map(r => `- ${r.id}`).join('\n') : 'None.'}\n\n## Missing evidence\n\n${missing.length ? missing.map(r => `- ${r.id}`).join('\n') : 'None.'}\n\n## Expired evidence\n\n${rows.some(r => r.expired) ? rows.filter(r => r.expired).map(r => `- ${r.id}: ${r.expired}`).join('\n') : 'None.'}\n\n## Expired exceptions\n\nNone accepted by the current pilot catalog.\n`;
writeFileSync(resolve(security, 'reports/asvs-summary.md'), report);
console.log(`Evaluated ${rows.length} controls; report: security/reports/asvs-summary.md`);
if (critical.length) process.exitCode = 2;

