import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';

import {
  getAgentArgs,
  hasGeneratedApiChanges,
  selectAgent,
  snapshotGeneratedApi,
} from './api-repair.js';

test('preserva el contrato previo y detecta cambios', () => {
  const root = mkdtempSync(join(tmpdir(), 'api-repair-'));
  const sourceDir = join(root, 'generated');
  const snapshotDir = join(root, 'previous');

  try {
    mkdirSync(sourceDir);
    writeFileSync(join(sourceDir, 'model.ts'), 'export type Model = string;');
    snapshotGeneratedApi({ sourceDir, snapshotDir });
    assert.equal(hasGeneratedApiChanges({ snapshotDir, currentDir: sourceDir }), false);

    writeFileSync(join(sourceDir, 'model.ts'), 'export type Model = number;');
    assert.equal(hasGeneratedApiChanges({ snapshotDir, currentDir: sourceDir }), true);
  } finally {
    rmSync(root, { recursive: true, force: true });
  }
});

test('usa Claude por defecto y permite Codex', () => {
  assert.equal(selectAgent(undefined), 'claude');
  assert.equal(selectAgent('codex'), 'codex');
  assert.deepEqual(getAgentArgs('codex').slice(0, 4), [
    'exec',
    '--sandbox',
    'workspace-write',
    '-C',
  ]);
  assert.throws(() => selectAgent('otro'), /claude.*codex/i);
});
