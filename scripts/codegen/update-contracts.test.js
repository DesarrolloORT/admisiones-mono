import assert from 'node:assert/strict';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';

import { replaceGeneratedDirectory } from './codegen-utils.js';
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

test('replaces generated directory contents without replacing the directory itself', () => {
  const root = mkdtempSync(join(tmpdir(), 'codegen-'));
  const source = join(root, 'source');
  const target = join(root, 'target');
  mkdirSync(source, { recursive: true });
  mkdirSync(target, { recursive: true });
  writeFileSync(join(source, 'next.txt'), 'next');
  writeFileSync(join(target, 'stale.txt'), 'stale');

  replaceGeneratedDirectory(source, target);

  assert.equal(readFileSync(join(target, 'next.txt'), 'utf-8'), 'next');
  assert.equal(existsSync(join(target, 'stale.txt')), false);
  assert.equal(existsSync(source), false);
});
