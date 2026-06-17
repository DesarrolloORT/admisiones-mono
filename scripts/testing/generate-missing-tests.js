#!/usr/bin/env node
import fs from 'fs';
import path from 'path';

import { getExpectedTestPath, getMissingTests } from './utils.js';

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
 * Reads the content of a source file and extracts an exported symbol.
 */
function getExportInfo(fileContent) {
  const runtimeMatch = fileContent.match(
    /export\s+(?:abstract\s+)?(?:const|class|enum|function|let|var)\s+(\w+)/
  );
  if (runtimeMatch && runtimeMatch[1]) {
    return { kind: 'runtime', name: runtimeMatch[1] };
  }

  const typeMatch = fileContent.match(/export\s+(?:interface|type)\s+(\w+)/);
  if (typeMatch && typeMatch[1]) {
    return { kind: 'type', name: typeMatch[1] };
  }

  return undefined;
}

/**
 * Determines the appropriate test template based on file content.
 */
function getTestTemplate(sourcePath, fileContent) {
  const exportInfo = getExportInfo(fileContent);
  const describeName = exportInfo?.name ?? path.basename(sourcePath, '.ts');
  const relativeImportPath = path
    .relative(path.dirname(getExpectedTestPath(sourcePath)), path.resolve(sourcePath))
    .replace(/\\/g, '/')
    .replace(/\.ts$/, '');

  if (!exportInfo) {
    return `describe('${describeName}', () => {
  it('should have a test placeholder', () => {
    expect(true).toBe(true);
  });
});`;
  }

  if (exportInfo.kind === 'type') {
    return `import type { ${exportInfo.name} } from '${relativeImportPath}';

describe('${exportInfo.name}', () => {
  it('should be importable as a type', () => {
    const value: ${exportInfo.name} | undefined = undefined;

    expect(value).toBeUndefined();
  });
});`;
  }

  return `import { ${exportInfo.name} } from '${relativeImportPath}';

describe('${exportInfo.name}', () => {
  it('should be importable', () => {
    expect(${exportInfo.name}).toBeDefined();
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
      const template = getTestTemplate(source, fileContent);
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
