import { execSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, rmSync } from 'node:fs';
import { dirname, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseArgs as nodeParseArgs } from 'node:util';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const ENV_DIR = resolve(ROOT, 'src/environments');

const DEFAULTS = {
  swaggerPath: '/swagger/v1/swagger.json',
  env: 'environment.ts',
  output: 'src/app/shared/api-models',
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
// Environment file URL extraction
// ---------------------------------------------------------------------------

function extractApiOrigin(filePath) {
  const content = readFileSync(filePath, 'utf-8');

  // Collect top-level const/let/var string assignments
  const vars = {};
  for (const m of content.matchAll(/(?:const|let|var)\s+(\w+)\s*=\s*['"`]([^'"`\n]+)['"`]/g)) {
    vars[m[1]] = m[2];
  }

  const urls = new Set();

  // Plain string URLs
  for (const m of content.matchAll(/['"`](https?:\/\/[^'"`\s${}]+)['"`]/g)) {
    urls.add(m[1]);
  }

  // Template literals with ${VAR} prefix
  for (const m of content.matchAll(/`\$\{(\w+)\}([^`]*)`/g)) {
    if (vars[m[1]]) urls.add(vars[m[1]] + m[2]);
  }

  // Deduplicate by origin
  const origins = [...urls].reduce((set, u) => {
    try {
      set.add(new URL(u).origin);
    } catch {
      /* skip invalid */
    }
    return set;
  }, new Set());

  return [...origins];
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

const envFilePath = resolve(ENV_DIR, flags.env);
const output = flags.output;

if (!existsSync(envFilePath)) {
  console.error(`✗ Environment file not found: src/environments/${flags.env}`);
  process.exit(1);
}

const origins = extractApiOrigin(envFilePath);
if (origins.length === 0) {
  console.error('✗ No API URLs found in the environment file.');
  process.exit(1);
}

const swaggerUrl = `${origins[0]}${flags['swagger-path']}`;
const outputRel = relative(ROOT, resolve(ROOT, output));

console.log(`  env       : src/environments/${flags.env}`);
console.log(`  origin    : ${origins[0]}`);
console.log(`  swagger   : ${swaggerUrl}`);
console.log(`  output    : ${outputRel}/\n`);

// Clean previous generation to avoid stale models
if (existsSync(resolve(ROOT, output))) {
  rmSync(resolve(ROOT, output), { recursive: true });
}
mkdirSync(resolve(ROOT, output), { recursive: true });

const cmd = [
  'npx --yes @openapitools/openapi-generator-cli generate',
  '--global-property models',
  `-i "${swaggerUrl}"`,
  '-g typescript-angular',
  `-o ${output}`,
  '--additional-properties modelPropertyNaming=original',
].join(' ');

console.log(`> ${cmd}\n`);

try {
  execSync(cmd, {
    cwd: ROOT,
    stdio: 'inherit',
    env: {
      ...process.env,
      // Skip SSL cert validation for self-signed dev certs
      JAVA_OPTS: '-Dio.swagger.v3.parser.util.RemoteUrl.trustAll=true',
    },
  });
  console.log('\n✓ Models updated successfully.');
} catch (error) {
  process.exit(error.status || 1);
}

