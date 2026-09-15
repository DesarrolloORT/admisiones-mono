import { readFileSync, appendFileSync } from 'node:fs';

const [input, mappingFile = new URL('../asvs/applicability/sonar-mapping.json', import.meta.url)] = process.argv.slice(2);
if (!input) throw new Error('Usage: node security/tools/import-sonar.mjs <sonar-report.json> [mapping.json]');
const report = JSON.parse(readFileSync(input, 'utf8'));
const mappings = JSON.parse(readFileSync(mappingFile, 'utf8'));
const commitSha = process.env.GITHUB_SHA || process.env.BUILD_SOURCEVERSION || 'unknown';
const timestamp = new Date().toISOString();
for (const issue of report.issues ?? []) {
  for (const controlId of mappings[issue.rule] ?? []) {
    const evidence = { controlId, application: 'admisiones', component: issue.component?.includes('admisiones:') ? 'frontend' : 'backend', repository: 'admisiones-mono', commitSha, source: 'sonar', tool: 'SonarQube', rule: issue.rule, file: issue.component, line: issue.line, timestamp, result: 'FAIL', notes: issue.message };
    appendFileSync(new URL('../evidence/evidence.jsonl', import.meta.url), `${JSON.stringify(evidence)}\n`);
  }
}

