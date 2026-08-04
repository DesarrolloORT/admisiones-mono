import assert from 'node:assert/strict';
import test from 'node:test';

import {
  extractScriptNonce,
  injectNonceIntoHtml,
  resolveBrowserOutputDir,
} from './inject-script-nonce.js';

test('extractScriptNonce reads the nonce from the generated environment', () => {
  const source = `export const generatedEnvironment = {
    "RECAPTCHA_KEY": "site-key",
    "RECAPTCHA_NONCE": "admisiones-recaptcha-2026",
  };`;

  assert.equal(extractScriptNonce(source), 'admisiones-recaptcha-2026');
});

test('extractScriptNonce returns null when the environment has no captcha nonce', () => {
  const source = `export const generatedEnvironment = { "RECAPTCHA_KEY": "" };`;

  assert.equal(extractScriptNonce(source), null);
});

test('injectNonceIntoHtml adds nonce to every script tag missing one', () => {
  const html = '<script src="main.js" type="module"></script><script src="polyfills.js"></script>';

  const patched = injectNonceIntoHtml(html, 'abc123');

  assert.equal(
    patched,
    '<script nonce="abc123" src="main.js" type="module"></script><script nonce="abc123" src="polyfills.js"></script>'
  );
});

test('injectNonceIntoHtml does not duplicate an existing nonce', () => {
  const html = '<script nonce="already-set" src="main.js"></script>';

  assert.equal(injectNonceIntoHtml(html, 'abc123'), html);
});

test('resolveBrowserOutputDir derives the browser folder from angular.json outputPath', () => {
  const angularJson = JSON.stringify({
    projects: {
      admisiones: { architect: { build: { options: { outputPath: 'dist/admisiones' } } } },
    },
  });

  const result = resolveBrowserOutputDir(angularJson, 'C:/repo');

  assert.match(result.replaceAll('\\', '/'), /dist\/admisiones\/browser$/);
});
