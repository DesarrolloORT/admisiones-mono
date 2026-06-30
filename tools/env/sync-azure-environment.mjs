import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import fs from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

import { AppConfigurationClient } from '@azure/app-configuration';
import { AzureCliCredential } from '@azure/identity';

function getArg(name, defaultValue = undefined) {
  const index = process.argv.indexOf(`--${name}`);

  if (index === -1) {
    return defaultValue;
  }

  const value = process.argv[index + 1];

  if (!value || value.startsWith('--')) {
    return defaultValue;
  }

  return value;
}

function hasFlag(name) {
  return process.argv.includes(`--${name}`);
}

function info(message) {
  console.log(`[env-sync] ${message}`);
}

function warn(message) {
  console.warn(`[env-sync] WARN: ${message}`);
}

function fail(message, error = undefined) {
  console.error('\n[env-sync] ERROR');
  console.error(`[env-sync] ${message}`);

  if (error?.message) {
    console.error(`[env-sync] Detalle: ${error.message}`);
  }

  console.error('');
  process.exit(1);
}

async function readJsonIfExists(filePath) {
  try {
    const content = await fs.readFile(filePath, 'utf8');
    return JSON.parse(content);
  } catch {
    return null;
  }
}

async function fileExists(filePath) {
  try {
    await fs.access(filePath);
    return true;
  } catch {
    return false;
  }
}

function validateRequiredArgs({ project, env, endpoint }) {
  if (!project) {
    fail('Falta --project. Ejemplo: --project admisiones');
  }

  if (!env) {
    fail('Falta --env. Ejemplo: --env desa');
  }

  if (!endpoint) {
    fail(
      [
        'Falta --endpoint o la variable AZURE_APPCONFIG_ENDPOINT.',
        'Ejemplo:',
        'node tools/env/sync-azure-environment.mjs --project admisiones --env desa --endpoint https://appconfigurationdesarrolloia.azconfig.io',
      ].join('\n')
    );
  }
}

function validateJsonConfig(config) {
  if (config === null || config === undefined) {
    fail('El environment recibido desde Azure no puede ser null o undefined.');
  }

  if (typeof config !== 'object') {
    fail('El environment recibido desde Azure debe ser un objeto JSON.');
  }

  if (Array.isArray(config)) {
    fail('El environment recibido desde Azure debe ser un objeto JSON, no un array.');
  }
}

function getUniqueExistingAzureCliDirsOnWindows(customAzureCliDir) {
  const candidateDirs = [
    customAzureCliDir,
    'C:\\Program Files\\Microsoft SDKs\\Azure\\CLI2\\wbin',
    'C:\\Program Files (x86)\\Microsoft SDKs\\Azure\\CLI2\\wbin',
    process.env.ProgramFiles
      ? path.join(process.env.ProgramFiles, 'Microsoft SDKs', 'Azure', 'CLI2', 'wbin')
      : undefined,
    process.env['ProgramFiles(x86)']
      ? path.join(process.env['ProgramFiles(x86)'], 'Microsoft SDKs', 'Azure', 'CLI2', 'wbin')
      : undefined,
  ].filter(Boolean);

  const unique = [];

  for (const dir of candidateDirs) {
    const normalized = path.normalize(dir);
    const alreadyIncluded = unique.some(item => item.toLowerCase() === normalized.toLowerCase());

    if (!alreadyIncluded && existsSync(path.join(normalized, 'az.cmd'))) {
      unique.push(normalized);
    }
  }

  return unique;
}

function ensureAzureCliInPathOnWindows(customAzureCliDir) {
  if (process.platform !== 'win32') {
    return;
  }

  const currentPath = process.env.PATH ?? '';
  const pathEntries = currentPath
    .split(path.delimiter)
    .filter(Boolean)
    .map(entry => path.normalize(entry).toLowerCase());

  const existingAzureCliDirs = getUniqueExistingAzureCliDirsOnWindows(customAzureCliDir);

  for (const dir of existingAzureCliDirs) {
    const normalizedDir = path.normalize(dir);

    if (!pathEntries.includes(normalizedDir.toLowerCase())) {
      process.env.PATH = `${normalizedDir}${path.delimiter}${currentPath}`;
      info(`Azure CLI detectado y agregado al PATH del proceso: ${normalizedDir}`);
      return;
    }
  }
}

function assertAzureCliAvailable() {
  const result = spawnSync('az', ['account', 'show', '--output', 'json'], {
    encoding: 'utf8',
    shell: process.platform === 'win32',
  });

  if (result.status === 0) {
    return;
  }

  const stderr = result.stderr?.trim();
  const stdout = result.stdout?.trim();
  const details = [stderr, stdout].filter(Boolean).join('\n');

  fail(
    [
      'No se pudo ejecutar "az account show" desde este proceso Node.',
      '',
      'Esto suele pasar por una de estas causas:',
      '1. Azure CLI no está instalado.',
      '2. Azure CLI está instalado pero no está en el PATH de esta terminal.',
      '3. No se ejecutó "az login" con el usuario de Windows actual.',
      '4. VS Code/PowerShell/CMD quedaron abiertos antes de instalar Azure CLI y no tomaron el PATH nuevo.',
      '',
      'Acciones recomendadas:',
      '1. Cerrar y abrir nuevamente la terminal o VS Code.',
      '2. Ejecutar: az login',
      '3. Ejecutar: az account show',
      '4. Volver a ejecutar: npm start',
      '',
      'Ruta estándar esperada en Windows:',
      'C:\\Program Files\\Microsoft SDKs\\Azure\\CLI2\\wbin\\az.cmd',
    ].join('\n'),
    details ? new Error(details) : undefined
  );
}

async function writeGeneratedEnvironment(outputPath, config, metadata) {
  const fileContent = `// AUTO-GENERATED — DO NOT EDIT MANUALLY
// Source: Azure App Configuration
// Key: ${metadata.key}
// Label: ${metadata.label}
// ETag: ${metadata.etag}
// LastModified: ${metadata.lastModified ?? 'unknown'}
// GeneratedAtUtc: ${new Date().toISOString()}

export const generatedEnvironment = ${JSON.stringify(config, null, 2)} as const;
`;

  await fs.mkdir(path.dirname(outputPath), { recursive: true });
  await fs.writeFile(outputPath, fileContent, 'utf8');
}

async function writeCache(cachePath, metadata) {
  const cache = {
    project: metadata.project,
    environment: metadata.env,
    key: metadata.key,
    label: metadata.label,
    etag: metadata.etag,
    lastModified: metadata.lastModified,
    syncedAtUtc: new Date().toISOString(),
  };

  await fs.mkdir(path.dirname(cachePath), { recursive: true });
  await fs.writeFile(cachePath, JSON.stringify(cache, null, 2), 'utf8');
}

async function main() {
  const project = getArg('project');
  const env = getArg('env');
  const endpoint = getArg('endpoint', process.env.AZURE_APPCONFIG_ENDPOINT);
  const force = hasFlag('force');

  validateRequiredArgs({ project, env, endpoint });

  const key = getArg('key', `frontend:${project}:environment`);
  const label = getArg('label', env);
  const customAzureCliDir = getArg('azure-cli-dir', undefined);

  const outputPath = getArg('output', path.normalize('src/environments/generated-environment.ts'));

  const cachePath = getArg('cache', path.normalize(`.ort/env-cache/${project}-${env}.json`));

  info(`Project: ${project}`);
  info(`Environment: ${env}`);
  info(`Azure endpoint: ${endpoint}`);
  info(`Azure key: ${key}`);
  info(`Azure label: ${label}`);
  info(`Output: ${outputPath}`);
  info(`Cache: ${cachePath}`);

  ensureAzureCliInPathOnWindows(customAzureCliDir);
  assertAzureCliAvailable();

  const credential = new AzureCliCredential();
  const client = new AppConfigurationClient(endpoint, credential);

  let setting;

  try {
    setting = await client.getConfigurationSetting({
      key,
      label,
    });
  } catch (error) {
    fail(
      [
        'No se pudo leer la configuración desde Azure App Configuration.',
        '',
        'Revisar:',
        `1. Que exista la key "${key}" con label "${label}".`,
        '2. Que el usuario autenticado con "az login" tenga permiso de lectura.',
        '3. Que el endpoint sea correcto.',
        '4. Que "az account show" funcione en esta misma terminal.',
        '',
        'Comandos útiles:',
        'az account show',
        'az login',
      ].join('\n'),
      error
    );
  }

  const remoteEtag = setting.etag;
  const remoteLastModified = setting.lastModified ? setting.lastModified.toISOString() : undefined;

  if (!setting.value || setting.value.trim().length === 0) {
    fail(`La key "${key}" con label "${label}" no tiene value.`);
  }

  const localCache = await readJsonIfExists(cachePath);
  const outputExists = await fileExists(outputPath);

  if (!force && outputExists && localCache?.etag && localCache.etag === remoteEtag) {
    info(`OK. Environment local actualizado. ETag=${remoteEtag}`);

    if (remoteLastModified) {
      info(`Última modificación en Azure: ${remoteLastModified}`);
    }

    return;
  }

  info('Environment nuevo o modificado. Regenerando archivo local...');

  if (setting.contentType && !setting.contentType.includes('application/json')) {
    warn(`El content type recibido es "${setting.contentType}". Se esperaba "application/json".`);
  }

  let config;

  try {
    config = JSON.parse(setting.value);
  } catch (error) {
    fail(
      [
        `El value de "${key}" con label "${label}" no es JSON válido.`,
        '',
        'El value en Azure debe ser un objeto JSON completo.',
        'Ejemplo:',
        '{',
        '  "environment": "desa",',
        '  "project": "admisiones",',
        '  "apiUrl": "https://...",',
        '  "cspPolicy": "default-src ..."',
        '}',
      ].join('\n'),
      error
    );
  }

  validateJsonConfig(config);

  await writeGeneratedEnvironment(outputPath, config, {
    key,
    label,
    etag: remoteEtag,
    lastModified: remoteLastModified,
  });

  await writeCache(cachePath, {
    project,
    env,
    key,
    label,
    etag: remoteEtag,
    lastModified: remoteLastModified,
  });

  info(`Generado: ${outputPath}`);
  info(`Cache actualizada: ${cachePath}`);
  info(`ETag actual: ${remoteEtag}`);

  if (remoteLastModified) {
    info(`Última modificación en Azure: ${remoteLastModified}`);
  }
}

main().catch(error => {
  fail('Error inesperado ejecutando env-sync.', error);
});
