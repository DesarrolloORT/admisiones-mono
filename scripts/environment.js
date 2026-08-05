import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createInterface } from 'node:readline/promises';

const ENVIRONMENTS = ['desa', 'testing', 'preprod', 'local', 'prod'];
const AZURE_ENV = './node_modules/@desarrolloort/azure-env-sync/bin/azure-env-sync.mjs';
const ANGULAR_CLI = './node_modules/@angular/cli/bin/ng.js';
const [action, selectedEnvironment, ...args] = process.argv.slice(2);
let environment = selectedEnvironment;

if (action === 'self-test') {
  assert.equal(buildConfiguration('desa'), 'development');
  assert.equal(buildConfiguration('prod'), 'production');
  assert.deepEqual(syncArguments(['offline']), ['--offline']);
  console.log('environment self-test OK');
  process.exit(0);
}

if (!['sync', 'start', 'build'].includes(action)) fail('Accion invalida: usa sync, start o build.');

if (!environment) {
  if (!process.stdin.isTTY) fail('Falta el ambiente en una ejecucion no interactiva.');
  const prompt = createInterface({ input: process.stdin, output: process.stdout });
  environment = (await prompt.question(`Ambiente (${ENVIRONMENTS.join(', ')}): `)).trim();
  prompt.close();
}

if (!ENVIRONMENTS.includes(environment)) fail(`Ambiente invalido: ${environment}.`);

run('ort-azure-env', AZURE_ENV, [
  'admisiones',
  '--env',
  environment,
  ...(action === 'sync' ? syncArguments(args) : []),
]);

if (action === 'start') run('ng', ANGULAR_CLI, ['serve', ...args]);
if (action === 'build') {
  run('node ./scripts/codegen/update-api.js', './scripts/codegen/update-api.js', ['--skip-build']);
  run('ng', ANGULAR_CLI, ['build', '--configuration', buildConfiguration(environment), ...args]);
  run('node ./scripts/build/inject-script-nonce.js', './scripts/build/inject-script-nonce.js', []);
}

function buildConfiguration(env) {
  return ['preprod', 'prod'].includes(env) ? 'production' : 'development';
}

function syncArguments(options) {
  return options.flatMap(option => {
    if (option.startsWith('--')) return [option];
    const [name, value] = option.split('=', 2);
    return [`--${name}`, ...(value ? [value] : [])];
  });
}

function run(label, script, commandArgs) {
  console.log(`\n> ${label} ${commandArgs.join(' ')}`);
  const result = spawnSync(process.execPath, [script, ...commandArgs], { stdio: 'inherit' });
  if (result.error) fail(result.error.message);
  if (result.status !== 0) process.exit(result.status ?? 1);
}

function fail(message) {
  console.error(message);
  process.exit(1);
}
