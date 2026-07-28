#!/usr/bin/env node
import { spawnSync } from 'node:child_process';
import { cpSync, existsSync, mkdirSync, rmSync } from 'node:fs';
import { resolve } from 'node:path';
import { stdin, stdout } from 'node:process';
import { createInterface } from 'node:readline/promises';
import { pathToFileURL } from 'node:url';
import { parseArgs as nodeParseArgs } from 'node:util';

import { ROOT } from './codegen-utils.js';

export const GENERATED_API_DIR = resolve(ROOT, 'src/app/shared/api/generated');
export const API_REPAIR_SNAPSHOT_DIR = resolve(ROOT, 'tmp/update-api/previous');

const AGENTS = {
  claude: {
    prompt:
      '/repair-api-update Repara quirurgicamente todos los consumidores afectados por la ultima actualizacion de API.',
    manual: 'Abri Claude Code en este repo y ejecuta: /repair-api-update',
  },
  codex: {
    prompt:
      'Use $repair-api-update to surgically repair every consumer affected by the latest API update.',
    manual: 'Abri Codex en este repo y ejecuta: $repair-api-update',
  },
};

export function snapshotGeneratedApi({
  sourceDir = GENERATED_API_DIR,
  snapshotDir = API_REPAIR_SNAPSHOT_DIR,
} = {}) {
  rmSync(snapshotDir, { recursive: true, force: true });
  mkdirSync(resolve(snapshotDir, '..'), { recursive: true });
  if (existsSync(sourceDir)) cpSync(sourceDir, snapshotDir, { recursive: true });
  else mkdirSync(snapshotDir, { recursive: true });
}

export function hasGeneratedApiChanges({
  snapshotDir = API_REPAIR_SNAPSHOT_DIR,
  currentDir = GENERATED_API_DIR,
} = {}) {
  if (!existsSync(currentDir)) return true;
  const result = spawnSync(
    'git',
    ['diff', '--no-index', '--quiet', '--', snapshotDir, currentDir],
    { cwd: ROOT, stdio: 'ignore' }
  );
  if (result.error) throw result.error;
  if (result.status > 1) throw new Error('No se pudo comparar el snapshot de API.');
  return result.status === 1;
}

export function selectAgent(requestedAgent, answer = '') {
  const agent = (requestedAgent ?? answer).trim().toLowerCase() || 'claude';
  if (agent in AGENTS) return agent;
  throw new Error('El agente debe ser "claude" o "codex".');
}

async function main() {
  const { values } = nodeParseArgs({
    options: {
      agent: { type: 'string' },
      help: { type: 'boolean', short: 'h', default: false },
    },
  });

  if (values.help) {
    console.log('Usage: npm run fix-api -- [--agent claude|codex]');
    return;
  }
  if (!existsSync(API_REPAIR_SNAPSHOT_DIR)) {
    console.error('No existe tmp/update-api/previous/. Ejecuta npm run update-api primero.');
    process.exitCode = 1;
    return;
  }

  let answer = '';
  if (!values.agent && stdin.isTTY && stdout.isTTY) {
    const input = createInterface({ input: stdin, output: stdout });
    try {
      answer = await input.question('Agente [Claude/codex] (Claude): ');
    } finally {
      input.close();
    }
  }

  try {
    const agent = selectAgent(values.agent, answer);
    if (!commandExists(agent)) {
      console.error(`No se encontro ${agent} en PATH.\n${AGENTS[agent].manual}`);
      process.exitCode = 1;
      return;
    }
    const args = agent === 'codex' ? ['-C', ROOT, AGENTS[agent].prompt] : [AGENTS[agent].prompt];
    const result = spawnSync(agent, args, {
      cwd: ROOT,
      stdio: 'inherit',
      shell: process.platform === 'win32',
    });
    if (result.error) throw result.error;
    process.exitCode = result.status ?? 1;
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}

function commandExists(command) {
  const locator = process.platform === 'win32' ? 'where.exe' : 'which';
  return spawnSync(locator, [command], { stdio: 'ignore' }).status === 0;
}

const isMain = process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url;

if (isMain) await main();
