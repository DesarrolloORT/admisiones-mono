import { mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { parseArgs as nodeParseArgs } from 'node:util';

import {
  DEFAULT_ENVIRONMENT_FILE,
  downloadJson,
  normalizeContractDocument,
  normalizeContractsIndex,
  readJsonFile,
  replaceGeneratedDirectory,
  resolveSwaggerSource,
  ROOT,
  toProjectPath,
} from './codegen-utils.js';

export { normalizeContractDocument, normalizeContractsIndex } from './codegen-utils.js';

const DEFAULTS = {
  contractsPath: '/contracts',
  env: DEFAULT_ENVIRONMENT_FILE,
  output: 'src/app/shared/api/generated/contracts',
};

const { values: flags } = nodeParseArgs({
  options: {
    env: { type: 'string', default: DEFAULTS.env },
    'contracts-path': { type: 'string', default: DEFAULTS.contractsPath },
    'contracts-dir': { type: 'string' },
    output: { type: 'string', short: 'o', default: DEFAULTS.output },
    help: { type: 'boolean', short: 'h', default: false },
  },
  strict: false,
});

if (flags.help) {
  console.log(`
Usage: node scripts/codegen/update-contracts.js [options]

Options:
  --env <file>              Environment file inside src/environments/ (default: ${DEFAULTS.env})
  --contracts-path <path>   Contracts index path appended to the API origin (default: ${DEFAULTS.contractsPath})
  --contracts-dir <dir>     Local snapshot directory with index.json y los contratos
                            (creado por fetch-api-spec.js); evita el acceso de red al backend
  -o, --output <dir>        Output directory for generated contracts (default: ${DEFAULTS.output})
  -h, --help                Show this help
`);
  process.exit(0);
}

const isMain = process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url;

if (isMain) {
  await main();
}

async function main() {
  const output = flags.output;
  const outputAbs = resolve(ROOT, output);
  const outputRel = toProjectPath(output);
  const tempOutputAbs = resolve(ROOT, `${output}.tmp-${process.pid}`);
  const contractsDir = flags['contracts-dir'] ? resolve(ROOT, flags['contracts-dir']) : null;
  let stage = 'resolving contracts source';
  let contractsSource;

  try {
    if (contractsDir) {
      console.log(`  source    : ${toProjectPath(contractsDir)}/ (snapshot local)`);
      console.log(`  output    : ${outputRel}/\n`);
    } else {
      contractsSource = resolveContractsSource(flags.env, flags['contracts-path']);

      console.log(`  env       : src/environments/${flags.env}`);
      console.log(`  origin    : ${contractsSource.origin}`);
      console.log(`  contracts : ${contractsSource.contractsUrl}`);
      console.log(`  output    : ${outputRel}/\n`);
    }

    stage = contractsDir ? 'reading contracts index from snapshot' : 'downloading contracts index';
    const index = normalizeContractsIndex(
      contractsDir
        ? readJsonFile(resolve(contractsDir, 'index.json'))
        : await downloadJson(contractsSource.contractsUrl)
    );

    stage = contractsDir ? 'reading contract files from snapshot' : 'downloading contract files';
    rmSync(tempOutputAbs, { recursive: true, force: true });
    mkdirSync(tempOutputAbs, { recursive: true });

    const files = [];
    for (const item of index) {
      const contract = contractsDir
        ? normalizeContractDocument(readJsonFile(resolve(contractsDir, item.name)))
        : normalizeContractDocument(
            await downloadJson(new URL(item.url, contractsSource.origin).toString())
          );
      writeFileSync(resolve(tempOutputAbs, item.name), `${JSON.stringify(contract, null, 2)}\n`);
      files.push({ name: item.name, url: item.url, exportName: toContractExportName(item.name) });
    }

    writeFileSync(
      resolve(tempOutputAbs, 'contracts.index.json'),
      `${JSON.stringify(index, null, 2)}\n`
    );
    writeFileSync(resolve(tempOutputAbs, 'index.ts'), renderContractsIndex(files));

    stage = 'replacing generated contracts';
    replaceGeneratedDirectory(tempOutputAbs, outputAbs);

    console.log(`\n✓ Contracts updated successfully: ${files.length} file(s).`);
  } catch (error) {
    rmSync(tempOutputAbs, { recursive: true, force: true });
    printFailure({ error, stage, contractsSource, outputRel });
    process.exit(error.status || 1);
  }
}

export function resolveContractsSource(envFileName, contractsPath = DEFAULTS.contractsPath) {
  const swaggerSource = resolveSwaggerSource(envFileName, '/swagger/v1/swagger.json');
  return {
    envFilePath: swaggerSource.envFilePath,
    origin: swaggerSource.origin,
    contractsUrl: `${swaggerSource.origin}${contractsPath}`,
  };
}

function renderContractsIndex(files) {
  const imports = files.map(file => `import ${file.exportName} from './${file.name}';`).join('\n');
  const entries = files
    .map(file => `  ${JSON.stringify(file.name)}: ${file.exportName},`)
    .join('\n');

  return `// -----------------------------------------------------------------------------
// AUTO-GENERATED FILE.
// Do not edit manually.
// Run: npm run update-api
// -----------------------------------------------------------------------------
${imports}

export const generatedContracts = {
${entries}
} as const;

export type GeneratedContractName = keyof typeof generatedContracts;
`;
}

function toContractExportName(name) {
  const words = name
    .replace(/\.json$/, '')
    .split(/[^A-Za-z0-9]+/)
    .filter(Boolean);
  const base = words.map(word => `${word.charAt(0).toUpperCase()}${word.slice(1)}`).join('');
  return `contract${base || 'Document'}`;
}

function printFailure({ error, stage, contractsSource, outputRel }) {
  console.error('\n✗ No se pudieron actualizar los contratos de API.');
  console.error(`  Etapa    : ${stage}`);
  console.error(`  Ubicacion: ${outputRel}/`);
  if (contractsSource) {
    console.error(`  Contracts: ${contractsSource.contractsUrl}`);
  }
  console.error(`  Causa    : ${describeError(error)}`);
}

function describeError(error) {
  if (error?.code === 'ECONNREFUSED') {
    return 'el servidor de contratos rechazo la conexion.';
  }
  if (error?.code === 'ENOTFOUND') {
    return 'no se pudo resolver el host del servidor de contratos.';
  }
  if (error?.code === 'ETIMEDOUT' || error?.message?.includes('timed out')) {
    return 'la descarga de contratos supero el tiempo de espera.';
  }
  return error?.message || String(error);
}
