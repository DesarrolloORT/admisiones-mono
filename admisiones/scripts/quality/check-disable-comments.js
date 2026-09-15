#!/usr/bin/env node
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join, relative, sep } from 'node:path';

const TARGET_EXTENSIONS = new Set([
  '.css',
  '.html',
  '.js',
  '.jsx',
  '.mjs',
  '.cjs',
  '.scss',
  '.ts',
  '.tsx',
]);
const IGNORED_SEGMENTS = new Set(['.angular', '.git', 'coverage', 'dist', 'node_modules']);
const DISABLE_RE = /\b(?<tool>eslint|stylelint)-disable(?:-next-line|-line)?\b/;

function hasTargetExtension(file) {
  return [...TARGET_EXTENSIONS].some(extension => file.endsWith(extension));
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
    file => hasTargetExtension(file) && !isIgnored(file) && existsSync(file)
  );
}

function validateDisable(line) {
  const match = DISABLE_RE.exec(line);
  if (!match) return null;

  const directive = match[0];
  const rest = line.slice(match.index + directive.length);
  const reasonMatch = rest.match(/\s--\s+(.+)$/);
  const ruleText = reasonMatch ? rest.slice(0, reasonMatch.index).trim() : '';
  const reason = reasonMatch ? reasonMatch[1].replace(/(?:\*\/|-->)\s*$/, '').trim() : '';
  const rules = ruleText.split(/[\s,]+/).filter(Boolean);

  if (rules.length === 0 || reason.length === 0) {
    return `${directive} requiere regla concreta y motivo: ${directive} rule/name -- motivo`;
  }

  return null;
}

function checkFiles(files) {
  const failures = [];

  for (const file of files) {
    const lines = readFileSync(file, 'utf-8').split(/\r?\n/);
    lines.forEach((line, index) => {
      const failure = validateDisable(line);
      if (failure) failures.push({ file, line: index + 1, failure });
    });
  }

  return failures;
}

function runSelfTest() {
  const eslintDisable = 'eslint-' + 'disable';
  const stylelintDisable = 'stylelint-' + 'disable';

  assert.equal(
    validateDisable(`<!-- ${eslintDisable} @angular-eslint/template/cyclomatic-complexity -->`),
    `${eslintDisable} requiere regla concreta y motivo: ${eslintDisable} rule/name -- motivo`
  );
  assert.equal(
    validateDisable(
      `/* ${stylelintDisable} declaration-property-unit-allowed-list -- Uses token fallback. */`
    ),
    null
  );
  assert.equal(
    validateDisable(
      `// ${stylelintDisable}-next-line declaration-property-unit-allowed-list -- Hairline divider.`
    ),
    null
  );
}

if (process.argv.includes('--self-test')) {
  runSelfTest();
  console.log('check-disable-comments self-test OK');
  process.exit(0);
}

const failures = checkFiles(sourceFiles());

if (failures.length > 0) {
  console.error('Disable comments sin regla o motivo:');
  for (const { file, line, failure } of failures) {
    console.error(`- ${file.split(sep).join('/')}:${line}: ${failure}`);
  }
  process.exit(1);
}

console.log('OK: todos los disable comments tienen regla y motivo.');
