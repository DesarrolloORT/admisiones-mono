import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

import { AppConfigurationClient } from '@azure/app-configuration';
import {
  deserializeAuthenticationRecord,
  DeviceCodeCredential,
  EnvironmentCredential,
  InteractiveBrowserCredential,
  serializeAuthenticationRecord,
  useIdentityPlugin,
} from '@azure/identity';

const DEFAULT_CACHE_PATH = path.normalize('tmp/env/azure-environment-cache.json');
const DEFAULT_DAILY_USAGE_PATH = path.normalize('tmp/env/azure-environment-usage.json');
const DEFAULT_AUTH_RECORD_PATH = path.normalize('tmp/env/azure-auth-record.json');
const APP_CONFIG_SCOPE = 'https://azconfig.io/.default';
const DEFAULT_CACHE_TTL_MINUTES = 60;
const DEFAULT_DAILY_LIMIT = 50;
const DEFAULT_MAX_STALE_MINUTES = 24 * 60;
const AZURE_PAGE_SIZE = 100;

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

const ARGUMENTS_WITH_VALUES = new Set([
  'auth-record-path',
  'cache-path',
  'cache-ttl-minutes',
  'daily-limit',
  'daily-usage-path',
  'endpoint',
  'env',
  'key',
  'label',
  'label-filter',
  'max-stale-minutes',
  'output',
  'project',
  'tenant-id',
  'web-config',
]);

function hasFlag(name) {
  return process.argv.includes(`--${name}`);
}

function getPositionalArg(args = process.argv.slice(2)) {
  for (let index = 0; index < args.length; index += 1) {
    const arg = args[index];

    if (arg.startsWith('--')) {
      if (ARGUMENTS_WITH_VALUES.has(arg.slice(2))) {
        index += 1;
      }

      continue;
    }

    return arg;
  }

  return undefined;
}

function getNonNegativeIntegerArg(name, defaultValue) {
  const rawValue = getArg(name, String(defaultValue));
  const value = Number(rawValue);

  if (!Number.isInteger(value) || value < 0) {
    fail(`--${name} debe ser un entero mayor o igual a 0.`);
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

function getUtcDay(date = new Date()) {
  return date.toISOString().slice(0, 10);
}

function minutesSince(isoDate, now = Date.now()) {
  const time = Date.parse(isoDate);

  return Number.isNaN(time) ? Number.POSITIVE_INFINITY : Math.floor((now - time) / 60000);
}

function isCacheFresh(cache, ttlMinutes, now = Date.now()) {
  return ttlMinutes > 0 && cache?.fetchedAt && minutesSince(cache.fetchedAt, now) <= ttlMinutes;
}

function getCacheLabels(cache) {
  return Object.keys(cache?.settings ?? {}).sort();
}

function getCachedSetting(cache, label) {
  return cache?.settings?.[label] ?? null;
}

async function readJsonFile(filePath) {
  try {
    return JSON.parse(await fs.readFile(filePath, 'utf8'));
  } catch (error) {
    if (error?.code === 'ENOENT') {
      return null;
    }

    fail(`No se pudo leer JSON desde ${filePath}.`, error);
  }
}

async function writeJsonFile(filePath, value) {
  await fs.mkdir(path.dirname(filePath), { recursive: true });
  await fs.writeFile(filePath, `${JSON.stringify(value, null, 2)}\n`, 'utf8');
}

async function clearCache(cachePath, dailyUsagePath, authRecordPath) {
  await fs.rm(cachePath, { force: true });
  await fs.rm(dailyUsagePath, { force: true });
  await fs.rm(authRecordPath, { force: true });
  info(`Cache eliminado: ${cachePath}`);
  info(`Contador eliminado: ${dailyUsagePath}`);
  info(`Sesión eliminada: ${authRecordPath}`);
}

async function readDailyUsage(dailyUsagePath) {
  const today = getUtcDay();
  const usage = await readJsonFile(dailyUsagePath);

  if (!usage || usage.day !== today) {
    return { day: today, azureReads: 0 };
  }

  return {
    day: today,
    azureReads: Number.isInteger(usage.azureReads) ? usage.azureReads : 0,
  };
}

async function assertAndRecordAzureRead(dailyUsagePath, dailyLimit) {
  if (dailyLimit === 0) {
    return;
  }

  const usage = await readDailyUsage(dailyUsagePath);

  if (usage.azureReads >= dailyLimit) {
    fail(
      [
        `Guardrail diario alcanzado: ${usage.azureReads}/${dailyLimit} lecturas Azure para ${usage.day}.`,
        'Usa cache local, --offline, o sube --daily-limit solo si el equipo lo acordó.',
      ].join('\n')
    );
  }

  await writeJsonFile(dailyUsagePath, {
    day: usage.day,
    azureReads: usage.azureReads + 1,
  });
}

async function enableTokenCachePersistence() {
  try {
    const { cachePersistencePlugin } = await import('@azure/identity-cache-persistence');

    useIdentityPlugin(cachePersistencePlugin);

    return true;
  } catch (error) {
    warn(
      `No se pudo habilitar la persistencia del token cache (${error?.message ?? 'error desconocido'}). El login por navegador se pedirá más seguido.`
    );

    return false;
  }
}

async function resolveCredential({ authRecordPath, tenantId, useDeviceCode }) {
  if (process.env.AZURE_CLIENT_ID && process.env.AZURE_TENANT_ID) {
    info('Credenciales de service principal detectadas en variables de entorno (CI).');

    return new EnvironmentCredential();
  }

  const persistenceEnabled = await enableTokenCachePersistence();
  const savedRecord = await readJsonFile(authRecordPath);
  const credentialOptions = {
    tenantId,
    ...(persistenceEnabled && {
      tokenCachePersistenceOptions: { enabled: true, name: 'ort-env-sync' },
    }),
    ...(savedRecord && {
      authenticationRecord: deserializeAuthenticationRecord(JSON.stringify(savedRecord)),
    }),
  };
  const credential = useDeviceCode
    ? new DeviceCodeCredential(credentialOptions)
    : new InteractiveBrowserCredential(credentialOptions);

  if (!savedRecord) {
    info(
      useDeviceCode
        ? 'Primera vez: seguí las instrucciones en consola para iniciar sesión con tu cuenta ORT...'
        : 'Primera vez: se abrirá el navegador para iniciar sesión con tu cuenta ORT...'
    );

    try {
      const record = await credential.authenticate(APP_CONFIG_SCOPE);

      await writeJsonFile(authRecordPath, JSON.parse(serializeAuthenticationRecord(record)));
      info(`Sesión guardada en ${authRecordPath}. Los próximos usos serán silenciosos.`);
    } catch (error) {
      fail(
        [
          'No se pudo completar el login por navegador con Entra ID.',
          '',
          'Revisar:',
          '1. Que hayas completado el login en el navegador que se abrió.',
          '2. Que tu cuenta ORT tenga acceso al tenant correcto (podés forzarlo con --tenant-id o AZURE_TENANT_ID).',
          '3. Que el tenant permita el flujo interactivo (si ves un error AADSTS, reportalo a operaciones).',
        ].join('\n'),
        error
      );
    }
  }

  return credential;
}

async function writeGeneratedEnvironment(outputPath, config, metadata) {
  const fileContent = [
    '// Generated by tools/env/sync-azure-environment.mjs. Do not edit manually.',
    `// Env: ${metadata.env}. Source: ${metadata.source}. Azure fetched at: ${metadata.fetchedAt}.`,
    `export const generatedEnvironment = ${JSON.stringify(config, null, 2)} as const;`,
    '',
  ].join('\n');

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

  if (typeof policy !== 'string' || !policy.trim()) {
    return null;
  }

  return policy.trim();
}

async function writeWebConfig(outputPath, cspPolicy) {
  const fileContent = `<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <httpProtocol>
      <customHeaders>
        <remove name="Cache-Control" />
        <remove name="X-Content-Type-Options" />
        <remove name="X-Frame-Options" />
        <remove name="Content-Security-Policy" />
        <remove name="Referrer-Policy" />
        <remove name="Permissions-Policy" />
        <remove name="Strict-Transport-Security" />
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

function parseSettingConfig(setting) {
  if (!setting.value || setting.value.trim().length === 0) {
    fail(`La key "${setting.key}" con label "${setting.label}" no tiene value.`);
  }

  if (setting.contentType && !setting.contentType.includes('application/json')) {
    warn(
      `El content type recibido para label "${setting.label}" es "${setting.contentType}". Se esperaba "application/json".`
    );
  }

  try {
    const config = JSON.parse(setting.value);

    validateJsonConfig(config);

    return config;
  } catch (error) {
    fail(
      [
        `El value de "${setting.key}" con label "${setting.label}" no es JSON válido.`,
        '',
        'El value en Azure debe ser un objeto JSON completo.',
      ].join('\n'),
      error
    );
  }
}

async function fetchSettingsFromAzure({ client, key, labelFilter }) {
  const pageIterator = client
    .listConfigurationSettings({
      keyFilter: key,
      labelFilter,
    })
    .byPage({ maxPageSize: AZURE_PAGE_SIZE });
  const page = await pageIterator.next();

  if (page.done || !page.value.items.length) {
    fail(`No se encontraron settings para key "${key}" con labelFilter "${labelFilter}".`);
  }

  if (page.value.continuationToken) {
    fail(
      [
        `La consulta devolvió más de ${AZURE_PAGE_SIZE} labels para "${key}".`,
        'Por guardrail no se siguen páginas extra automáticamente.',
        'Usa --label-filter con labels concretos separados por coma.',
      ].join('\n')
    );
  }

  return page.value.items;
}

function toIsoDate(value) {
  return value instanceof Date ? value.toISOString() : value || null;
}

function buildCache({ endpoint, key, settings, fetchedAt }) {
  const cacheSettings = {};

  for (const setting of settings) {
    const label = setting.label ?? '';

    cacheSettings[label] = {
      config: parseSettingConfig(setting),
      contentType: setting.contentType ?? null,
      etag: setting.etag ?? null,
      lastModified: toIsoDate(setting.lastModified),
    };
  }

  return {
    endpoint,
    key,
    fetchedAt,
    settings: cacheSettings,
  };
}

function cacheMatches(cache, { endpoint, key }) {
  return cache?.endpoint === endpoint && cache?.key === key;
}

async function resolveEnvironmentSetting({
  authRecordPath,
  cachePath,
  dailyLimit,
  dailyUsagePath,
  endpoint,
  key,
  label,
  labelFilter,
  refresh,
  offline,
  tenantId,
  ttlMinutes,
  maxStaleMinutes,
  useDeviceCode,
}) {
  const cache = await readJsonFile(cachePath);
  const matchingCache = cacheMatches(cache, { endpoint, key }) ? cache : null;
  const cachedSetting = getCachedSetting(matchingCache, label);

  if (cachedSetting && (offline || (!refresh && isCacheFresh(matchingCache, ttlMinutes)))) {
    return { setting: cachedSetting, source: 'cache', fetchedAt: matchingCache.fetchedAt };
  }

  if (offline) {
    fail(
      `No hay cache local para label "${label}". Ejecuta npm run env:refresh -- --env ${label}.`
    );
  }

  if (cachedSetting && dailyLimit > 0) {
    const usage = await readDailyUsage(dailyUsagePath);
    const cacheAge = minutesSince(matchingCache.fetchedAt);

    if (usage.azureReads >= dailyLimit && cacheAge <= maxStaleMinutes) {
      warn(
        `Guardrail diario alcanzado (${usage.azureReads}/${dailyLimit}); usando cache stale de ${cacheAge} minutos.`
      );

      return { setting: cachedSetting, source: 'stale-cache', fetchedAt: matchingCache.fetchedAt };
    }
  }

  await assertAndRecordAzureRead(dailyUsagePath, dailyLimit);

  const credential = await resolveCredential({ authRecordPath, tenantId, useDeviceCode });
  const client = new AppConfigurationClient(endpoint, credential);
  let settings;

  try {
    settings = await fetchSettingsFromAzure({ client, key, labelFilter });
  } catch (error) {
    fail(
      [
        'No se pudo leer la configuración desde Azure App Configuration.',
        '',
        'Revisar:',
        `1. Que exista la key "${key}" con labelFilter "${labelFilter}".`,
        '2. Que tu cuenta tenga el rol "App Configuration Data Reader" sobre el recurso.',
        '3. Que el endpoint sea correcto.',
        `4. Si el problema es de sesión, ejecutar "npm run env:cache:clear" (borra ${authRecordPath}) y reintentar para volver a iniciar sesión.`,
      ].join('\n'),
      error
    );
  }

  const fetchedAt = new Date().toISOString();
  const nextCache = buildCache({ endpoint, key, settings, fetchedAt });
  const setting = getCachedSetting(nextCache, label);

  if (!setting) {
    fail(
      [
        `No existe label "${label}" para key "${key}".`,
        `Labels disponibles: ${getCacheLabels(nextCache).join(', ') || '(ninguno)'}`,
      ].join('\n')
    );
  }

  await writeJsonFile(cachePath, nextCache);

  return { setting, source: 'azure', fetchedAt };
}

function runSelfTest() {
  const now = Date.parse('2026-07-02T12:00:00.000Z');
  const cache = {
    endpoint: 'https://example.azconfig.io',
    key: 'frontend:admisiones:environment',
    fetchedAt: '2026-07-02T11:30:00.000Z',
    settings: {
      desa: { config: { CSP_POLICY: "object-src 'none'" } },
      prod: { config: { CSP_POLICY: "object-src 'none'" } },
    },
  };

  assert.equal(isCacheFresh(cache, 60, now), true);
  assert.equal(isCacheFresh(cache, 20, now), false);
  assert.deepEqual(getCacheLabels(cache), ['desa', 'prod']);
  assert.equal(cacheMatches(cache, { endpoint: cache.endpoint, key: cache.key }), true);
  assert.equal(cacheMatches(cache, { endpoint: cache.endpoint, key: 'other' }), false);
  assert.equal(
    getPositionalArg([
      '--project',
      'admisiones',
      '--endpoint',
      'https://example',
      '--offline',
      'desa',
    ]),
    'desa'
  );
  assert.equal(getPositionalArg(['--project', 'admisiones', '--env', 'desa']), undefined);
  assert.equal(getCspPolicy({ CSP_POLICY: " object-src 'none'; " }), "object-src 'none';");
  assert.equal(getCspPolicy({ cspPolicy: "base-uri 'self'" }), "base-uri 'self'");
  assert.equal(
    getCspPolicy({ CSP_POLICY: "script-src 'self'", RECAPTCHA_KEY: 'public-site-key' }),
    "script-src 'self'"
  );
  assert.equal(getCspPolicy({ CSP_POLICY: ' ' }), null);
  info('Self-test OK');
}

async function main() {
  if (hasFlag('self-test')) {
    runSelfTest();
    return;
  }

  const cachePath = getArg('cache-path', DEFAULT_CACHE_PATH);
  const dailyUsagePath = getArg('daily-usage-path', DEFAULT_DAILY_USAGE_PATH);
  const authRecordPath = getArg('auth-record-path', DEFAULT_AUTH_RECORD_PATH);

  if (hasFlag('clear-cache')) {
    await clearCache(cachePath, dailyUsagePath, authRecordPath);
    return;
  }

  const project = getArg('project');
  const env = getArg('env', getPositionalArg());
  const endpoint = getArg('endpoint', process.env.AZURE_APPCONFIG_ENDPOINT);

  validateRequiredArgs({ project, env, endpoint });

  const key = getArg('key', `frontend:${project}:environment`);
  const label = getArg('label', env);
  const labelFilter = getArg('label-filter', '*');
  const tenantId = getArg('tenant-id', process.env.AZURE_TENANT_ID ?? 'organizations');
  const useDeviceCode = hasFlag('device-code');
  const refresh = hasFlag('refresh');
  const offline = hasFlag('offline');
  const ttlMinutes = getNonNegativeIntegerArg('cache-ttl-minutes', DEFAULT_CACHE_TTL_MINUTES);
  const dailyLimit = getNonNegativeIntegerArg('daily-limit', DEFAULT_DAILY_LIMIT);
  const maxStaleMinutes = getNonNegativeIntegerArg('max-stale-minutes', DEFAULT_MAX_STALE_MINUTES);
  const outputPath = getArg('output', path.normalize('src/environments/generated-environment.ts'));
  const webConfigPath = getArg('web-config', path.normalize('src/web.config'));

  info(`Project: ${project}`);
  info(`Environment: ${env}`);
  info(`Label: ${label}`);
  info(`Label filter: ${labelFilter}`);
  info(`Cache: ${cachePath}`);
  info(`Cache TTL: ${ttlMinutes} minutos`);
  info(`Output: ${outputPath}`);
  info(`Web config: ${webConfigPath}`);

  const { setting, source, fetchedAt } = await resolveEnvironmentSetting({
    authRecordPath,
    cachePath,
    dailyLimit,
    dailyUsagePath,
    endpoint,
    key,
    label,
    labelFilter,
    refresh,
    offline,
    tenantId,
    ttlMinutes,
    maxStaleMinutes,
    useDeviceCode,
  });
  const config = setting.config;

  const cspPolicy = getCspPolicy(config);
  if (!cspPolicy) {
    fail('El environment de Azure debe incluir CSP_POLICY o cspPolicy para generar web.config.');
  }

  await writeGeneratedEnvironment(outputPath, config, { env, source, fetchedAt });
  await writeWebConfig(webConfigPath, cspPolicy);

  info(`Source: ${source}`);
  info(`Generado: ${outputPath}`);
  info(`Generado: ${webConfigPath}`);
}

main().catch(error => {
  fail('Error inesperado ejecutando env-sync.', error);
});
