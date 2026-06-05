import { existsSync, readFileSync } from 'node:fs';
import { dirname, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
export const ENV_DIR = resolve(ROOT, 'src/environments');

export function toProjectPath(path) {
  return relative(ROOT, resolve(ROOT, path)).replaceAll('\\', '/');
}

export function extractApiOrigins(filePath) {
  const raw = readFileSync(filePath, 'utf-8');
  // Strip single-line comments so commented-out URLs are not picked up.
  const content = raw.replace(/^\s*\/\/.*$/gm, '');

  const vars = {};
  for (const match of content.matchAll(/(?:const|let|var)\s+(\w+)\s*=\s*['"`]([^'"`\n]+)['"`]/g)) {
    vars[match[1]] = match[2];
  }

  const urls = new Set();

  for (const match of content.matchAll(/['"`](https?:\/\/[^'"`\s${}]+)['"`]/g)) {
    urls.add(match[1]);
  }

  for (const match of content.matchAll(/`\$\{(\w+)\}([^`]*)`/g)) {
    if (vars[match[1]]) {
      urls.add(vars[match[1]] + match[2]);
    }
  }

  const origins = [...urls].reduce((set, url) => {
    try {
      set.add(new URL(url).origin);
    } catch {
      /* skip invalid URLs */
    }
    return set;
  }, new Set());

  return [...origins];
}

export function resolveSwaggerSource(envFileName, swaggerPath) {
  const envFilePath = resolve(ENV_DIR, envFileName);

  if (!existsSync(envFilePath)) {
    throw new Error(`Environment file not found: src/environments/${envFileName}`);
  }

  const origins = extractApiOrigins(envFilePath);
  if (origins.length === 0) {
    throw new Error('No API URLs found in the environment file.');
  }

  return {
    envFilePath,
    origin: origins[0],
    swaggerUrl: `${origins[0]}${swaggerPath}`,
  };
}
