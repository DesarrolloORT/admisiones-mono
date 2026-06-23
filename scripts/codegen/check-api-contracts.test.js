import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import test from 'node:test';

import { checkApiContracts, findUnknownResponsesInSource } from './check-api-contracts.js';

test('detects response unknown only at the endpoint response boundary', () => {
  const source = `
    export const valid = defineEndpoint<{ response: { items: Array<unknown> } }>({});
    export const invalid = defineEndpoint<{ response: unknown }>({});
  `;

  const violations = findUnknownResponsesInSource(source);

  assert.equal(violations.length, 1);
  assert.match(violations[0].message, /invalid/);
});

test('detects generated DTOs exposed by public adapter methods', () => {
  const fixture = createFixture({
    'src/app/shared/api/generated/models/backend.ts':
      'export interface BackendDto { value: string; }',
    'src/app/features/demo/endpoints/demo.endpoint.ts': `
      import type { BackendDto } from '../../../shared/api/generated/models/backend';
      export class DemoEndpoint {
        public load(): BackendDto { return { value: 'x' }; }
      }
    `,
  });

  try {
    const violations = checkApiContracts({ root: fixture, tsconfigPath: 'tsconfig.json' });
    assert.equal(violations.length, 1);
    assert.match(violations[0].message, /load.*generated/);
  } finally {
    rmSync(fixture, { recursive: true, force: true });
  }
});

test('accepts feature-owned public contracts', () => {
  const fixture = createFixture({
    'src/app/features/demo/models/demo.ts': 'export interface DemoResult { value: string; }',
    'src/app/features/demo/endpoints/demo.endpoint.ts': `
      import type { DemoResult } from '../models/demo';
      export class DemoEndpoint {
        public load(): DemoResult { return { value: 'x' }; }
      }
    `,
  });

  try {
    const violations = checkApiContracts({ root: fixture, tsconfigPath: 'tsconfig.json' });
    assert.deepEqual(violations, []);
  } finally {
    rmSync(fixture, { recursive: true, force: true });
  }
});

function createFixture(files) {
  const root = mkdtempSync(join(tmpdir(), 'api-contracts-'));
  writeFixtureFile(
    root,
    'tsconfig.json',
    JSON.stringify({
      compilerOptions: {
        module: 'NodeNext',
        moduleResolution: 'NodeNext',
        strict: true,
        target: 'ES2022',
      },
      include: ['src/**/*.ts'],
    })
  );

  for (const [path, content] of Object.entries(files)) {
    writeFixtureFile(root, path, content);
  }

  return root;
}

function writeFixtureFile(root, path, content) {
  const file = join(root, path);
  mkdirSync(dirname(file), { recursive: true });
  writeFileSync(file, content);
}
