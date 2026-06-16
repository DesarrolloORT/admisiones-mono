#!/usr/bin/env node
import path from 'path';
import { spawnSync } from 'child_process';

import { getMissingTests } from './utils.js';

const shouldGenerate = process.argv.includes('--generate');
const missingTests = getMissingTests();

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
    process.exit(1);
  }

  console.log('⚠️  Algunos archivos no tienen un archivo de testing correspondiente.');
  console.log('💡 Ejecutá `npm run generate-tests` para crear los placeholders faltantes.');
  process.exit(1);
}
