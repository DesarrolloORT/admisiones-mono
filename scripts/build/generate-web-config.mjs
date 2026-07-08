#!/usr/bin/env node

import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const ROOT_DIR = process.cwd();

const GENERATED_ENVIRONMENT_PATH = resolve(ROOT_DIR, 'src/environments/environment.generated.ts');
const WEB_CONFIG_TEMPLATE_PATH = resolve(ROOT_DIR, 'src/web.config.template');
const WEB_CONFIG_OUTPUT_PATH = resolve(ROOT_DIR, 'src/web.config');

function extractValue(content, key) {
  const patterns = [
    new RegExp(`["']${key}["']\\s*:\\s*"([\\s\\S]*?)"\\s*,?`),
    new RegExp(`${key}\\s*:\\s*"([\\s\\S]*?)"\\s*,?`),
    new RegExp(`["']${key}["']\\s*:\\s*'([\\s\\S]*?)'\\s*,?`),
    new RegExp(`${key}\\s*:\\s*'([\\s\\S]*?)'\\s*,?`),
  ];

  for (const pattern of patterns) {
    const match = content.match(pattern);

    if (match?.[1]) {
      return match[1];
    }
  }

  return undefined;
}

function readCspPolicy() {
  if (process.env.CSP_POLICY?.trim()) {
    return process.env.CSP_POLICY.trim();
  }

  if (!existsSync(GENERATED_ENVIRONMENT_PATH)) {
    throw new Error(
      `No existe ${GENERATED_ENVIRONMENT_PATH}. Primero ejecutá env:sync para traer la configuración desde Azure.`
    );
  }

  const content = readFileSync(GENERATED_ENVIRONMENT_PATH, 'utf8');

  const cspPolicy = extractValue(content, 'CSP_POLICY') ?? extractValue(content, 'cspPolicy');

  if (!cspPolicy?.trim()) {
    throw new Error(
      `No se encontró CSP_POLICY/cspPolicy en ${GENERATED_ENVIRONMENT_PATH}. Revisá la clave frontend:admisiones:environment en Azure App Configuration.`
    );
  }

  return cspPolicy.replaceAll('\\"', '"').replaceAll('\\n', ' ').replaceAll('\\r', ' ').trim();
}

function escapeXmlAttribute(value) {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('"', '&quot;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;');
}

const cspPolicy = readCspPolicy();

if (!cspPolicy.includes('default-src')) {
  throw new Error('CSP_POLICY inválida: falta default-src.');
}

if (!cspPolicy.includes('script-src')) {
  throw new Error('CSP_POLICY inválida: falta script-src.');
}

if (!existsSync(WEB_CONFIG_TEMPLATE_PATH)) {
  throw new Error(`No existe ${WEB_CONFIG_TEMPLATE_PATH}.`);
}

const template = readFileSync(WEB_CONFIG_TEMPLATE_PATH, 'utf8');

if (!template.includes('__CSP_POLICY__')) {
  throw new Error('El template web.config no contiene el placeholder __CSP_POLICY__.');
}

const webConfig = template.replace('__CSP_POLICY__', escapeXmlAttribute(cspPolicy));

writeFileSync(WEB_CONFIG_OUTPUT_PATH, webConfig, 'utf8');

console.log('web.config generado correctamente desde CSP_POLICY de Azure App Configuration.');

