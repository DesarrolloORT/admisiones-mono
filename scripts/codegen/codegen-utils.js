import { cpSync, existsSync, mkdirSync, readdirSync, readFileSync, rmSync } from 'node:fs';
import http from 'node:http';
import https from 'node:https';
import { dirname, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
export const ENV_DIR = resolve(ROOT, 'src/environments');
export const DEFAULT_ENVIRONMENT_FILE = 'generated-environment.ts';

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

export function replaceGeneratedDirectory(source, target) {
  mkdirSync(target, { recursive: true });

  const sourceEntries = new Set(readdirSync(source));
  for (const entry of readdirSync(target)) {
    if (!sourceEntries.has(entry)) {
      rmSync(resolve(target, entry), { recursive: true, force: true });
    }
  }

  for (const entry of sourceEntries) {
    const targetEntry = resolve(target, entry);
    rmSync(targetEntry, { recursive: true, force: true });
    cpSync(resolve(source, entry), targetEntry, {
      recursive: true,
      force: true,
    });
  }

  rmSync(source, { recursive: true, force: true });
}

export function downloadJson(url, redirectCount = 0) {
  if (redirectCount > 5) {
    return Promise.reject(new Error(`Too many redirects while downloading Swagger: ${url}`));
  }

  return new Promise((resolvePromise, rejectPromise) => {
    const parsedUrl = new URL(url);
    const client = parsedUrl.protocol === 'http:' ? http : https;
    const request = client.get(
      parsedUrl,
      {
        headers: { Accept: 'application/json' },
        rejectUnauthorized: false,
      },
      response => {
        const statusCode = response.statusCode ?? 0;
        const location = response.headers.location;

        if (statusCode >= 300 && statusCode < 400 && location) {
          response.resume();
          downloadJson(new URL(location, url).toString(), redirectCount + 1)
            .then(resolvePromise)
            .catch(rejectPromise);
          return;
        }

        if (statusCode < 200 || statusCode >= 300) {
          response.resume();
          rejectPromise(new Error(`Swagger download failed with HTTP ${statusCode}: ${url}`));
          return;
        }

        response.setEncoding('utf-8');
        let raw = '';
        response.on('data', chunk => {
          raw += chunk;
        });
        response.on('end', () => {
          try {
            resolvePromise(JSON.parse(raw.replace(/^\uFEFF/, '')));
          } catch (error) {
            rejectPromise(new Error(`JSON response is not valid JSON: ${error.message}`));
          }
        });
      }
    );

    request.on('error', rejectPromise);
    request.setTimeout(30000, () => {
      request.destroy(new Error(`Swagger download timed out: ${url}`));
    });
  });
}
