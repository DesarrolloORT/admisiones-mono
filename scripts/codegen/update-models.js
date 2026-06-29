import { execSync } from 'node:child_process';
import {
  cpSync,
  existsSync,
  mkdirSync,
  readdirSync,
  renameSync,
  rmSync,
  writeFileSync,
} from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { parseArgs as nodeParseArgs } from 'node:util';

import {
  DEFAULT_ENVIRONMENT_FILE,
  downloadJson,
  resolveSwaggerSource,
  ROOT,
  toProjectPath,
} from './codegen-utils.js';

const DEFAULTS = {
  swaggerPath: '/swagger/v1/swagger.json',
  env: DEFAULT_ENVIRONMENT_FILE,
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

const isMain = process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url;

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

if (isMain) {
  await main();
}

async function main() {
  const output = flags.output;
  const outputAbs = resolve(ROOT, output);
  const outputRel = toProjectPath(output);
  const tempOutputAbs = resolve(ROOT, `${output}.tmp-${process.pid}`);
  const backupOutputAbs = resolve(ROOT, `${output}.backup-${process.pid}`);
  const tempSwaggerAbs = resolve(ROOT, `${output}.swagger-${process.pid}.json`);
  const tempSwaggerRel = toProjectPath(tempSwaggerAbs);
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
    const swagger = await downloadJson(swaggerSource.swaggerUrl);
    const { renamedSchemas } = sanitizeOpenApiSchemaNames(swagger);
    mkdirSync(dirname(tempSwaggerAbs), { recursive: true });
    writeFileSync(tempSwaggerAbs, JSON.stringify(swagger), 'utf-8');
    if (renamedSchemas.length > 0) {
      console.log(
        `  normalized: ${renamedSchemas.length} schema name(s) with invalid OpenAPI characters`
      );
    }

    stage = 'generating TypeScript models';
    rmSync(tempOutputAbs, { recursive: true, force: true });
    mkdirSync(tempOutputAbs, { recursive: true });

    const tempOutputRel = toProjectPath(tempOutputAbs);
    const cmd = [
      'npx --yes @openapitools/openapi-generator-cli generate',
      '--global-property models',
      `-i "${tempSwaggerRel}"`,
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
    rmSync(tempSwaggerAbs, { force: true });
    printFailure({ error, stage, swaggerSource, outputRel, hadExistingModels });
    process.exit(error.status || 1);
  } finally {
    rmSync(tempSwaggerAbs, { force: true });
  }
}

export function sanitizeOpenApiSchemaNames(swagger) {
  const schemas = swagger?.components?.schemas;
  if (!schemas || typeof schemas !== 'object') {
    return { renamedSchemas: [] };
  }

  const schemaNamePattern = /^[a-zA-Z0-9._-]+$/;
  const usedNames = new Set(Object.keys(schemas));
  const replacements = new Map();

  for (const name of Object.keys(schemas)) {
    if (schemaNamePattern.test(name)) {
      continue;
    }

    let replacement = name.replace(/[^A-Za-z0-9._-]/g, '') || 'Schema';
    for (let index = 2; usedNames.has(replacement); index++) {
      replacement = `${replacement}${index}`;
    }

    usedNames.delete(name);
    usedNames.add(replacement);
    replacements.set(name, replacement);
  }

  if (replacements.size === 0) {
    return { renamedSchemas: [] };
  }

  for (const [from, to] of replacements) {
    schemas[to] = schemas[from];
    delete schemas[from];
  }

  replaceSchemaRefs(swagger, replacements);

  return {
    renamedSchemas: [...replacements].map(([from, to]) => ({ from, to })),
  };
}

function replaceSchemaRefs(value, replacements) {
  if (Array.isArray(value)) {
    for (const item of value) {
      replaceSchemaRefs(item, replacements);
    }
    return;
  }

  if (!value || typeof value !== 'object') {
    return;
  }

  if (typeof value.$ref === 'string') {
    const prefix = '#/components/schemas/';
    if (value.$ref.startsWith(prefix)) {
      const rawName = value.$ref.slice(prefix.length).replaceAll('~1', '/').replaceAll('~0', '~');
      const decodedName = decodeURIComponent(rawName);
      const replacement = replacements.get(rawName) ?? replacements.get(decodedName);
      if (replacement) {
        value.$ref = `${prefix}${replacement}`;
      }
    }
  }

  for (const item of Object.values(value)) {
    replaceSchemaRefs(item, replacements);
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
    rmSync(backup, { recursive: true, force: true });
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
