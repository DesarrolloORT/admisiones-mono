import { execSync } from 'node:child_process';
import { cpSync, existsSync, mkdirSync, readdirSync, renameSync, rmSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { parseArgs as nodeParseArgs } from 'node:util';

import { downloadJson, resolveSwaggerSource, ROOT, toProjectPath } from './codegen-utils.js';

const DEFAULTS = {
  swaggerPath: '/swagger/v1/swagger.json',
  env: 'environment.generated.ts',
  output: 'src/app/shared/api/generated/models',
};

// ---------------------------------------------------------------------------
// CLI args
// ---------------------------------------------------------------------------

const { values: flags } = nodeParseArgs({
  options: {
    env: { type: 'string', default: DEFAULTS.env },
    'swagger-path': { type: 'string', default: DEFAULTS.swaggerPath },
    output: { type: 'string', short: 'o', default: DEFAULTS.output },
    help: { type: 'boolean', short: 'h', default: false },
  },
  strict: false,
});

if (flags.help) {
  console.log(`
Usage: node scripts/codegen/update-models.js [options]

Options:
  --env <file>            Environment file inside src/environments/ (default: ${DEFAULTS.env})
  --swagger-path <path>   Swagger doc path appended to the API origin (default: ${DEFAULTS.swaggerPath})
  -o, --output <dir>      Output directory for generated models (default: ${DEFAULTS.output})
  -h, --help              Show this help
`);
  process.exit(0);
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

await main();

async function main() {
  const output = flags.output;
  const outputAbs = resolve(ROOT, output);
  const outputRel = toProjectPath(output);
  const tempOutputAbs = resolve(ROOT, `${output}.tmp-${process.pid}`);
  const backupOutputAbs = resolve(ROOT, `${output}.backup-${process.pid}`);
  const hadExistingModels = hasFiles(outputAbs);
  let stage = 'resolving Swagger source';
  let swaggerSource;

  try {
    swaggerSource = resolveSwaggerSource(flags.env, flags['swagger-path']);

    console.log(`  env       : src/environments/${flags.env}`);
    console.log(`  origin    : ${swaggerSource.origin}`);
    console.log(`  swagger   : ${swaggerSource.swaggerUrl}`);
    console.log(`  output    : ${outputRel}/\n`);

    stage = 'downloading Swagger contract';
    await downloadJson(swaggerSource.swaggerUrl);

    stage = 'generating TypeScript models';
    rmSync(tempOutputAbs, { recursive: true, force: true });
    mkdirSync(tempOutputAbs, { recursive: true });

    const tempOutputRel = toProjectPath(tempOutputAbs);
    const cmd = [
      'npx --yes @openapitools/openapi-generator-cli generate',
      '--global-property models',
      `-i "${swaggerSource.swaggerUrl}"`,
      '-g typescript-angular',
      `-o "${tempOutputRel}"`,
      '--additional-properties modelPropertyNaming=original',
    ].join(' ');

    console.log(`> ${cmd}\n`);

    execSync(cmd, {
      cwd: ROOT,
      stdio: 'inherit',
      env: {
        ...process.env,
        // Skip SSL cert validation for self-signed dev certs
        JAVA_OPTS: '-Dio.swagger.v3.parser.util.RemoteUrl.trustAll=true',
      },
    });

    stage = 'preparing generated models';
    flattenModels(tempOutputAbs);

    stage = 'replacing generated models';
    replaceDirectory(tempOutputAbs, outputAbs, backupOutputAbs);

    console.log('\n✓ Models updated successfully.');
  } catch (error) {
    rmSync(tempOutputAbs, { recursive: true, force: true });
    printFailure({ error, stage, swaggerSource, outputRel, hadExistingModels });
    process.exit(error.status || 1);
  }
}

function flattenModels(outputDir) {
  const modelSubdir = resolve(outputDir, 'model');

  if (!existsSync(modelSubdir)) {
    return;
  }

  for (const file of readdirSync(modelSubdir)) {
    renameSync(join(modelSubdir, file), join(outputDir, file));
  }

  rmSync(modelSubdir, { recursive: true });
  console.log('✓ Flattened model/ into models/.');
}

function replaceDirectory(source, target, backup) {
  const hadTarget = existsSync(target);

  if (hadTarget) {
    renameSync(target, backup);
  }

  try {
    cpSync(source, target, { recursive: true, errorOnExist: true });
    rmSync(source, { recursive: true, force: true });
  } catch (error) {
    rmSync(target, { recursive: true, force: true });
    if (hadTarget && existsSync(backup)) {
      renameSync(backup, target);
    }
    throw error;
  }

  rmSync(backup, { recursive: true, force: true });
}

function hasFiles(directory) {
  return existsSync(directory) && readdirSync(directory).length > 0;
}

function printFailure({ error, stage, swaggerSource, outputRel, hadExistingModels }) {
  console.error('\n✗ No se pudieron actualizar los modelos de API.');
  console.error(`  Etapa    : ${stage}`);
  console.error(`  Ubicacion: ${outputRel}/`);
  if (swaggerSource) {
    console.error(`  Swagger  : ${swaggerSource.swaggerUrl}`);
  }
  console.error(`  Causa    : ${describeError(error)}`);
  console.error(
    hadExistingModels
      ? '  Estado   : se conservaron los modelos generados anteriores.'
      : '  Estado   : no habia modelos anteriores para conservar.'
  );
  console.error(
    '  Accion   : verifica que el backend y su Swagger esten disponibles, y vuelve a ejecutar npm run update-api.'
  );
}

function describeError(error) {
  if (error?.code === 'ECONNREFUSED') {
    return 'el servidor Swagger rechazo la conexion.';
  }
  if (error?.code === 'ENOTFOUND') {
    return 'no se pudo resolver el host del servidor Swagger.';
  }
  if (error?.code === 'ETIMEDOUT' || error?.message?.includes('timed out')) {
    return 'la descarga de Swagger supero el tiempo de espera.';
  }
  if (error?.status) {
    return `OpenAPI Generator termino con codigo ${error.status}.`;
  }
  return error?.message || String(error);
}
