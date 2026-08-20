import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createInterface } from 'node:readline/promises';

import azureEnvironmentConfig from './azure-environment.config.js';

const ENVIRONMENTS = ['desa', 'testing', 'preprod', 'local', 'prod'];
const FDP_LABELS = ['desa', 'preprod', 'testing'];
const AZURE_ENV = './node_modules/@desarrolloort/azure-env-sync/bin/azure-env-sync.mjs';
const ANGULAR_CLI = './node_modules/@angular/cli/bin/ng.js';
const [action, selectedEnvironment, ...args] = process.argv.slice(2);
let environment = selectedEnvironment;
let fdpLabel;

if (action === 'self-test') {
  assert.equal(buildConfiguration('desa'), 'development');
  assert.equal(buildConfiguration('prod'), 'production');
  assert.deepEqual(syncArguments(['offline']), ['--offline']);
  assert.deepEqual(localSyncArguments('desa'), [
    '--config',
    './scripts/azure-environment.config.js',
    '--cache-path',
    'tmp/env/azure-environment-cache-local-desa.json',
  ]);
  process.env.FDP_API_LABEL = 'preprod';
  assert.equal(
    azureEnvironmentConfig
      .settings({ project: 'admisiones', env: 'local' })
      .find(setting => setting.name === 'fdpApiUrl').label,
    'preprod'
  );
  console.log('environment self-test OK');
  process.exit(0);
}

if (!['sync', 'start', 'build'].includes(action)) fail('Accion invalida: usa sync, start o build.');

let prompt;
if (!environment) {
  if (!process.stdin.isTTY) fail('Falta el ambiente en una ejecucion no interactiva.');
  prompt = createInterface({ input: process.stdin, output: process.stdout });
  environment = (await prompt.question(`Ambiente (${ENVIRONMENTS.join(', ')}): `)).trim();
}

if (!ENVIRONMENTS.includes(environment)) fail(`Ambiente invalido: ${environment}.`);

if (environment === 'local') {
  if (!process.stdin.isTTY) fail('Falta el label de FDP en una ejecucion no interactiva.');
  prompt ??= createInterface({ input: process.stdin, output: process.stdout });
  fdpLabel = (
    await prompt.question(`Label de frontend:fdp:api_base (${FDP_LABELS.join(', ')}): `)
  ).trim();
  if (!FDP_LABELS.includes(fdpLabel)) fail(`Label de FDP invalido: ${fdpLabel}.`);
}
prompt?.close();

run(
  'ort-azure-env',
  AZURE_ENV,
  [
    'admisiones',
    '--env',
    environment,
    ...(fdpLabel ? localSyncArguments(fdpLabel) : []),
    ...(action === 'sync' ? syncArguments(args) : []),
  ],
  fdpLabel ? { FDP_API_LABEL: fdpLabel } : {}
);

if (action === 'start') run('ng', ANGULAR_CLI, ['serve', ...args]);
if (action === 'build') {
  // Puerta de release: en preprod/prod falla si la documentacion de la API sigue
  // expuesta o si el contrato servido todavia trae implementacion interna.
  run('node ./scripts/quality/check-prod-exposure.js', './scripts/quality/check-prod-exposure.js', [
    '--env',
    environment,
  ]);
  // Genera desde el snapshot versionado en .api-spec/, igual que `npm run update-api`:
  // el ambiente que sincroniza el build no debe cambiar los tipos generados.
  run('node ./scripts/codegen/update-api.js', './scripts/codegen/update-api.js', [
    '--skip-build',
    '--spec-dir',
    '.api-spec',
  ]);
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

function localSyncArguments(label) {
  return [
    '--config',
    './scripts/azure-environment.config.js',
    '--cache-path',
    `tmp/env/azure-environment-cache-local-${label}.json`,
  ];
}

function run(label, script, commandArgs, extraEnvironment = {}) {
  console.log(`\n> ${label} ${commandArgs.join(' ')}`);
  const result = spawnSync(process.execPath, [script, ...commandArgs], {
    stdio: 'inherit',
    env: { ...process.env, ...extraEnvironment },
  });
  if (result.error) fail(result.error.message);
  if (result.status !== 0) process.exit(result.status ?? 1);
}

function fail(message) {
  console.error(message);
  process.exit(1);
}
