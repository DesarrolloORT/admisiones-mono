import { execSync } from 'node:child_process';
import { existsSync, mkdirSync, rmSync } from 'node:fs';
import { resolve } from 'node:path';
import { parseArgs as nodeParseArgs } from 'node:util';

import { resolveSwaggerSource, ROOT, toProjectPath } from './codegen-utils.js';

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
// Main
// ---------------------------------------------------------------------------

const output = flags.output;

let swaggerSource;
try {
  swaggerSource = resolveSwaggerSource(flags.env, flags['swagger-path']);
} catch (error) {
  console.error(`✗ ${error.message}`);
  process.exit(1);
}

const outputRel = toProjectPath(output);

console.log(`  env       : src/environments/${flags.env}`);
console.log(`  origin    : ${swaggerSource.origin}`);
console.log(`  swagger   : ${swaggerSource.swaggerUrl}`);
console.log(`  output    : ${outputRel}/\n`);

// Clean previous generation to avoid stale models
if (existsSync(resolve(ROOT, output))) {
  rmSync(resolve(ROOT, output), { recursive: true });
}
mkdirSync(resolve(ROOT, output), { recursive: true });

const cmd = [
  'npx --yes @openapitools/openapi-generator-cli generate',
  '--global-property models',
  `-i "${swaggerSource.swaggerUrl}"`,
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
