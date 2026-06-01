import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

let loaded = false;

export function loadE2eEnv(filePath = resolve(process.cwd(), '.env.e2e.local')): void {
  if (loaded) {
    return;
  }

  loaded = true;

  if (!existsSync(filePath)) {
    return;
  }

  const contents = readFileSync(filePath, 'utf8');

  for (const rawLine of contents.split(/\r?\n/)) {
    const line = rawLine.trim();

    if (!line || line.startsWith('#')) {
      continue;
    }

    const separatorIndex = line.indexOf('=');

    if (separatorIndex <= 0) {
      continue;
    }

    const key = line.slice(0, separatorIndex).trim();
    const rawValue = line.slice(separatorIndex + 1).trim();
    const value = rawValue.replace(/^(['"])(.*)\1$/, '$2');

    process.env[key] ??= value;
  }
}

export function getE2eEnv(name: string): string | null {
  loadE2eEnv();

  const value = process.env[name]?.trim();

  return value ? value : null;
}

export function getE2eBoolean(name: string, defaultValue = false): boolean {
  const value = getE2eEnv(name);

  if (!value) {
    return defaultValue;
  }

  return ['1', 'true', 'yes', 'y'].includes(value.toLowerCase());
}

export function getE2eNumber(name: string, defaultValue: number): number {
  const rawValue = getE2eEnv(name);

  if (!rawValue) {
    return defaultValue;
  }

  const value = Number(rawValue);

  return Number.isFinite(value) ? value : defaultValue;
}
