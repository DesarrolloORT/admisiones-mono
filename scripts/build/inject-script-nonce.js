import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

import { DEFAULT_ENVIRONMENT_FILE, ENV_DIR, ROOT } from '../codegen/codegen-utils.js';

const isMain = process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url;

export function extractScriptNonce(generatedEnvironmentSource) {
  const match = generatedEnvironmentSource.match(/"RECAPTCHA_NONCE":\s*"([^"]+)"/);
  return match ? match[1] : null;
}

export function injectNonceIntoHtml(html, nonce) {
  return html.replace(/<script(?![^>]*\bnonce=)/g, `<script nonce="${nonce}"`);
}

export function resolveBrowserOutputDir(angularJsonSource, rootDir) {
  const angularJson = JSON.parse(angularJsonSource);
  const project = Object.values(angularJson.projects)[0];
  const outputPath = project.architect.build.options.outputPath;
  return resolve(rootDir, outputPath, 'browser');
}

function run() {
  const environmentPath = resolve(ENV_DIR, DEFAULT_ENVIRONMENT_FILE);
  const nonce = extractScriptNonce(readFileSync(environmentPath, 'utf-8'));

  if (!nonce) {
    console.log(
      '==> RECAPTCHA_NONCE no definido en el ambiente; se omite la inyeccion de nonce en index.html.'
    );
    return;
  }

  const angularJsonPath = resolve(ROOT, 'angular.json');
  const browserOutputDir = resolveBrowserOutputDir(readFileSync(angularJsonPath, 'utf-8'), ROOT);
  const indexPath = resolve(browserOutputDir, 'index.html');

  if (!existsSync(indexPath)) {
    console.error(`✗ No se encontro ${indexPath}. Este script debe correr despues de ng build.`);
    process.exit(1);
  }

  const html = readFileSync(indexPath, 'utf-8');
  const patched = injectNonceIntoHtml(html, nonce);
  writeFileSync(indexPath, patched);

  console.log(`==> nonce="${nonce}" agregado a los <script> de ${indexPath}`);
}

if (isMain) {
  run();
}
