#!/usr/bin/env node
/**
 * Puerta de release: lo que es aceptable exponer en desa no lo es en produccion.
 *
 * La API publica hoy `/swagger/v1/swagger.json` y `/contracts/*.contract.json`
 * sin autenticacion. Los endpoints de datos si piden auth, asi que no hay acceso
 * indebido, pero los contratos documentan la implementacion interna (vistas y
 * tablas Oracle, columnas, endpoints internos y `limitacionesConocidas`). Eso es
 * reconocimiento gratis para un atacante y no lo necesita el frontend.
 *
 * Este check corre solo en builds de `preprod` y `prod`, y falla hasta que:
 *   1. el contrato servido deje de traer identificadores internos, y
 *   2. alguien confirme que `/swagger` y `/contracts` quedaron cerrados en prod
 *      (no se puede verificar offline: el build no tiene red por diseno).
 *
 * Escapes explicitos y trazables, para que el dia del release nadie tenga que
 * saltear el check a mano:
 *   PROD_API_DOCS_CLOSED=1              ya se cerraron los endpoints de doc
 *   ALLOW_PUBLIC_API_CONTRACT_INTERNALS=1   se acepta el riesgo de los internos
 */
import assert from 'node:assert/strict';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

const GATED_ENVIRONMENTS = new Set(['preprod', 'prod']);
const CONTRACTS_DIR = '.api-spec/contracts';

/** Marcas de implementacion interna que no deberian viajar en un contrato publico. */
const INTERNAL_PATTERNS = [
  { label: 'vista Oracle', re: /\bVD_[A-Z][A-Z0-9_]+/g },
  { label: 'tabla Oracle', re: /\bT_[A-Z][A-Z0-9_]+/g },
  { label: 'schema Oracle', re: /\bCREADOR\.[A-Z][A-Z0-9_.]+/g },
  { label: 'endpoint interno', re: /\bORTSecure\/[A-Za-z0-9/]+/g },
  { label: 'sistema interno', re: /\bSGI\b/g },
  { label: 'limitaciones conocidas', re: /"limitacionesConocidas"/g },
];

export function findInternalMarkers(text) {
  return INTERNAL_PATTERNS.flatMap(({ label, re }) => {
    const matches = [...new Set(text.match(re) ?? [])];
    return matches.map(match => ({ label, match }));
  });
}

function contractFiles() {
  if (!existsSync(CONTRACTS_DIR)) return [];
  return readdirSync(CONTRACTS_DIR)
    .filter(file => file.endsWith('.json'))
    .map(file => join(CONTRACTS_DIR, file));
}

function resolveEnvironment() {
  const flagIndex = process.argv.indexOf('--env');
  if (flagIndex >= 0) return process.argv[flagIndex + 1];
  return process.env.APP_ENV ?? '';
}

function runSelfTest() {
  assert.deepEqual(findInternalMarkers('{"desc":"sale de VD_PRUEBAS_DISPONIBLES"}'), [
    { label: 'vista Oracle', match: 'VD_PRUEBAS_DISPONIBLES' },
  ]);
  assert.deepEqual(findInternalMarkers('{"desc":"alta en T_INSCRIPTO_PRUEBA y espejo a SGI"}'), [
    { label: 'tabla Oracle', match: 'T_INSCRIPTO_PRUEBA' },
    { label: 'sistema interno', match: 'SGI' },
  ]);
  assert.deepEqual(findInternalMarkers('{"data":{"primaryPhone":{"type":"string"}}}'), []);
}

if (process.argv.includes('--self-test')) {
  runSelfTest();
  console.log('check-prod-exposure self-test OK');
  process.exit(0);
}

const environment = resolveEnvironment();

if (!GATED_ENVIRONMENTS.has(environment)) {
  console.log(
    `OK: check de exposicion omitido (ambiente "${environment || 'sin declarar'}"; aplica a preprod y prod).`
  );
  process.exit(0);
}

const findings = contractFiles().flatMap(file => {
  const markers = findInternalMarkers(readFileSync(file, 'utf-8'));
  return markers.map(marker => ({ file, ...marker }));
});

const failures = [];

if (findings.length > 0 && process.env.ALLOW_PUBLIC_API_CONTRACT_INTERNALS !== '1') {
  failures.push(
    'Los contratos servidos todavia documentan implementacion interna. Pedir a backend que ' +
      'mueva esos bloques a documentacion interna y refrescar el snapshot con "npm run api-spec:refresh".'
  );
  for (const { file, label, match } of findings) {
    failures.push(`  - ${file}: ${label} "${match}"`);
  }
}

if (process.env.PROD_API_DOCS_CLOSED !== '1') {
  failures.push(
    'Sin confirmar que "/swagger/v1/swagger.json" y "/contracts" quedaron cerrados (deshabilitados, ' +
      'con auth o con allowlist) en el ambiente que se libera. Confirmalo con PROD_API_DOCS_CLOSED=1.'
  );
}

if (failures.length > 0) {
  console.error(`Exposicion pendiente antes de liberar a "${environment}":`);
  for (const failure of failures) console.error(failure);
  console.error('\nDetalle y criterio: docs/RELEASES.md#antes-de-liberar-a-produccion');
  process.exit(1);
}

console.log(`OK: exposicion de documentacion de API revisada para "${environment}".`);
