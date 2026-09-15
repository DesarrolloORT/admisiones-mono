#!/usr/bin/env node
import fs from 'fs';
import path from 'path';

import { getMissingTests } from './utils.js';

/**
 * Ensures that the directory for the given file exists.
 */
function ensureDirectoryExistence(filePath) {
  const dirname = path.dirname(filePath);
  if (fs.existsSync(dirname)) {
    return true;
  }
  ensureDirectoryExistence(dirname);
  fs.mkdirSync(dirname);
}

/**
 * Reads the content of a source file and extracts its export name.
 * Falls back to using the file's basename if no export is found.
 */
function getExportName(fileContent, filePath) {
  const match = fileContent.match(
    /export\s+(?:abstract\s+)?(?:const|class|enum|function|interface|let|type|var)\s+(\w+)/
  );
  if (match && match[1]) {
    return match[1];
  }
  return path.basename(filePath, '.ts');
}

/**
 * Determines the appropriate test template based on file content.
 */
function getTestTemplate(exportName) {
  return `describe('${exportName}', () => {
  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});`;
}

/**
 * Generates missing test files for each source file that lacks a corresponding test.
 */
function generateMissingTests() {
  const missingTests = getMissingTests();
  missingTests.forEach(({ source, test }) => {
    try {
      const fileContent = fs.readFileSync(source, 'utf-8');
      const exportName = getExportName(fileContent, source);
      const template = getTestTemplate(exportName);
      ensureDirectoryExistence(test);
      fs.writeFileSync(test, template);
      console.log(`🧪 Archivo de testing creado: ${test}`);
    } catch (error) {
      console.error(`❗  Error al generar archivo de testing para ${source}:`, error);
      process.exitCode = 1;
    }
  });
}

// Generate placeholder tests next to their source files.
generateMissingTests();

if (process.exitCode && process.exitCode !== 0) {
  process.exit(1);
} else {
  console.log('─'.repeat(80));
  console.log('✅ Generación de archivos de testing completada');
  process.exit(0);
}
