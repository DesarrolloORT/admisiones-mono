#!/usr/bin/env node
import { existsSync } from 'node:fs';
import { spawnSync } from 'node:child_process';

const REQUIRED_ENV = ['SONAR_HOST_URL', 'SONAR_TOKEN', 'SONAR_PROJECT_KEY'];
const npm = process.platform === 'win32' ? 'npm.cmd' : 'npm';

function run(command, args) {
  const result = spawnSync(command, args, { stdio: 'inherit' });
  if (result.error) {
    console.error(`No se pudo ejecutar ${command}: ${result.error.message}`);
    return 1;
  }
  return result.status ?? 1;
}

function commandExists(command, args = ['--version']) {
  const result = spawnSync(command, args, { stdio: 'ignore' });
  return !result.error && result.status === 0;
}

function powershellScanner(path) {
  return {
    command: 'powershell',
    argsPrefix: ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', path],
  };
}

function findScanner() {
  if (process.env.SONAR_SCANNER_PATH) {
    const path = process.env.SONAR_SCANNER_PATH;
    if (path.endsWith('.ps1')) return powershellScanner(path);
    return { command: path, argsPrefix: [] };
  }

  const windowsRunner = 'C:\\sonar-scanner\\run.ps1';
  if (process.platform === 'win32' && existsSync(windowsRunner)) {
    return powershellScanner(windowsRunner);
  }

  if (commandExists('sonar-scanner')) {
    return { command: 'sonar-scanner', argsPrefix: [] };
  }

  return null;
}

function currentBranch() {
  const result = spawnSync('git', ['branch', '--show-current'], { encoding: 'utf-8' });
  if (result.status !== 0) return '';
  return result.stdout.trim();
}

const missingEnv = REQUIRED_ENV.filter(name => !process.env[name]);
const scanner = findScanner();

if (missingEnv.length > 0 || !scanner) {
  const missing = missingEnv.length > 0 ? missingEnv.join(', ') : 'sonar-scanner';
  console.log(`Sonar local omitido: falta ${missing}.`);
  console.log(
    'Configura SONAR_HOST_URL, SONAR_TOKEN, SONAR_PROJECT_KEY y sonar-scanner para ejecutar el scan local.'
  );
  process.exit(0);
}

const coverageStatus = run(npm, ['run', 'test:sonar']);
if (coverageStatus !== 0) process.exit(coverageStatus);

const sonarArgs = [
  ...scanner.argsPrefix,
  `-Dsonar.host.url=${process.env.SONAR_HOST_URL}`,
  `-Dsonar.token=${process.env.SONAR_TOKEN}`,
  `-Dsonar.projectKey=${process.env.SONAR_PROJECT_KEY}`,
  '-Dsonar.qualitygate.wait=true',
  '-Dsonar.qualitygate.timeout=300',
];

if (process.env.SONAR_PULLREQUEST_KEY) {
  sonarArgs.push(
    `-Dsonar.pullrequest.key=${process.env.SONAR_PULLREQUEST_KEY}`,
    `-Dsonar.pullrequest.branch=${process.env.SONAR_PULLREQUEST_BRANCH || currentBranch()}`,
    `-Dsonar.pullrequest.base=${process.env.SONAR_PULLREQUEST_BASE || 'main'}`
  );
} else {
  const branch = currentBranch();
  if (branch) sonarArgs.push(`-Dsonar.branch.name=${branch}`);
}

process.exit(run(scanner.command, sonarArgs));
