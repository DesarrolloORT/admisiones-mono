#!/usr/bin/env node
import path from 'path';

import { getMissingTests } from './utils.js';

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
  console.log('⚠️  Algunos archivos no tienen un archivo de testing correspondiente.');
  process.exit(1);
}
