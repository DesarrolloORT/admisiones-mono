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

const digestFixture = {
  '@programName': 'ZAP', '@generated': 'Fri, 5 Sep 2025 10:27:42',
  site: [{ '@name': 'http://host:4200', alerts: [
    { pluginid: '10038', alert: 'Content Security Policy (CSP) Header Not Set', riskdesc: 'Medium (High)', count: 4, instances: [{ uri: 'http://host:4200' }] },
    { pluginid: '10021', alert: 'X-Content-Type-Options Header Missing', riskdesc: 'Low (Medium)', count: 7, instances: [{ uri: 'http://host:4200/main.js' }] },
    { pluginid: '90034', alert: 'Cloud Metadata Potentially Exposed', riskdesc: 'High (Low)', count: 1, instances: [{ uri: 'http://host:4200/latest/meta-data/' }] },
    { pluginid: '10109', alert: 'Modern Web Application', riskdesc: 'Informational (Medium)', count: 4, instances: [{ uri: 'http://host:4200' }] },
    { pluginid: '40012', alert: 'Cross Site Scripting (Reflected)', riskdesc: 'High (Medium)', count: 2, instances: [{ uri: 'http://host:4200/buscar' }] }
  ] }]
};

test('ZAP digest groups fixes by action and separates false positives, noise and unknown alerts', () => {
  const dir = mkdtempSync(join(tmpdir(), 'admisiones-digest-'));
  const input = join(dir, 'zap.json');
  writeFileSync(input, JSON.stringify(digestFixture));
  const digest = run(process.execPath, ['security/tools/zap-digest.mjs', input], { cwd: root, env, encoding: 'utf8' });

  const actions = digest.slice(digest.indexOf('## Qué hay que hacer'), digest.indexOf('## Sin clasificar'));
  assert.match(actions, /### 1\. Agregar headers de seguridad de respuesta/);
  assert.match(actions, /\*\*Resuelve 2 alertas \/ 11 instancias:\*\*/);
  assert.equal(actions.match(/^### /gm).length, 1, 'alertas con la misma acción colapsan en un solo paso');

  assert.match(digest, /## Sin clasificar[\s\S]*Cross Site Scripting \(Reflected\)\*\* — High \(2\) · pluginid `40012`/);
  assert.ok(digest.indexOf('## Sin clasificar') < digest.indexOf('## Ruido e informativos'), 'lo desconocido va antes del ruido');
  assert.match(digest, /## Probables falsos positivos[\s\S]*Cloud Metadata Potentially Exposed — High \/ confianza Low/);
  assert.match(digest, /## Ruido e informativos[\s\S]*Modern Web Application/);
  assert.match(digest, /alertas en \*\*4 URLs\*\*/);
  assert.match(digest, /no recorrió la aplicación/);
  assert.match(digest, /Ninguna alerta de este reporte está mapeada/);

  const target = join(dir, 'digest.md');
  run(process.execPath, ['security/tools/zap-digest.mjs', input, '--out', target], { cwd: root, env });
  assert.equal(readFileSync(target, 'utf8'), digest);
});

test('ZAP digest reads the HTML report and reports unmapped ASVS evidence', () => {
  const dir = mkdtempSync(join(tmpdir(), 'admisiones-digest-html-'));
  const input = join(dir, 'zap.html');
  writeFileSync(input, [
    '<html><head><title>FDP</title></head><body>',
    '<p><span>on Fri 5 Sept 2025, at 10:27:42</span></p>',
    '<ul class="sites-list"><li>http://host:4200</li></ul>',
    '<table class="alert-type-counts-table"><tr><th>Alert type</th><th>Risk</th><th>Count</th></tr>',
    '<tr><td>Server Leaks Version Information via &quot;Server&quot; HTTP Response Header Field</td><td><span>Low</span></td><td>7 <span>(100.0%)</span></td></tr>',
    '<tr><td>Modern Web Application</td><td><span>Informational</span></td><td>4 <span>(57.1%)</span></td></tr>',
    '<tr><th>Total</th><th></th><th>2</th></tr></table>',
    '<p class="request-method-n-url">GET http://host:4200</p>',
    '</body></html>'
  ].join(''));
  const digest = run(process.execPath, ['security/tools/zap-digest.mjs', input], { cwd: root, env, encoding: 'utf8' });

  assert.match(digest, /\*\*Escaneo:\*\* FDP · Fri 5 Sept 2025, at 10:27:42/);
  assert.match(digest, /\*\*Objetivo:\*\* http:\/\/host:4200/);
  assert.match(digest, /- `http:\/\/host:4200`/, 'la URL se extrae sin el atributo HTML');
  assert.doesNotMatch(digest, /request-method-n-url/);
  assert.match(digest, /### 1\. Ocultar la versión del servidor/, 'el pluginid se resuelve por nombre');
  assert.match(digest, /registrará evidencia FAIL para:\n- Server Leaks Version Information[\s\S]*v5\.0\.0-16\.5\.1/);
  assert.doesNotMatch(digest, /Total/, 'la fila de totales no se toma como alerta');
});

test('ZAP digest rejects malformed reports and unrecognised HTML', () => {
  const dir = mkdtempSync(join(tmpdir(), 'admisiones-digest-bad-'));
  const json = join(dir, 'zap.json');
  const html = join(dir, 'zap.html');
  writeFileSync(json, '{');
  assert.match(spawnSync(process.execPath, ['security/tools/zap-digest.mjs', json], { cwd: root, env, encoding: 'utf8' }).stderr, /malformado/);
  writeFileSync(json, JSON.stringify({ site: {} }));
  assert.match(spawnSync(process.execPath, ['security/tools/zap-digest.mjs', json], { cwd: root, env, encoding: 'utf8' }).stderr, /"site" debe ser un arreglo/);
  writeFileSync(html, '<html><body>sin tabla</body></html>');
  assert.match(spawnSync(process.execPath, ['security/tools/zap-digest.mjs', html], { cwd: root, env, encoding: 'utf8' }).stderr, /Exporte el reporte en formato JSON/);
  assert.notEqual(spawnSync(process.execPath, ['security/tools/zap-digest.mjs'], { cwd: root, env }).status, 0);
});
