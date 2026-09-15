import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { appendUniqueEvidence, environments } from './security-model.mjs';

const [input, environment, mappingFile = new URL('../asvs/applicability/zap-mapping.json', import.meta.url), output = new URL('../evidence/evidence.jsonl', import.meta.url)] = process.argv.slice(2);
if (!input || !environments.filter(value => value !== 'production').includes(environment)) {
  throw new Error('Usage: node security/tools/import-zap.mjs <zap-report.json> <development|testing|preproduction> [mapping.json] [evidence.jsonl]');
}
let report;
try { report = JSON.parse(readFileSync(input, 'utf8')); } catch { throw new Error('Malformed ZAP JSON report'); }
if (!Array.isArray(report.site)) throw new Error('Invalid ZAP report: site must be an array');
const mapping = JSON.parse(readFileSync(mappingFile, 'utf8'));
const root = resolve(import.meta.dirname, '../..');
const commitSha = process.env.GITHUB_SHA || process.env.BUILD_SOURCEVERSION || execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim();
const timestamp = new Date().toISOString();
const items = [];
for (const site of report.site) {
  if (!Array.isArray(site.alerts)) throw new Error('Invalid ZAP report: alerts must be an array');
  for (const alert of site.alerts) for (const controlId of mapping[String(alert.pluginid)] ?? []) {
    items.push({
      controlId, application: 'admisiones', component: 'system', repository: 'DesarrolloORT/admisiones-mono',
      commitSha, source: 'zap', tool: 'OWASP ZAP', toolVersion: report['@version'], rule: String(alert.pluginid),
      environment, timestamp, result: 'FAIL', scannerSeverity: String(alert.riskdesc ?? '').split(' ')[0] || 'unknown',
      notes: String(alert.name ?? 'ZAP alert')
    });
  }
}
console.log(`Imported ${appendUniqueEvidence(output, items)} unique ZAP findings`);
