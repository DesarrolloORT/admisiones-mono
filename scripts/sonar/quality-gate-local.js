#!/usr/bin/env node
// Replica local del feedback de Sonar sin servidor ni token.
// Corre lint, cobertura (con umbral) y duplicacion, y resume un quality-gate.
import { existsSync, readFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import assert from 'node:assert/strict';

const npm = process.platform === 'win32' ? 'npm.cmd' : 'npm';
const npx = process.platform === 'win32' ? 'npx.cmd' : 'npx';

const LCOV_PATH = 'coverage/admisiones/lcov.info';
const COVERAGE_THRESHOLD = Number(process.env.SONAR_COVERAGE_THRESHOLD ?? 80);
const DUPLICATION_THRESHOLD = Number(process.env.SONAR_DUPLICATION_THRESHOLD ?? 3);

function run(command, args) {
  const result = spawnSync(command, args, { stdio: 'inherit' });
  if (result.error) {
    console.error(`No se pudo ejecutar ${command}: ${result.error.message}`);
    return 1;
  }
  return result.status ?? 1;
}

// Parsea lcov y devuelve el porcentaje de lineas cubiertas (LF/LH agregados).
function parseLcovLineCoverage(content) {
  let found = 0;
  let hit = 0;
  for (const line of content.split(/\r?\n/)) {
    if (line.startsWith('LF:')) found += Number(line.slice(3)) || 0;
    else if (line.startsWith('LH:')) hit += Number(line.slice(3)) || 0;
  }
  if (found === 0) return null;
  return (hit / found) * 100;
}

// Lee el porcentaje de duplicacion desde el reporte json de jscpd.
function parseJscpdPercentage(content) {
  const report = JSON.parse(content);
  const percentage = report?.statistics?.total?.percentage;
  return typeof percentage === 'number' ? percentage : null;
}

function gateLint() {
  const status = run(npm, ['run', 'lint:check']);
  return {
    name: 'Lint (eslint/prettier/stylelint/contracts)',
    ok: status === 0,
    detail: status === 0 ? 'sin issues' : 'ver salida de lint arriba',
  };
}

function gateCoverage() {
  const status = run(npm, ['run', 'test:sonar']);
  if (status !== 0) {
    return { name: 'Cobertura', ok: false, detail: 'los tests fallaron' };
  }
  if (!existsSync(LCOV_PATH)) {
    return { name: 'Cobertura', ok: false, detail: `no se genero ${LCOV_PATH}` };
  }
  const coverage = parseLcovLineCoverage(readFileSync(LCOV_PATH, 'utf-8'));
  if (coverage === null) {
    return { name: 'Cobertura', ok: false, detail: 'lcov sin lineas medibles' };
  }
  const ok = coverage >= COVERAGE_THRESHOLD;
  return {
    name: 'Cobertura',
    ok,
    detail: `${coverage.toFixed(2)}% (umbral ${COVERAGE_THRESHOLD}%)`,
  };
}

function gateDuplication() {
  const reportDir = 'coverage/jscpd';
  const status = run(npx, [
    '--yes',
    'jscpd',
    'src',
    '--reporters',
    'json',
    '--output',
    reportDir,
    '--silent',
    '--gitignore',
  ]);
  const reportPath = `${reportDir}/jscpd-report.json`;
  if (!existsSync(reportPath)) {
    return {
      name: 'Duplicacion',
      ok: status === 0,
      detail: status === 0 ? 'sin reporte, sin duplicados' : 'jscpd fallo',
    };
  }
  const percentage = parseJscpdPercentage(readFileSync(reportPath, 'utf-8'));
  if (percentage === null) {
    return { name: 'Duplicacion', ok: true, detail: 'sin duplicados' };
  }
  const ok = percentage <= DUPLICATION_THRESHOLD;
  return {
    name: 'Duplicacion',
    ok,
    detail: `${percentage.toFixed(2)}% (umbral ${DUPLICATION_THRESHOLD}%)`,
  };
}

function printSummary(results) {
  console.log('\n=== Quality gate local (tipo Sonar) ===');
  for (const { name, ok, detail } of results) {
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}: ${detail}`);
  }
  const failed = results.filter(result => !result.ok);
  if (failed.length > 0) {
    console.log(`\nQuality gate: FAIL (${failed.length} gate(s) en rojo)`);
    return 1;
  }
  console.log('\nQuality gate: PASS');
  return 0;
}

function runSelfTest() {
  assert.equal(parseLcovLineCoverage('LF:10\nLH:8\nLF:10\nLH:10'), 90);
  assert.equal(parseLcovLineCoverage('sin datos'), null);
  assert.equal(
    parseJscpdPercentage('{"statistics":{"total":{"percentage":2.5}}}'),
    2.5
  );
  assert.equal(parseJscpdPercentage('{"statistics":{}}'), null);
  console.log('quality-gate-local self-test OK');
}

if (process.argv.includes('--self-test')) {
  runSelfTest();
  process.exit(0);
}

const results = [gateLint(), gateCoverage(), gateDuplication()];
process.exit(printSummary(results));
