import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { appendUniqueEvidence, environments } from './security-model.mjs';

const [input, mappingFile = new URL('../asvs/applicability/sonar-mapping.json', import.meta.url), output = new URL('../evidence/evidence.jsonl', import.meta.url)] = process.argv.slice(2);
if (!input) throw new Error('Usage: node security/tools/import-sonar.mjs <sonar-report.json> [mapping.json] [evidence.jsonl]');
const environment = process.env.SONAR_ENVIRONMENT || 'testing';
if (!environments.includes(environment)) throw new Error(`Invalid SONAR_ENVIRONMENT: ${environment}`);
let report;
try { report = JSON.parse(readFileSync(input, 'utf8')); } catch { throw new Error('Malformed Sonar JSON report'); }
if (!Array.isArray(report.issues)) throw new Error('Invalid Sonar report: issues must be an array');
const mappings = JSON.parse(readFileSync(mappingFile, 'utf8'));
const root = resolve(import.meta.dirname, '../..');
const commitSha = process.env.GITHUB_SHA || process.env.BUILD_SOURCEVERSION || execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim();
const timestamp = new Date().toISOString();
const items = [];
for (const issue of report.issues) for (const controlId of mappings[issue.rule] ?? []) {
  items.push({
    controlId, application: 'admisiones', component: issue.component?.includes('admisiones:') ? 'frontend' : 'backend',
    repository: 'DesarrolloORT/admisiones-mono', commitSha, source: 'sonar', tool: 'SonarQube',
    rule: issue.rule, file: issue.component, line: issue.line, environment, timestamp, result: 'FAIL',
    scannerSeverity: issue.severity ?? 'unknown', notes: `Sonar issue ${issue.severity ?? 'with unknown severity'}`
  });
}
console.log(`Imported ${appendUniqueEvidence(output, items)} unique Sonar findings`);
