import assert from 'node:assert/strict';
import test from 'node:test';

import { normalizeContractDocument, normalizeContractsIndex } from './update-contracts.js';

test('normalizes safe contracts index entries', () => {
  assert.deepEqual(
    normalizeContractsIndex([
      { name: 'encuesta-inicial.contract.json', url: '/contracts/encuesta-inicial.contract.json' },
    ]),
    [{ name: 'encuesta-inicial.contract.json', url: '/contracts/encuesta-inicial.contract.json' }]
  );
});

test('rejects unsafe contract index entries', () => {
  assert.throws(
    () => normalizeContractsIndex([{ name: '../x.json', url: '/contracts/../x.json' }]),
    /unsafe name/
  );
  assert.throws(
    () => normalizeContractsIndex([{ name: 'x.json', url: 'https://evil.test/x.json' }]),
    /unsafe url/
  );
});

test('parses contract documents returned as BOM-prefixed JSON strings', () => {
  assert.deepEqual(
    normalizeContractDocument('\uFEFF{"version":2,"fields":{"x":{"type":"string"}}}'),
    {
      version: 2,
      fields: { x: { type: 'string' } },
    }
  );
});
