import assert from 'node:assert/strict';
import test from 'node:test';

import {
  checkDiff,
  docsNoneReason,
  findMissingDocumentation,
  parseFlowMetadata,
} from './check-knowledge.js';

const login = {
  businessId: 'admisiones.login',
  documentPath: 'docs/flujos/login.md',
  sourcePaths: ['src/app/features/auth/endpoints/auth.endpoint.ts'],
};
const registration = {
  businessId: 'admisiones.registro',
  documentPath: 'docs/flujos/registro.md',
  sourcePaths: ['src/app/features/auth/endpoints/auth.endpoint.ts'],
};

test('parses the canonical flow contract', () => {
  const metadata = parseFlowMetadata(
    `---
businessId: admisiones.login
sourcePaths:
  - src/app/features/auth/
---
# Login
`,
    'docs/flujos/login.md'
  );

  assert.equal(metadata.businessId, 'admisiones.login');
  assert.deepEqual(metadata.sourcePaths, ['src/app/features/auth/']);
});

test('maps shared business code to every affected flow and ignores tests', () => {
  assert.deepEqual(
    findMissingDocumentation(
      [login, registration],
      ['src/app/features/auth/endpoints/auth.endpoint.ts']
    ).map(flow => flow.businessId),
    ['admisiones.login', 'admisiones.registro']
  );
  assert.deepEqual(
    findMissingDocumentation([login], ['src/app/features/auth/endpoints/auth.endpoint.spec.ts']),
    []
  );
});

test('requires documentation or a reasoned docs-none exemption', () => {
  assert.throws(() => checkDiff([login], ['src/app/features/auth/endpoints/auth.endpoint.ts']));
  assert.equal(docsNoneReason('docs-none: rename only'), 'rename only');
  assert.equal(
    checkDiff(
      [login],
      ['src/app/features/auth/endpoints/auth.endpoint.ts'],
      'docs-none: rename only'
    ).exempted,
    true
  );
  assert.equal(docsNoneReason('docs-none: <reason>'), null);
});
