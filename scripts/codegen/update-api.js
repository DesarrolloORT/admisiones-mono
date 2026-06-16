import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';
import { parseArgs as nodeParseArgs } from 'node:util';

import { ROOT } from './codegen-utils.js';

const { values: flags } = nodeParseArgs({
  options: {
    env: { type: 'string' },
    'swagger-path': { type: 'string' },
    help: { type: 'boolean', short: 'h', default: false },
  },
});

if (flags.help) {
  console.log(`
Usage: node scripts/codegen/update-api.js [options]

Options:
  --env <file>            Environment file inside src/environments/
  --swagger-path <path>   Swagger doc path appended to the API origin
  -h, --help              Show this help

The command updates models and endpoints, then compiles the Angular serving
configuration to report API incompatibilities before npm start.
`);
  process.exit(0);
}

const sharedArgs = [];
if (flags.env) {
  sharedArgs.push('--env', flags.env);
}
if (flags['swagger-path']) {
  sharedArgs.push('--swagger-path', flags['swagger-path']);
}

runNodeStage({
  label: 'Actualizando modelos OpenAPI',
  script: 'update-models.js',
  args: sharedArgs,
  failure: {
    what: 'No se pudieron regenerar los modelos desde Swagger.',
    where: 'scripts/codegen/update-models.js y src/app/shared/api/generated/models/',
  },
});

runNodeStage({
  label: 'Actualizando endpoints OpenAPI',
  script: 'update-endpoints.js',
  args: sharedArgs,
  failure: {
    what: 'No se pudieron regenerar los endpoints desde Swagger.',
    where: 'scripts/codegen/update-endpoints.js y src/app/shared/api/generated/endpoints/',
  },
});

validateAngularCompilation();

console.log('\n✓ API actualizada y compatibilidad Angular validada.');

function runNodeStage({ label, script, args, failure }) {
  console.log(`\n==> ${label}`);
  const scriptPath = resolve(ROOT, 'scripts/codegen', script);
  const result = spawnSync(process.execPath, [scriptPath, ...args], {
    cwd: ROOT,
    stdio: 'inherit',
  });

  if (result.error || result.status !== 0) {
    printStageFailure({
      stage: label,
      what: failure.what,
      where: failure.where,
      detail: result.error?.message || `el proceso termino con codigo ${result.status ?? 1}.`,
    });
    process.exit(result.status || 1);
  }
}

function validateAngularCompilation() {
  const label = 'Validando compilacion equivalente a npm start';
  console.log(`\n==> ${label}`);

  const angularCli = resolve(ROOT, 'node_modules/@angular/cli/bin/ng.js');
  const result = spawnSync(
    process.execPath,
    [angularCli, 'build', '--configuration', 'serving', '--no-progress'],
    {
      cwd: ROOT,
      stdio: 'inherit',
    }
  );

  if (result.error || result.status !== 0) {
    printStageFailure({
      stage: label,
      what: 'La API se regenero, pero la aplicacion ya no compila con el contrato nuevo.',
      where:
        'los errores TS/NG inmediatamente anteriores indican cada archivo, linea y simbolo afectado.',
      detail:
        result.error?.message ||
        `Angular termino con codigo ${result.status ?? 1}; npm start fallaria por la misma causa.`,
    });
    process.exit(result.status || 1);
  }
}

function printStageFailure({ stage, what, where, detail }) {
  console.error('\n✗ update-api no pudo completarse.');
  console.error(`  Etapa    : ${stage}`);
  console.error(`  Que paso : ${what}`);
  console.error(`  Donde    : ${where}`);
  console.error(`  Detalle  : ${detail}`);
}
