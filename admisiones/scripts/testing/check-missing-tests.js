#!/usr/bin/env node
import { spawnSync } from 'child_process';
import path from 'path';

import { getMissingTests, isExcludedFile } from './utils.js';

const shouldGenerate = process.argv.includes('--generate');
const stagedOnly = process.argv.includes('--staged');

function getStagedSourceFiles() {
  const srcAppRoot = path.resolve('src/app') + path.sep;
  const git = spawnSync('git', ['diff', '--cached', '--name-only', '--diff-filter=ACMR'], {
    cwd: process.cwd(),
    encoding: 'utf-8',
  });

  if (git.error) {
    console.error('❗ No se pudo leer el index de Git:', git.error.message);
    process.exit(1);
  }

  if (git.status !== 0) {
    console.error(git.stderr || '❗ No se pudo leer el index de Git.');
    process.exit(git.status ?? 1);
  }

  return git.stdout
    .split(/\r?\n/)
    .filter(Boolean)
    .map(file => path.resolve(file))
    .filter(
      file =>
        file.startsWith(srcAppRoot) &&
        file.endsWith('.ts') &&
        !file.endsWith('.spec.ts') &&
        !isExcludedFile(file)
    );
}

const sourceFiles = stagedOnly ? getStagedSourceFiles() : undefined;
const missingTests = getMissingTests(sourceFiles);

if (missingTests.length === 0) {
  console.log('✅ Todos los archivos tienen sus archivos de testing correspondientes.');
  process.exit(0);
} else {
  console.log('Tests faltantes:');
  missingTests.forEach(({ source, test }) => {
    console.log(
      `🚧 ${path.relative(process.cwd(), source)} → ${path.relative(process.cwd(), test)}`
    );
  });
  console.log('─'.repeat(80));
  if (shouldGenerate) {
    const generation = spawnSync(
      process.execPath,
      ['./scripts/testing/generate-missing-tests.js'],
      {
        cwd: process.cwd(),
        stdio: 'inherit',
      }
    );

    if (generation.status !== 0) {
      console.log('⚠️  Fallo la generación automática de tests.');
      process.exit(generation.status ?? 1);
    }

    console.log('─'.repeat(80));
    console.log('⚠️  Se generaron placeholders de tests. Revisalos y agregalos al commit.');
    console.log('💡 Volvé a correr `git add` sobre los specs creados y reintentá el commit.');
    process.exit(0);
  }

  console.log('⚠️  Algunos archivos no tienen un archivo de testing correspondiente.');
  console.log('💡 Ejecutá `npm run generate-tests` para crear los placeholders faltantes.');
  process.exit(1);
}
