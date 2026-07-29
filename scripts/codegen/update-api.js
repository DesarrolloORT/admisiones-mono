import { spawnSync } from 'node:child_process';
import { join, resolve } from 'node:path';
import { parseArgs as nodeParseArgs } from 'node:util';

import { hasGeneratedApiChanges, snapshotGeneratedApi } from './api-repair.js';
import { DEFAULT_ENVIRONMENT_FILE, ROOT } from './codegen-utils.js';

const { values: flags } = nodeParseArgs({
  options: {
    env: { type: 'string', default: DEFAULT_ENVIRONMENT_FILE },
    'swagger-path': { type: 'string' },
    'contracts-path': { type: 'string' },
    'spec-dir': { type: 'string' },
    'skip-build': { type: 'boolean', default: false },
    help: { type: 'boolean', short: 'h', default: false },
  },
});

if (flags.help) {
  console.log(`
Usage: node scripts/codegen/update-api.js [options]

Options:
  --env <file>            Environment file inside src/environments/ (default: ${DEFAULT_ENVIRONMENT_FILE})
  --swagger-path <path>     Swagger doc path appended to the API origin
  --contracts-path <path>   Contracts index path appended to the API origin
  --spec-dir <dir>          Snapshot local creado por fetch-api-spec.js; genera todo
                            desde archivos sin acceso de red al backend
  --skip-build              Omite la compilacion Angular de validacion (util en CI,
                            donde un job posterior ya compila el proyecto)
  -h, --help              Show this help

The command updates models, endpoints and form contracts, then compiles the Angular serving
configuration to report API incompatibilities before npm start.
`);
  process.exit(0);
}

const sharedArgs = ['--env', flags.env];
if (flags['spec-dir']) {
  sharedArgs.push('--swagger-file', join(flags['spec-dir'], 'swagger.json'));
} else if (flags['swagger-path']) {
  sharedArgs.push('--swagger-path', flags['swagger-path']);
}

const contractsArgs = ['--env', flags.env];
if (flags['spec-dir']) {
  contractsArgs.push('--contracts-dir', join(flags['spec-dir'], 'contracts'));
} else if (flags['contracts-path']) {
  contractsArgs.push('--contracts-path', flags['contracts-path']);
}

snapshotGeneratedApi();

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

runNodeStage({
  label: 'Actualizando contratos de formulario',
  script: 'update-contracts.js',
  args: contractsArgs,
  failure: {
    what: 'No se pudieron regenerar los contratos de formulario desde /contracts.',
    where: 'scripts/codegen/update-contracts.js y src/app/shared/api/generated/contracts/',
  },
});

runNodeStage({
  label: 'Validando contratos públicos de adapters',
  script: 'check-api-contracts.js',
  args: [],
  repairable: true,
  failure: {
    what: 'Los adapters exponen DTOs generados, envían bodies fuera del contrato o existen responses sin schema tipado.',
    where: 'features/*/endpoints y src/app/shared/api/generated/endpoints/',
  },
});

if (flags['skip-build']) {
  console.log('\n==> Compilacion Angular de validacion omitida (--skip-build).');
  console.log('\n✓ API actualizada; la compatibilidad Angular se valida en el build posterior.');
} else {
  validateAngularCompilation();

  console.log('\n✓ API actualizada y compatibilidad Angular validada.');
}

if (hasGeneratedApiChanges()) {
  printRepairHint();
}

function runNodeStage({ label, script, args, failure, repairable = false }) {
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
    if (repairable) printRepairHint();
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
    printRepairHint();
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

function printRepairHint() {
  console.log('\nPara analizar el delta y reparar todos los consumidores afectados:');
  console.log('  npm run fix-api');
}
