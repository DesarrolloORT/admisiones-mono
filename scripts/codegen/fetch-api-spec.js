import { mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { parseArgs as nodeParseArgs } from 'node:util';

import {
  DEFAULT_ENVIRONMENT_FILE,
  downloadJson,
  normalizeContractDocument,
  normalizeContractsIndex,
  resolveSwaggerSource,
  ROOT,
  toProjectPath,
} from './codegen-utils.js';

const DEFAULTS = {
  swaggerPath: '/swagger/v1/swagger.json',
  contractsPath: '/contracts',
  env: DEFAULT_ENVIRONMENT_FILE,
  output: '.api-spec',
};

const { values: flags } = nodeParseArgs({
  options: {
    origin: { type: 'string' },
    env: { type: 'string', default: DEFAULTS.env },
    'swagger-path': { type: 'string', default: DEFAULTS.swaggerPath },
    'contracts-path': { type: 'string', default: DEFAULTS.contractsPath },
    output: { type: 'string', short: 'o', default: DEFAULTS.output },
    help: { type: 'boolean', short: 'h', default: false },
  },
  strict: false,
});

if (flags.help) {
  console.log(`
Usage: node scripts/codegen/fetch-api-spec.js [options]

Descarga el swagger.json y los contratos de formulario del backend a un
directorio local (snapshot). Ese snapshot alimenta a
"npm run update-api -- --spec-dir <dir>" para generar los contratos sin
acceso de red al backend.

Solo usa modulos nativos de Node: no requiere npm ci.

Options:
  --origin <url>            API origin (ej: https://apiadmisionesdesa.ort.edu.uy).
                            Si se omite, se resuelve desde el environment file.
  --env <file>              Environment file inside src/environments/ (default: ${DEFAULTS.env})
  --swagger-path <path>     Swagger doc path appended to the origin (default: ${DEFAULTS.swaggerPath})
  --contracts-path <path>   Contracts index path appended to the origin (default: ${DEFAULTS.contractsPath})
  -o, --output <dir>        Snapshot output directory (default: ${DEFAULTS.output})
  -h, --help                Show this help
`);
  process.exit(0);
}

const isMain = process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url;

if (isMain) {
  await main();
}

async function main() {
  const outputRel = toProjectPath(flags.output);

  try {
    const origin = flags.origin
      ? new URL(flags.origin).origin
      : resolveSwaggerSource(flags.env, flags['swagger-path']).origin;

    console.log(`  origin    : ${origin}`);
    console.log(`  output    : ${outputRel}/\n`);

    const summary = await fetchApiSpec({
      origin,
      swaggerPath: flags['swagger-path'],
      contractsPath: flags['contracts-path'],
      outputDir: resolve(ROOT, flags.output),
    });

    console.log(
      `✓ Snapshot de API guardado: swagger.json y ${summary.contractCount} contrato(s) en ${outputRel}/.`
    );
  } catch (error) {
    console.error('\n✗ No se pudo descargar el snapshot de la API.');
    console.error(`  Etapa    : ${error?.stage ?? 'resolving API origin'}`);
    console.error(`  Ubicacion: ${outputRel}/`);
    console.error(`  Causa    : ${error?.message || String(error)}`);
    console.error(
      '  Accion   : verifica que el backend, su Swagger y /contracts esten disponibles, y vuelve a ejecutar el comando.'
    );
    process.exit(1);
  }
}

export async function fetchApiSpec({ origin, swaggerPath, contractsPath, outputDir }) {
  let stage = 'downloading Swagger contract';

  try {
    const swagger = await downloadJson(`${origin}${swaggerPath}`);

    stage = 'downloading contracts index';
    const index = normalizeContractsIndex(await downloadJson(`${origin}${contractsPath}`));

    stage = 'downloading contract files';
    const contracts = [];
    for (const item of index) {
      const contractUrl = new URL(item.url, origin).toString();
      contracts.push({
        name: item.name,
        contract: normalizeContractDocument(await downloadJson(contractUrl)),
      });
    }

    stage = 'writing snapshot';
    rmSync(outputDir, { recursive: true, force: true });
    mkdirSync(resolve(outputDir, 'contracts'), { recursive: true });

    writeFileSync(resolve(outputDir, 'swagger.json'), `${JSON.stringify(swagger, null, 2)}\n`);
    writeFileSync(
      resolve(outputDir, 'contracts', 'index.json'),
      `${JSON.stringify(index, null, 2)}\n`
    );
    for (const { name, contract } of contracts) {
      writeFileSync(
        resolve(outputDir, 'contracts', name),
        `${JSON.stringify(contract, null, 2)}\n`
      );
    }

    return { contractCount: contracts.length };
  } catch (error) {
    if (error && typeof error === 'object' && error.stage === undefined) {
      error.stage = stage;
    }
    throw error;
  }
}
