import { writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const origin = (process.env.DOCS_SITE_ORIGIN || 'http://localhost').replace(/\/$/, '');
const baseUrl = process.env.DOCS_BASE_URL || '/docs/admisiones/';

if (!baseUrl.startsWith('/') || !baseUrl.endsWith('/')) {
  throw new Error('DOCS_BASE_URL must start and end with "/".');
}

const metadata = {
  system: process.env.DOCS_SYSTEM_SLUG || 'admisiones',
  repository: process.env.GITHUB_REPOSITORY || 'DesarrolloORT/admisiones',
  branch: process.env.GITHUB_REF_NAME || process.env.DOCS_SOURCE_BRANCH || 'local',
  commitSha: process.env.GITHUB_SHA || 'local',
  publishedAt: new Date().toISOString(),
  docsUrl: `${origin}${baseUrl}`,
};

await writeFile(path.join(root, 'static', 'deployment.json'), `${JSON.stringify(metadata, null, 2)}\n`);
