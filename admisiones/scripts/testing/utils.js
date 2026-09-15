import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Load configuration from the root-level file
const configPath = path.resolve(__dirname, '../../test-generator.config.json');
let config = { excludeFilePatterns: [], excludePaths: [] };
if (fs.existsSync(configPath)) {
  try {
    config = JSON.parse(fs.readFileSync(configPath, 'utf-8'));
  } catch (error) {
    console.error('Error parsing configuration file:', error);
  }
}

/**
 * Checks if a file should be excluded based on the configuration.
 * It checks if the file’s relative path contains any excluded path segment
 * or if the file name matches any of the exclude file patterns.
 */
function isExcludedFile(filePath) {
  const relativePath = path.relative(path.resolve('src/app'), filePath).replace(/\\/g, '/');
  // Exclude if the file is in any of the defined excluded paths.
  if (config.excludePaths.some(pattern => relativePath.includes(pattern))) {
    return true;
  }
  // For each pattern like "*.interface.ts", check if the file name ends with the configured ending.
  const fileName = path.basename(filePath);
  return config.excludeFilePatterns.some(pattern => {
    const trimmed = pattern.replace('*', '');
    return fileName.endsWith(trimmed);
  });
}

/**
 * Recursively retrieves all .ts files (excluding .spec.ts) from a directory.
 */
function getAllTsFiles(dir, fileList = []) {
  const files = fs.readdirSync(dir);
  files.forEach(file => {
    const filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);
    if (stat.isDirectory()) {
      getAllTsFiles(filePath, fileList);
    } else if (stat.isFile() && filePath.endsWith('.ts') && !filePath.endsWith('.spec.ts')) {
      if (!isExcludedFile(filePath)) {
        fileList.push(filePath);
      }
    }
  });
  return fileList;
}

/**
 * Recursively retrieves all .spec.ts files from src/app.
 */
function getAllSpecFilesInSrcApp(dir, fileList = []) {
  const files = fs.readdirSync(dir);
  files.forEach(file => {
    const filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);
    if (stat.isDirectory()) {
      getAllSpecFilesInSrcApp(filePath, fileList);
    } else if (stat.isFile() && filePath.endsWith('.spec.ts')) {
      fileList.push(filePath);
    }
  });
  return fileList;
}

/**
 * Computes the preferred co-located test file path for a given .ts file.
 */
function getExpectedTestPath(tsFilePath) {
  return tsFilePath.replace(/\.ts$/, '.spec.ts');
}

/**
 * Computes the legacy centralized test file path for a given .ts file.
 * Kept temporarily to avoid breaking repositories still migrating from /tests.
 */
function getLegacyTestPath(tsFilePath) {
  const relativePath = path.relative(path.resolve('src/app'), tsFilePath);
  const newFileName = relativePath.replace(/\.ts$/, '.spec.ts');
  return path.join('tests', newFileName);
}

/**
 * Retrieves a list of source files (with their expected test file paths) that lack tests.
 */
function getMissingTests(tsFiles = getAllTsFiles(path.resolve('src/app'))) {
  const missing = [];
  tsFiles.forEach(tsFile => {
    const expectedTestPath = getExpectedTestPath(tsFile);
    const legacyTestPath = getLegacyTestPath(tsFile);
    if (!fs.existsSync(expectedTestPath) && !fs.existsSync(legacyTestPath)) {
      missing.push({ source: tsFile, test: expectedTestPath });
    }
  });
  return missing;
}

export {
  config,
  getAllSpecFilesInSrcApp,
  getAllTsFiles,
  getExpectedTestPath,
  getLegacyTestPath,
  getMissingTests,
  isExcludedFile,
};
