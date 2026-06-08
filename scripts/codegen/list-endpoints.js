import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { parseArgs as nodeParseArgs } from 'node:util';

import { ROOT, toProjectPath } from './codegen-utils.js';

const DEFAULTS = {
  endpoints: 'src/app/shared/api/generated/endpoints',
  adapters: 'src/app/features',
};

const { values: flags } = nodeParseArgs({
  options: {
    endpoints: { type: 'string', default: DEFAULTS.endpoints },
    adapters: { type: 'string', default: DEFAULTS.adapters },
    json: { type: 'boolean', default: false },
    help: { type: 'boolean', short: 'h', default: false },
  },
  strict: false,
});

if (flags.help) {
  console.log(`
Usage: node scripts/codegen/list-endpoints.js [options]

Options:
  --endpoints <dir>   Generated endpoints directory (default: ${DEFAULTS.endpoints})
  --adapters <dir>    Feature directory containing endpoint adapters (default: ${DEFAULTS.adapters})
  --json              Print machine-readable JSON
  -h, --help          Show this help
`);
  process.exit(0);
}

const endpointsDir = resolve(ROOT, flags.endpoints);
const adaptersDir = resolve(ROOT, flags.adapters);

if (!existsSync(endpointsDir)) {
  console.error(
    `✗ Generated endpoints not found at ${toProjectPath(flags.endpoints)}. Run: npm run update-api`
  );
  process.exit(1);
}

const endpoints = readGeneratedEndpoints(endpointsDir);
const adapterReferences = readAdapterReferences(
  adaptersDir,
  endpoints.map(endpoint => endpoint.constant)
);
const rows = endpoints.map(endpoint => ({
  ...endpoint,
  adapter: adapterReferences.get(endpoint.constant)?.join(', ') ?? '-',
}));

if (flags.json) {
  console.log(JSON.stringify(rows, null, 2));
} else {
  printTable(rows);
}

function readGeneratedEndpoints(dir) {
  return readdirSync(dir)
    .filter(file => file.endsWith('.endpoints.ts'))
    .sort()
    .flatMap(file => {
      const filePath = resolve(dir, file);
      const content = readFileSync(filePath, 'utf-8');
      const endpoints = [];
      const endpointRegex =
        /export const (\w+) = defineEndpoint<[\s\S]*?\>\(\{[\s\S]*?method:\s*['"]([^'"]+)['"][\s\S]*?path:\s*(['"])((?:\\.|(?!\3).)*)\3/g;

      for (const match of content.matchAll(endpointRegex)) {
        endpoints.push({
          method: match[2],
          path: unescapeStringLiteral(match[4]),
          constant: match[1],
          file: toProjectPath(filePath),
        });
      }

      return endpoints;
    })
    .sort((a, b) => a.path.localeCompare(b.path) || a.method.localeCompare(b.method));
}

function readAdapterReferences(dir, endpointNames) {
  const references = new Map();
  const names = new Set(endpointNames);

  for (const filePath of findAdapterFiles(dir)) {
    const content = readFileSync(filePath, 'utf-8');
    const foundNames = endpointNames.filter(name => content.includes(name));

    for (const name of foundNames) {
      if (!names.has(name)) {
        continue;
      }

      const current = references.get(name) ?? [];
      current.push(toProjectPath(filePath));
      references.set(name, current);
    }
  }

  return references;
}

function findAdapterFiles(dir) {
  if (!existsSync(dir)) {
    return [];
  }

  const results = [];

  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const fullPath = resolve(dir, entry.name);

    if (entry.isDirectory()) {
      results.push(...findAdapterFiles(fullPath));
      continue;
    }

    if (entry.isFile() && entry.name.endsWith('.endpoint.ts')) {
      results.push(fullPath);
    }
  }

  return results.sort();
}

function printTable(rows) {
  if (rows.length === 0) {
    console.log('No generated endpoints found.');
    return;
  }

  const headers = ['METHOD', 'PATH', 'CONSTANT', 'FILE', 'ADAPTER'];
  const values = rows.map(row => [row.method, row.path, row.constant, row.file, row.adapter]);
  const widths = headers.map((header, index) =>
    Math.max(header.length, ...values.map(row => row[index].length))
  );
  const formatRow = row => row.map((value, index) => value.padEnd(widths[index])).join('  ');

  console.log(formatRow(headers));
  console.log(formatRow(widths.map(width => '-'.repeat(width))));

  for (const row of values) {
    console.log(formatRow(row));
  }
}

function unescapeStringLiteral(value) {
  return value.replace(/\\'/g, "'").replace(/\\"/g, '"').replace(/\\\\/g, '\\');
}
