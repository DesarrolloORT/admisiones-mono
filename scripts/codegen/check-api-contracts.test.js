import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import test from 'node:test';

import {
  checkApiContracts,
  findUnknownResponsesInSource,
  findUnsafeAssertionsInSource,
} from './check-api-contracts.js';

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
    'src/app/features/demo/api/demo.api.ts': `
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
    'src/app/features/demo/api/demo.api.ts': `
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

test('detects unknown and any exposed by public adapter methods', () => {
  const fixture = createFixture({
    'src/app/features/demo/api/demo.api.ts': `
      interface Observable<T> {}
      interface DemoPayload { value: unknown; }
      interface DemoAny { value: any; }
      export class DemoEndpoint {
        public load(): Observable<unknown> { throw new Error('test'); }
        public save(payload: DemoPayload): DemoAny { return { value: payload.value }; }
      }
    `,
  });

  try {
    const violations = checkApiContracts({ root: fixture, tsconfigPath: 'tsconfig.json' });
    assert.equal(violations.length, 3);
    assert.match(violations.map(v => v.message).join('\n'), /load.*unknown.*retorno/);
    assert.match(violations.map(v => v.message).join('\n'), /save.*unknown.*parámetro payload/);
    assert.match(violations.map(v => v.message).join('\n'), /save.*any.*retorno/);
  } finally {
    rmSync(fixture, { recursive: true, force: true });
  }
});

test('detects double assertions through unknown in adapters', () => {
  const source = `
    export class DemoEndpoint {
      public load(): string {
        return ({} as unknown as { value: string }).value;
      }
    }
  `;

  const violations = findUnsafeAssertionsInSource(source);

  assert.equal(violations.length, 1);
  assert.match(violations[0].message, /as unknown as/);
});

test('detects stale request fields hidden behind adapter variables', () => {
  const fixture = createFixture({
    'src/app/features/demo/api/demo.api.ts': `
      interface ApiEndpoint<TRequest> { readonly __types?: { request: TRequest }; }
      declare const api: {
        request<TRequest>(
          endpoint: ApiEndpoint<TRequest>,
          options: { body?: TRequest }
        ): void;
      };
      declare const endpoint: ApiEndpoint<{
        enrollmentId?: number;
        paymentType?: string | null;
      }>;
      export class DemoEndpoint {
        public save(): void {
          const body = { enrolledPersonId: 7, paymentMethod: 'ABITAB' };
          api.request(endpoint, { body });
        }
      }
    `,
  });

  try {
    const violations = checkApiContracts({ root: fixture, tsconfigPath: 'tsconfig.json' });
    assert.equal(violations.length, 1);
    assert.match(violations[0].message, /enrolledPersonId.*request actual/);
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
