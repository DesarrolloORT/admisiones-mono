#!/usr/bin/env node
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join, relative, sep } from 'node:path';
import { pathToFileURL } from 'node:url';

const IGNORED_SEGMENTS = new Set([
  '.angular',
  '.git',
  'coverage',
  'dist',
  'generated',
  'node_modules',
]);

// Separadores decorativos: no aportan informacion y envejecen mal.
const BANNER_RE = /^\s*(?:\/\/|\/\*+|\*)\s*([-=*_#+~])\1{7,}\s*(?:\*\/)?\s*$/;

// Aperturas que narran el codigo en vez de explicar el porque.
const NARRATION = [
  'behind the scenes',
  'detras de escena',
  'this method',
  'this function',
  'this class',
  'this component',
  'este metodo',
  'este método',
  'esta funcion',
  'esta función',
  'esta clase',
  'este componente',
];

// Presupuesto de densidad: dispara solo con volumen real de comentarios.
const MAX_RATIO = 25;
const MIN_COMMENT_LINES = 12;

// Prosa dentro del codigo: 3+ lineas `//` seguidas es un nombre que falta.
const MAX_COMMENT_RUN = 2;

// Deuda previa: cada entrada necesita motivo. Al tocar el archivo, limpialo y sacalo.
const DENSITY_ALLOWLIST = new Map([
  [
    'src/app/features/scholarships/models/available-scholarship.interface.ts',
    'deuda previa -- limpiar al proximo cambio del archivo',
  ],
  [
    'src/app/features/scholarships/models/scholarship-catalog.ts',
    'deuda previa -- limpiar al proximo cambio del archivo',
  ],
  [
    'src/app/features/scholarships/models/scholarship-process.ts',
    'deuda previa -- limpiar al proximo cambio del archivo',
  ],
  [
    'src/app/features/scholarships/facades/scholarship-process.ts',
    'deuda previa -- limpiar al proximo cambio del archivo',
  ],
  [
    'src/app/features/scholarships/api/scholarships.api.ts',
    'deuda previa -- limpiar al proximo cambio del archivo',
  ],
  ['src/app/shared/files/image-upload.ts', 'deuda previa -- limpiar al proximo cambio del archivo'],
]);

// Deuda previa de prosa en el codigo. Al tocar el archivo: extrae nombres, mueve el
// racional de dominio a docs/flujos/ y sacalo de aca.
const COMMENT_RUN_ALLOWLIST = new Set([
  'src/app/features/auth/components/shared/document-fields/document-fields.ts',
  'src/app/features/catalogs/components/location-select/location-select.ts',
  'src/app/features/catalogs/services/academic-proposal-selection.ts',
  'src/app/features/enrollments/api/enrollments.api.ts',
  'src/app/features/enrollments/facades/enrollment-payment.ts',
  'src/app/features/enrollments/facades/enrollment-process.spec.ts',
  'src/app/features/enrollments/facades/enrollment-process.ts',
  'src/app/features/enrollments/facades/enrollment-survey.spec.ts',
  'src/app/features/enrollments/facades/enrollment-survey.ts',
  'src/app/features/enrollments/models/enrollment-detail.ts',
  'src/app/features/enrollments/models/enrollment-entry.spec.ts',
  'src/app/features/enrollments/models/enrollment-entry.ts',
  'src/app/features/enrollments/models/enrollment-flow-forms.ts',
  'src/app/features/enrollments/models/enrollment-flow-mappers.ts',
  'src/app/features/enrollments/models/enrollment-flow-view.ts',
  'src/app/features/enrollments/pages/steps/enrollment-confirmation-step/sections/enrollment-seminars-summary/enrollment-seminars-summary.ts',
  'src/app/features/enrollments/services/external-payment-submitter.ts',
  'src/app/features/scholarships/facades/scholarship-personal.spec.ts',
  'src/app/shared/animations/fade-in-out.spec.ts',
  'src/app/shared/api/core/api-http-client.spec.ts',
  'src/app/shared/api/core/api-http-client.ts',
  'src/app/shared/ui/responsive-select/responsive-select.spec.ts',
  'src/app/shared/ui/responsive-select/responsive-select.ts',
]);

function toPosix(file) {
  return file.split(sep).join('/');
}

function isIgnored(file) {
  return file.split(/[\\/]/).some(part => IGNORED_SEGMENTS.has(part));
}

function listFilesFromGit() {
  const result = spawnSync('git', ['ls-files', '--cached', '--others', '--exclude-standard'], {
    encoding: 'utf-8',
  });

  if (result.status !== 0) return [];
  return result.stdout.split(/\r?\n/).filter(Boolean);
}

function listFilesFromDisk(dir = process.cwd(), files = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name);
    const rel = relative(process.cwd(), path);
    if (isIgnored(rel)) continue;
    if (entry.isDirectory()) listFilesFromDisk(path, files);
    if (entry.isFile()) files.push(rel);
  }
  return files;
}

function sourceFiles() {
  const files = listFilesFromGit();
  const candidates = files.length > 0 ? files : listFilesFromDisk();
  return candidates.filter(
    file =>
      toPosix(file).startsWith('src/') &&
      file.endsWith('.ts') &&
      !isIgnored(file) &&
      existsSync(file)
  );
}

function isComment(line) {
  return /^\s*(?:\/\/|\/\*|\*)/.test(line);
}

function commentPayload(line) {
  return line
    .replace(/^\s*(?:\/\/+|\/\*+|\*)/, '')
    .replace(/\*\/\s*$/, '')
    .trim()
    .toLowerCase();
}

export function validateCommentLine(line) {
  if (BANNER_RE.test(line)) {
    return 'separador decorativo: borralo, la estructura la da el codigo';
  }

  if (!isComment(line)) return null;

  const payload = commentPayload(line);
  const phrase = NARRATION.find(candidate => payload.startsWith(candidate));

  if (phrase) {
    return `"${phrase}" narra el codigo: un comentario explica el porque, no el que`;
  }

  return null;
}

export function findCommentRuns(source) {
  const runs = [];
  let start = 0;
  let length = 0;

  const flush = () => {
    if (length > MAX_COMMENT_RUN) runs.push({ line: start, length });
    length = 0;
  };

  source.split(/\r?\n/).forEach((line, index) => {
    if (!/^\s*\/\//.test(line)) return flush();
    if (length === 0) start = index + 1;
    length++;
  });
  flush();

  return runs;
}

export function measureDensity(source) {
  const lines = source.split(/\r?\n/);
  const total = lines.filter(line => line.trim() !== '').length;
  const comments = lines.filter(isComment).length;

  return { comments, total, ratio: total === 0 ? 0 : Math.round((comments * 100) / total) };
}

function checkFiles(files) {
  const failures = [];

  for (const file of files) {
    const rel = toPosix(file);
    const source = readFileSync(file, 'utf-8');

    source.split(/\r?\n/).forEach((line, index) => {
      const failure = validateCommentLine(line);
      if (failure) failures.push({ file: rel, line: index + 1, failure });
    });

    if (!COMMENT_RUN_ALLOWLIST.has(rel)) {
      for (const { line, length } of findCommentRuns(source)) {
        failures.push({
          file: rel,
          line,
          failure: `${length} lineas de prosa seguidas (maximo ${MAX_COMMENT_RUN}): falta un nombre, extrae o mueve el racional a docs/flujos/`,
        });
      }
    }

    if (DENSITY_ALLOWLIST.has(rel)) continue;

    const { comments, total, ratio } = measureDensity(source);
    if (comments >= MIN_COMMENT_LINES && ratio > MAX_RATIO) {
      failures.push({
        file: rel,
        line: 1,
        failure: `${ratio}% de lineas comentadas (${comments}/${total}), maximo ${MAX_RATIO}%`,
      });
    }
  }

  return failures;
}

function runSelfTest() {
  assert.equal(
    validateCommentLine('// -----------------------------------------------'),
    'separador decorativo: borralo, la estructura la da el codigo'
  );
  assert.equal(
    validateCommentLine(' * Behind the scenes: POST /auth/login using generated endpoint.'),
    '"behind the scenes" narra el codigo: un comentario explica el porque, no el que'
  );
  assert.equal(
    validateCommentLine('/** Sin body a proposito: el token viaja en la cookie. */'),
    null
  );
  assert.equal(validateCommentLine('const behindTheScenes = 1;'), null);
  assert.equal(validateCommentLine('/**'), null);
  assert.deepEqual(measureDensity('// a\n// b\n\nconst x = 1;'), {
    comments: 2,
    total: 3,
    ratio: 67,
  });
  assert.deepEqual(findCommentRuns('// a\n// b\nconst x = 1;'), []);
  assert.deepEqual(findCommentRuns('const x = 1;\n// a\n// b\n// c'), [{ line: 2, length: 3 }]);
  assert.deepEqual(findCommentRuns('// a\n\n// b\n// c'), []);
}

function main() {
  if (process.argv.includes('--self-test')) {
    runSelfTest();
    console.log('check-comment-noise self-test OK');
    return 0;
  }

  const failures = checkFiles(sourceFiles());

  if (failures.length > 0) {
    console.error('Comentarios que no explican el porque:');
    for (const { file, line, failure } of failures) {
      console.error(`- ${file}:${line}: ${failure}`);
    }
    console.error('\nRegla: CLAUDE.md (## Comentarios en codigo)');
    return 1;
  }

  console.log('OK: sin ruido de comentarios.');
  return 0;
}

// Importable desde los tests sin disparar el chequeo.
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  process.exit(main());
}
