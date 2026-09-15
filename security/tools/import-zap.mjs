import { readFileSync, appendFileSync } from 'node:fs';

const [input, environment] = process.argv.slice(2);
if (!input || !['development', 'testing', 'preproduction'].includes(environment)) throw new Error('Usage: node security/tools/import-zap.mjs <zap-report.json> <development|testing|preproduction>');
const report = JSON.parse(readFileSync(input, 'utf8'));
const mapping = { '10010': ['v5.0.0-3.3.4'], '10023': ['v5.0.0-13.4.2'], '10036': ['v5.0.0-16.5.1'] };
const commitSha = process.env.GITHUB_SHA || process.env.BUILD_SOURCEVERSION || 'unknown';
const timestamp = new Date().toISOString();
for (const site of report.site ?? []) for (const alert of site.alerts ?? []) {
  for (const controlId of mapping[String(alert.pluginid)] ?? []) {
    const evidence = { controlId, application: 'admisiones', component: 'system', repository: 'admisiones-mono', commitSha, source: 'zap', tool: 'OWASP ZAP', toolVersion: report['@version'], rule: String(alert.pluginid), environment, timestamp, result: 'FAIL', notes: alert.name };
    appendFileSync(new URL('../evidence/evidence.jsonl', import.meta.url), `${JSON.stringify(evidence)}\n`);
  }
}

