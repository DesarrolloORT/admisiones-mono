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

async function writeGeneratedEnvironment(outputPath, config) {
  const fileContent = `export const generatedEnvironment = ${JSON.stringify(config, null, 2)} as const;\n`;

  await fs.mkdir(path.dirname(outputPath), { recursive: true });
  await fs.writeFile(outputPath, fileContent, 'utf8');
}

function escapeXmlAttribute(value) {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('"', '&quot;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;');
}

function getCspPolicy(config) {
  const policy = config.CSP_POLICY ?? config.cspPolicy;
  return typeof policy === 'string' && policy.trim() ? policy.trim() : null;
}

async function writeWebConfig(outputPath, cspPolicy) {
  const fileContent = `<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <httpProtocol>
      <customHeaders>
        <add name="Cache-Control" value="no-cache" />
        <add name="X-Content-Type-Options" value="nosniff" />
        <add name="X-Frame-Options" value="SAMEORIGIN" />
        <add name="Content-Security-Policy" value="${escapeXmlAttribute(cspPolicy)}" />
        <add name="Referrer-Policy" value="no-referrer" />
        <add name="Permissions-Policy" value="camera=(), geolocation=(), microphone=()" />
        <add name="Strict-Transport-Security" value="max-age=31536000; includeSubDomains" />
      </customHeaders>
    </httpProtocol>
    <staticContent>
      <remove fileExtension=".json" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
      <remove fileExtension=".webmanifest" />
      <mimeMap fileExtension=".webmanifest" mimeType="application/manifest+json" />
    </staticContent>
    <rewrite>
      <rules>
        <rule name="Angular Routes" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
`;

  await fs.mkdir(path.dirname(outputPath), { recursive: true });
  await fs.writeFile(outputPath, fileContent, 'utf8');
}

async function main() {
  const project = getArg('project');
  const env = getArg('env');
  const endpoint = getArg('endpoint', process.env.AZURE_APPCONFIG_ENDPOINT);

  validateRequiredArgs({ project, env, endpoint });

  const key = getArg('key', `frontend:${project}:environment`);
  const label = getArg('label', env);
  const customAzureCliDir = getArg('azure-cli-dir', undefined);
  const outputPath = getArg('output', path.normalize('src/environments/generated-environment.ts'));
  const webConfigPath = getArg('web-config', path.normalize('src/web.config'));

  info(`Project: ${project}`);
  info(`Environment: ${env}`);
  info(`Output: ${outputPath}`);
  info(`Web config: ${webConfigPath}`);

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

  if (!setting.value || setting.value.trim().length === 0) {
    fail(`La key "${key}" con label "${label}" no tiene value.`);
  }

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

  const cspPolicy = getCspPolicy(config);
  if (!cspPolicy) {
    fail('El environment de Azure debe incluir CSP_POLICY o cspPolicy para generar web.config.');
  }

  await writeGeneratedEnvironment(outputPath, config);
  await writeWebConfig(webConfigPath, cspPolicy);

  info(`Generado: ${outputPath}`);
  info(`Generado: ${webConfigPath}`);
}

main().catch(error => {
  fail('Error inesperado ejecutando env-sync.', error);
});
