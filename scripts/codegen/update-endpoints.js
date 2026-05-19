import { existsSync, mkdirSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import http from 'node:http';
import https from 'node:https';
import { relative, resolve } from 'node:path';
import { parseArgs as nodeParseArgs } from 'node:util';

import { resolveSwaggerSource, ROOT, toProjectPath } from './codegen-utils.js';

const DEFAULTS = {
  swaggerPath: '/swagger/v1/swagger.json',
  env: 'environment.ts',
  output: 'src/app/shared/api/generated/endpoints',
  models: 'src/app/shared/api/generated/models/model',
};

const SUPPORTED_HTTP_METHODS = ['get', 'post', 'put', 'patch', 'delete'];
const METHODS_WITH_BODY = new Set(['post', 'put', 'patch']);
const GENERATED_HEADER = `// -----------------------------------------------------------------------------
// AUTO-GENERATED FILE.
// Do not edit manually.
// Run: npm run update-endpoints
// -----------------------------------------------------------------------------
`;

const { values: flags } = nodeParseArgs({
  options: {
    env: { type: 'string', default: DEFAULTS.env },
    'swagger-path': { type: 'string', default: DEFAULTS.swaggerPath },
    output: { type: 'string', short: 'o', default: DEFAULTS.output },
    models: { type: 'string', default: DEFAULTS.models },
    check: { type: 'boolean', default: false },
    help: { type: 'boolean', short: 'h', default: false },
  },
  strict: false,
});

if (flags.help) {
  console.log(`
Usage: node scripts/codegen/update-endpoints.js [options]

Options:
  --env <file>            Environment file inside src/environments/ (default: ${DEFAULTS.env})
  --swagger-path <path>   Swagger doc path appended to the API origin (default: ${DEFAULTS.swaggerPath})
  -o, --output <dir>      Output directory for generated endpoints (default: ${DEFAULTS.output})
  --models <dir>          Directory with generated API models (default: ${DEFAULTS.models})
  --check                 Dry-run: report breaking changes without writing files
  -h, --help              Show this help
`);
  process.exit(0);
}

async function main() {
  const swaggerSource = resolveSwaggerSource(flags.env, flags['swagger-path']);
  const outputAbs = resolve(ROOT, flags.output);
  const outputRel = toProjectPath(flags.output);

  console.log(`  env       : src/environments/${flags.env}`);
  console.log(`  origin    : ${swaggerSource.origin}`);
  console.log(`  swagger   : ${swaggerSource.swaggerUrl}`);
  console.log(`  output    : ${outputRel}/\n`);

  const swagger = await downloadJson(swaggerSource.swaggerUrl);
  const generation = generateEndpointFiles(swagger, {
    outputDir: flags.output,
    modelsDir: flags.models,
  });

  const breakingChanges = detectBreakingChanges(outputAbs, generation);

  if (breakingChanges.length > 0) {
    console.warn('\n⚠ Breaking changes detected:');
    for (const change of breakingChanges) {
      console.warn(`\n  ✗ ${change.name} (was in ${change.previousFile})`);
      if (change.movedTo) {
        console.warn(`    → Moved to: ${change.movedTo}`);
      } else {
        console.warn(`    → Removed from API`);
      }
      if (change.usages.length > 0) {
        console.warn(`    Usages in codebase:`);
        for (const usage of change.usages) {
          console.warn(`      ${usage}`);
        }
      }
    }
    console.warn('');
  }

  if (flags.check) {
    const staleImports = detectStaleImports(outputAbs);
    const hasProblems = breakingChanges.length > 0 || staleImports.length > 0;

    if (staleImports.length > 0) {
      console.warn('\n⚠ Stale imports detected (referencing endpoints that no longer exist):');
      for (const stale of staleImports) {
        console.warn(`\n  ${stale.file}:`);
        for (const ref of stale.missing) {
          const suggestion = ref.suggestion ? ` → Did you mean: ${ref.suggestion}` : '';
          console.warn(`    ✗ ${ref.name}${suggestion}`);
        }
      }
      console.warn('');
      printLlmFixPrompt(staleImports);
    }

    if (hasProblems) {
      const parts = [];
      if (breakingChanges.length > 0) {
        const withUsages = breakingChanges.filter(c => c.usages.length > 0);
        parts.push(
          `${breakingChanges.length} breaking change(s)` +
            (withUsages.length > 0 ? ` (${withUsages.length} with active usages)` : '')
        );
      }
      if (staleImports.length > 0) {
        const totalMissing = staleImports.reduce((sum, s) => sum + s.missing.length, 0);
        parts.push(`${totalMissing} stale import(s) in ${staleImports.length} file(s)`);
      }
      console.error(`✗ ${parts.join(', ')}.`);
      process.exit(1);
    }
    console.log('\n✓ No breaking changes or stale imports detected.');
    process.exit(0);
  }

  if (existsSync(outputAbs)) {
    rmSync(outputAbs, { recursive: true, force: true });
  }
  mkdirSync(outputAbs, { recursive: true });

  for (const file of generation.files) {
    writeFileSync(resolve(outputAbs, file.name), await formatTypeScript(file.content), 'utf-8');
  }

  if (generation.warnings.length > 0) {
    console.warn('\nWarnings:');
    for (const warning of generation.warnings) {
      console.warn(`  - ${warning}`);
    }
  }

  console.log(
    `\n✓ Endpoints updated successfully: ${generation.endpointCount} endpoints in ${generation.files.length} files.`
  );

  const staleImports = detectStaleImports(outputAbs);
  if (staleImports.length > 0) {
    console.warn('\n⚠ Stale imports detected (referencing endpoints that no longer exist):');
    for (const stale of staleImports) {
      console.warn(`\n  ${stale.file}:`);
      for (const ref of stale.missing) {
        const suggestion = ref.suggestion ? ` → Did you mean: ${ref.suggestion}` : '';
        console.warn(`    ✗ ${ref.name}${suggestion}`);
      }
    }
    console.warn('');
    printLlmFixPrompt(staleImports);
  }
}

main().catch(error => {
  console.error(`✗ ${error.message}`);
  process.exit(1);
});

function printLlmFixPrompt(staleImports) {
  const lines = [];
  for (const stale of staleImports) {
    for (const ref of stale.missing) {
      if (ref.suggestion) {
        lines.push(`  - ${stale.file}: replace "${ref.name}" with "${ref.suggestion}"`);
      } else {
        lines.push(
          `  - ${stale.file}: remove import and usage of "${ref.name}" (endpoint no longer exists in the API)`
        );
      }
    }
  }

  console.log('─'.repeat(70));
  console.log('Prompt para LLM (copia y pega en Copilot Chat para fix automatico):');
  console.log('─'.repeat(70));
  console.log(`
Los siguientes imports en mi proyecto referencian endpoints generados que
ya no existen en el Swagger actual. Por favor actualiza cada archivo:
${lines.join('\n')}

Para cada caso:
1. Actualiza el import al nuevo nombre y path correcto dentro de "generated/".
2. Actualiza todas las referencias en el archivo al nuevo endpoint.
3. Si no hay sugerencia de reemplazo, elimina el import y el codigo que lo usa,
   y deja un comentario TODO indicando que el endpoint fue removido del API.

Confirma antes de aplicar los cambios.
`);
  console.log('─'.repeat(70));
}

function detectBreakingChanges(outputDir, generation) {
  if (!existsSync(outputDir)) {
    return [];
  }

  const previousNames = extractExportedNames(outputDir);
  const newNames = extractNamesFromGeneration(generation);
  const removedNames = [...previousNames.entries()].filter(([name]) => !newNames.has(name));

  if (removedNames.length === 0) {
    return [];
  }

  const srcDir = resolve(ROOT, 'src');
  const changes = [];

  for (const [name, previousFile] of removedNames) {
    const movedTo = findMovedEndpoint(name, newNames, previousNames);
    const usages = findUsagesInSource(name, srcDir, outputDir);
    changes.push({ name, previousFile, movedTo, usages });
  }

  return changes;
}

function extractExportedNames(outputDir) {
  const names = new Map();

  let files;
  try {
    files = readdirSync(outputDir).filter(f => f.endsWith('.endpoints.ts') && f !== 'index.ts');
  } catch {
    return names;
  }

  for (const file of files) {
    const content = readFileSync(resolve(outputDir, file), 'utf-8');
    for (const match of content.matchAll(/export\s+const\s+(\w+)/g)) {
      names.set(match[1], file);
    }
  }

  return names;
}

function extractNamesFromGeneration(generation) {
  const names = new Map();

  for (const file of generation.files) {
    if (file.name === 'index.ts') continue;
    for (const match of file.content.matchAll(/export\s+const\s+(\w+)/g)) {
      names.set(match[1], file.name);
    }
  }

  return names;
}

function findMovedEndpoint(name, newNames, previousNames) {
  const previousFile = previousNames.get(name);
  const baseName = name.replace(/Endpoint$/, '').replace(/^(get|post|put|patch|delete)/, '');

  for (const [newName, newFile] of newNames) {
    if (newFile === previousFile) continue;
    const newBaseName = newName
      .replace(/Endpoint$/, '')
      .replace(/^(get|post|put|patch|delete)/, '');

    if (baseName.length > 4 && newBaseName.includes(baseName)) {
      return `${newName} (${newFile})`;
    }
  }

  return null;
}

function findUsagesInSource(name, srcDir, outputDir) {
  const usages = [];
  const normalizedOutputDir = resolve(outputDir).replaceAll('\\', '/');

  function walk(dir) {
    let entries;
    try {
      entries = readdirSync(dir, { withFileTypes: true });
    } catch {
      return;
    }

    for (const entry of entries) {
      const fullPath = resolve(dir, entry.name);
      if (entry.isDirectory()) {
        if (resolve(fullPath).replaceAll('\\', '/') === normalizedOutputDir) continue;
        walk(fullPath);
      } else if (entry.isFile() && entry.name.endsWith('.ts')) {
        const content = readFileSync(fullPath, 'utf-8');
        if (content.includes(name)) {
          const lines = content.split('\n');
          for (let i = 0; i < lines.length; i++) {
            if (lines[i].includes(name)) {
              const relPath = relative(ROOT, fullPath).replaceAll('\\', '/');
              usages.push(`${relPath}:${i + 1}`);
            }
          }
        }
      }
    }
  }

  walk(srcDir);
  return usages;
}

function detectStaleImports(outputDir) {
  const srcDir = resolve(ROOT, 'src');
  const allExports = extractExportedNames(outputDir);

  if (allExports.size === 0) {
    return [];
  }

  const staleFiles = [];
  const tsFiles = findTsFilesImportingGenerated(srcDir, outputDir);

  for (const filePath of tsFiles) {
    const content = readFileSync(filePath, 'utf-8');
    const importRegex = /import\s*\{([^}]+)\}\s*from\s*['"][^'"]*\/generated\/[^'"]*['"]/g;
    const missing = [];

    for (const match of content.matchAll(importRegex)) {
      const names = match[1]
        .split(',')
        .map(n =>
          n
            .trim()
            .split(/\s+as\s+/)[0]
            .trim()
        )
        .filter(Boolean);
      for (const name of names) {
        if (!allExports.has(name)) {
          const suggestion = suggestReplacement(name, allExports);
          missing.push({ name, suggestion });
        }
      }
    }

    if (missing.length > 0) {
      const relFile = relative(ROOT, filePath).replaceAll('\\', '/');
      staleFiles.push({ file: relFile, missing });
    }
  }

  return staleFiles;
}

function findTsFilesImportingGenerated(srcDir, outputDir) {
  const results = [];
  const normalizedOutputDir = resolve(outputDir).replaceAll('\\', '/');

  function walk(dir) {
    let entries;
    try {
      entries = readdirSync(dir, { withFileTypes: true });
    } catch {
      return;
    }

    for (const entry of entries) {
      const fullPath = resolve(dir, entry.name);
      if (entry.isDirectory()) {
        if (resolve(fullPath).replaceAll('\\', '/') === normalizedOutputDir) continue;
        walk(fullPath);
      } else if (entry.isFile() && entry.name.endsWith('.ts') && !entry.name.endsWith('.spec.ts')) {
        const content = readFileSync(fullPath, 'utf-8');
        if (content.includes('/generated/')) {
          results.push(fullPath);
        }
      }
    }
  }

  walk(srcDir);
  return results;
}

function suggestReplacement(name, allExports) {
  const baseName = name.replace(/Endpoint$/, '').replace(/^(get|post|put|patch|delete)/, '');

  if (baseName.length < 4) return null;

  const method = name.match(/^(get|post|put|patch|delete)/)?.[0] ?? '';
  const operationName = stripControllerPrefix(baseName);

  if (operationName.length < 4) return null;

  for (const [exportName] of allExports) {
    const exportBase = exportName
      .replace(/Endpoint$/, '')
      .replace(/^(get|post|put|patch|delete)/, '');
    const exportMethod = exportName.match(/^(get|post|put|patch|delete)/)?.[0] ?? '';
    const exportOperation = stripControllerPrefix(exportBase);

    if (method === exportMethod && exportOperation === operationName) {
      return exportName;
    }
  }

  return null;
}

function stripControllerPrefix(name) {
  const match = name.match(/^[A-Z][a-z0-9]*/);
  return match ? name.slice(match[0].length) : name;
}

function downloadJson(url, redirectCount = 0) {
  if (redirectCount > 5) {
    return Promise.reject(new Error(`Too many redirects while downloading Swagger: ${url}`));
  }

  return new Promise((resolvePromise, rejectPromise) => {
    const parsedUrl = new URL(url);
    const client = parsedUrl.protocol === 'http:' ? http : https;
    const requestOptions = {
      headers: {
        Accept: 'application/json',
      },
      rejectUnauthorized: false,
    };

    const request = client.get(parsedUrl, requestOptions, response => {
      const statusCode = response.statusCode ?? 0;
      const location = response.headers.location;

      if (statusCode >= 300 && statusCode < 400 && location) {
        response.resume();
        downloadJson(new URL(location, url).toString(), redirectCount + 1)
          .then(resolvePromise)
          .catch(rejectPromise);
        return;
      }

      if (statusCode < 200 || statusCode >= 300) {
        response.resume();
        rejectPromise(new Error(`Swagger download failed with HTTP ${statusCode}: ${url}`));
        return;
      }

      response.setEncoding('utf-8');
      let raw = '';
      response.on('data', chunk => {
        raw += chunk;
      });
      response.on('end', () => {
        try {
          resolvePromise(JSON.parse(raw));
        } catch (error) {
          rejectPromise(new Error(`Swagger response is not valid JSON: ${error.message}`));
        }
      });
    });

    request.on('error', rejectPromise);
    request.setTimeout(30000, () => {
      request.destroy(new Error(`Swagger download timed out: ${url}`));
    });
  });
}

async function formatTypeScript(content) {
  try {
    const prettier = await import('prettier');
    const config = (await prettier.resolveConfig(resolve(ROOT, '.prettierrc'))) ?? {};
    return await prettier.format(content, {
      ...config,
      parser: 'typescript',
    });
  } catch {
    return content;
  }
}

function generateEndpointFiles(swagger, options) {
  if (
    !swagger ||
    typeof swagger !== 'object' ||
    !swagger.paths ||
    typeof swagger.paths !== 'object'
  ) {
    throw new Error('Swagger document does not contain a valid "paths" object.');
  }

  const context = {
    swagger,
    outputDir: options.outputDir,
    modelsDir: options.modelsDir,
    warnings: [],
    names: new Map(),
  };
  const groups = new Map();

  for (const path of Object.keys(swagger.paths).sort()) {
    const pathItem = resolveMaybeRef(swagger, swagger.paths[path]);
    if (!pathItem || typeof pathItem !== 'object') {
      continue;
    }

    const pathLevelParameters = Array.isArray(pathItem.parameters) ? pathItem.parameters : [];

    for (const method of SUPPORTED_HTTP_METHODS) {
      const operation = resolveMaybeRef(swagger, pathItem[method]);
      if (!operation || typeof operation !== 'object') {
        continue;
      }

      const endpoint = createEndpoint(path, method, operation, pathLevelParameters, context);
      const group = getOrCreateGroup(groups, endpoint.fileName, endpoint.tagName);
      group.imports.set(
        'defineEndpoint',
        relativeImport(options.outputDir, 'src/app/shared/api/core/api-endpoint')
      );

      for (const [typeName, importPath] of endpoint.imports) {
        group.typeImports.set(typeName, importPath);
      }

      group.endpoints.push(endpoint.code);

      if (endpoint.payloadType) {
        group.payloadTypes.push(endpoint.payloadType);
      }
    }
  }

  const files = [...groups.values()]
    .sort((a, b) => a.fileName.localeCompare(b.fileName))
    .map(group => ({
      name: group.fileName,
      content: renderEndpointFile(group),
    }));

  files.push({
    name: 'index.ts',
    content: renderIndexFile(files.map(file => file.name)),
  });

  return {
    endpointCount: [...groups.values()].reduce((total, group) => total + group.endpoints.length, 0),
    files,
    warnings: context.warnings,
  };
}

function createEndpoint(path, method, operation, pathLevelParameters, context) {
  const operationId = getOperationId(path, method, operation);
  const constantName = getEndpointConstantName(path, method, operationId);
  registerEndpointName(constantName, `${method.toUpperCase()} ${path}`, context);

  const tagName = getTagName(operation);
  const fileName = `${toKebabCase(tagName)}.endpoints.ts`;
  const imports = new Map();
  const localContext = {
    ...context,
    imports,
    source: `${operationId} (${method.toUpperCase()} ${path})`,
  };

  const parameters = collectParameters(pathLevelParameters, operation.parameters, context.swagger);
  warnForHeaderParameters(parameters, localContext);

  const pathParamsType = createParametersType(
    parameters.filter(parameter => parameter.in === 'path'),
    true,
    localContext,
    2
  );
  const queryParamsType = createParametersType(
    parameters.filter(parameter => parameter.in === 'query'),
    false,
    localContext,
    2
  );
  const requestType = createRequestType(operation, method, localContext, 2);
  const responseType = createResponseType(operation, localContext, 2);
  const endpointDocComment = buildEndpointDocComment(
    operation,
    method,
    path,
    operationId,
    parameters,
    context.swagger
  );

  const code = `${endpointDocComment}export const ${constantName} = defineEndpoint<{
  pathParams: ${pathParamsType};
  queryParams: ${queryParamsType};
  request: ${requestType};
  response: ${responseType};
}>({
  operationId: ${toTsStringLiteral(operationId)},
  method: '${method.toUpperCase()}',
  path: ${toTsStringLiteral(path)},
});
`;

  const payloadType = createPayloadType(operation, method, constantName, tagName, context.swagger);

  return {
    code,
    constantName,
    fileName,
    imports,
    tagName,
    payloadType,
  };
}

function getOrCreateGroup(groups, fileName, tagName) {
  const existing = groups.get(fileName);
  if (existing) {
    return existing;
  }

  const group = {
    fileName,
    tagName,
    imports: new Map(),
    typeImports: new Map(),
    endpoints: [],
    payloadTypes: [],
  };
  groups.set(fileName, group);
  return group;
}

function renderEndpointFile(group) {
  const imports = [
    ...[...group.imports.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([name, importPath]) => `import { ${name} } from '${importPath}';`),
    ...[...group.typeImports.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([name, importPath]) => `import type { ${name} } from '${importPath}';`),
  ];

  const payloadSection =
    group.payloadTypes.length > 0
      ? `\n// ---------------------------------------------------------------------------\n// Payload types (auto-generated, adapter-safe — no direct backend DTO exposure)\n// ---------------------------------------------------------------------------\n\n${group.payloadTypes.join('\n')}`
      : '';

  return `${GENERATED_HEADER}
${imports.join('\n')}

${group.endpoints.join('\n')}${payloadSection}`;
}

function renderIndexFile(fileNames) {
  const exports = fileNames
    .filter(fileName => fileName !== 'index.ts')
    .sort()
    .map(fileName => `export * from './${fileName.replace(/\.ts$/, '')}';`);

  return `${GENERATED_HEADER}
${exports.join('\n')}
`;
}

function collectParameters(pathLevelParameters, operationParameters = [], swagger) {
  const parameterMap = new Map();

  for (const parameter of [...pathLevelParameters, ...(operationParameters ?? [])]) {
    const resolved = resolveMaybeRef(swagger, parameter);
    if (!resolved || typeof resolved !== 'object' || !resolved.name || !resolved.in) {
      continue;
    }

    parameterMap.set(`${resolved.in}:${resolved.name}`, resolved);
  }

  return [...parameterMap.values()];
}

function createParametersType(parameters, forceRequired, context, indentSpaces) {
  if (parameters.length === 0) {
    return 'never';
  }

  const indent = ' '.repeat(indentSpaces);
  const propertyIndent = ' '.repeat(indentSpaces + 2);
  const lines = parameters.map(parameter => {
    const optional = forceRequired || parameter.required ? '' : '?';
    const type = schemaToType(parameter.schema, context, indentSpaces + 2);
    return `${propertyIndent}${formatPropertyName(parameter.name)}${optional}: ${type};`;
  });

  return `{\n${lines.join('\n')}\n${indent}}`;
}

function createRequestType(operation, method, context, indentSpaces) {
  if (!METHODS_WITH_BODY.has(method)) {
    return 'never';
  }

  const requestBody = resolveMaybeRef(context.swagger, operation.requestBody);
  if (!requestBody) {
    return 'never';
  }

  const contentItem = pickContentItem(requestBody.content);
  if (!contentItem?.schema) {
    context.warnings.push(`Request body schema could not be resolved for ${context.source}.`);
    return 'unknown';
  }

  return schemaToType(contentItem.schema, context, indentSpaces);
}

function createResponseType(operation, context, indentSpaces) {
  const responses = operation.responses;
  if (!responses || typeof responses !== 'object') {
    context.warnings.push(`No 2xx response documented for ${context.source}.`);
    return 'unknown';
  }

  const responseCode = selectMainResponseCode(responses);
  if (!responseCode) {
    context.warnings.push(`No 2xx response documented for ${context.source}.`);
    return 'unknown';
  }

  if (responseCode === '204') {
    return 'void';
  }

  const response = resolveMaybeRef(context.swagger, responses[responseCode]);
  const contentItem = pickContentItem(response?.content);
  if (!contentItem?.schema) {
    context.warnings.push(`Response schema could not be resolved for ${context.source}.`);
    return 'unknown';
  }

  return schemaToType(contentItem.schema, context, indentSpaces);
}

function selectMainResponseCode(responses) {
  const preferred = ['200', '201', '204'];
  for (const code of preferred) {
    if (responses[code]) {
      return code;
    }
  }

  return Object.keys(responses)
    .filter(code => /^2\d\d$/.test(code))
    .sort()[0];
}

function pickContentItem(content) {
  if (!content || typeof content !== 'object') {
    return null;
  }

  const preferredTypes = ['application/json', 'text/json', 'application/*+json'];
  for (const mediaType of preferredTypes) {
    if (content[mediaType]) {
      return content[mediaType];
    }
  }

  const jsonMediaType = Object.keys(content).find(mediaType => mediaType.includes('json'));
  if (jsonMediaType) {
    return content[jsonMediaType];
  }

  const firstMediaType = Object.keys(content)[0];
  return firstMediaType ? content[firstMediaType] : null;
}

function schemaToType(schema, context, indentSpaces) {
  if (!schema || typeof schema !== 'object') {
    context.warnings.push(`Schema could not be resolved for ${context.source}.`);
    return 'unknown';
  }

  if (schema.$ref) {
    const schemaName = schemaNameFromRef(schema.$ref);
    const typeName = toModelTypeName(schemaName);
    context.imports.set(
      typeName,
      relativeImport(context.outputDir, `${context.modelsDir}/${toModelFileName(schemaName)}`)
    );
    return typeName;
  }

  const nullable = isNullableSchema(schema);

  if (Array.isArray(schema.allOf) && schema.allOf.length > 0) {
    return withNullable(
      schema.allOf.map(item => schemaToType(item, context, indentSpaces)).join(' & '),
      nullable
    );
  }

  const unionSchemas =
    Array.isArray(schema.oneOf) && schema.oneOf.length > 0 ? schema.oneOf : schema.anyOf;
  if (Array.isArray(unionSchemas) && unionSchemas.length > 0) {
    return withNullable(
      unionSchemas.map(item => schemaToType(item, context, indentSpaces)).join(' | '),
      nullable
    );
  }

  if (Array.isArray(schema.enum)) {
    return withNullable(
      schema.enum.map(value => JSON.stringify(value)).join(' | ') || 'unknown',
      nullable
    );
  }

  const type = getSchemaType(schema);

  if (type === 'array') {
    const itemType = schemaToType(schema.items, context, indentSpaces);
    return withNullable(`Array<${itemType}>`, nullable);
  }

  if (type === 'object' || schema.properties) {
    if (schema.properties && typeof schema.properties === 'object') {
      return withNullable(createInlineObjectType(schema, context, indentSpaces), nullable);
    }

    if (schema.additionalProperties && typeof schema.additionalProperties === 'object') {
      return withNullable(
        `Record<string, ${schemaToType(schema.additionalProperties, context, indentSpaces)}>`,
        nullable
      );
    }

    if (schema.additionalProperties === true) {
      return withNullable('Record<string, unknown>', nullable);
    }

    context.warnings.push(`Object schema without properties for ${context.source}.`);
    return withNullable('unknown', nullable);
  }

  if (type === 'string') {
    return withNullable(schema.format === 'binary' ? 'Blob' : 'string', nullable);
  }

  if (type === 'integer' || type === 'number') {
    return withNullable('number', nullable);
  }

  if (type === 'boolean') {
    return withNullable('boolean', nullable);
  }

  context.warnings.push(`Unsupported schema type for ${context.source}.`);
  return withNullable('unknown', nullable);
}

function createInlineObjectType(schema, context, indentSpaces) {
  const indent = ' '.repeat(indentSpaces);
  const propertyIndent = ' '.repeat(indentSpaces + 2);
  const required = new Set(Array.isArray(schema.required) ? schema.required : []);
  const lines = Object.keys(schema.properties)
    .sort()
    .map(propertyName => {
      const propertySchema = schema.properties[propertyName];
      const optional = required.has(propertyName) ? '' : '?';
      const type = schemaToType(propertySchema, context, indentSpaces + 2);
      return `${propertyIndent}${formatPropertyName(propertyName)}${optional}: ${type};`;
    });

  return `{\n${lines.join('\n')}\n${indent}}`;
}

function getSchemaType(schema) {
  if (Array.isArray(schema.type)) {
    return schema.type.find(type => type !== 'null');
  }

  return schema.type;
}

function isNullableSchema(schema) {
  return schema.nullable === true || (Array.isArray(schema.type) && schema.type.includes('null'));
}

function withNullable(type, nullable) {
  return nullable && type !== 'null' ? `${type} | null` : type;
}

function resolveMaybeRef(swagger, value) {
  if (!value || typeof value !== 'object' || !value.$ref) {
    return value;
  }

  return resolveRef(swagger, value.$ref);
}

function resolveRef(swagger, ref) {
  if (!ref.startsWith('#/')) {
    throw new Error(`Only local Swagger refs are supported: ${ref}`);
  }

  return ref
    .slice(2)
    .split('/')
    .map(segment => segment.replaceAll('~1', '/').replaceAll('~0', '~'))
    .reduce((current, segment) => {
      if (!current || typeof current !== 'object' || !(segment in current)) {
        throw new Error(`Swagger ref could not be resolved: ${ref}`);
      }
      return current[segment];
    }, swagger);
}

function schemaNameFromRef(ref) {
  return ref.split('/').at(-1).replaceAll('~1', '/').replaceAll('~0', '~');
}

function getOperationId(path, method, operation) {
  const operationId = typeof operation.operationId === 'string' ? operation.operationId.trim() : '';
  return operationId || `${method.toUpperCase()} ${path}`;
}

function getEndpointConstantName(path, method, operationId) {
  const source = operationId || `${method}_${path}`;
  const baseName = toCamelCase(source) || toCamelCase(`${method}_${path}`) || 'api';
  const constantName = `${baseName}Endpoint`;
  return /^[A-Za-z_$]/.test(constantName) ? constantName : `operation${constantName}`;
}

function registerEndpointName(name, source, context) {
  const previousSource = context.names.get(name);
  if (previousSource) {
    throw new Error(
      `Endpoint name collision: "${name}" is generated by both "${previousSource}" and "${source}".`
    );
  }

  context.names.set(name, source);
}

function getTagName(operation) {
  const tag = Array.isArray(operation.tags)
    ? operation.tags.find(item => typeof item === 'string')
    : null;
  return tag?.trim() || 'general';
}

// ---------------------------------------------------------------------------
// Payload type generation
// ---------------------------------------------------------------------------

function createPayloadType(operation, method, constantName, tagName, swagger) {
  if (!METHODS_WITH_BODY.has(method)) {
    return null;
  }

  const requestBody = resolveMaybeRef(swagger, operation.requestBody);
  if (!requestBody) {
    return null;
  }

  const contentItem = pickContentItem(requestBody.content);
  if (!contentItem?.schema) {
    return null;
  }

  const schema = contentItem.schema.$ref
    ? resolveRef(swagger, contentItem.schema.$ref)
    : contentItem.schema;

  if (!schema || (!schema.properties && !schema.allOf)) {
    return null;
  }

  const payloadName = derivePayloadName(constantName, tagName);
  const body = schemaToInlineInterface(schema, swagger, 2);

  return `/** Auto-generated payload for \`${constantName}\`. */\nexport interface ${payloadName} ${body}\n`;
}

function derivePayloadName(constantName, tagName) {
  const base = constantName.replace(/Endpoint$/, '').replace(/^(get|post|put|patch|delete)/, '');

  const tagPrefix = tagName.charAt(0).toUpperCase() + tagName.slice(1).toLowerCase();
  const normalizedBase = base.charAt(0).toUpperCase() + base.slice(1);

  const withoutTag =
    normalizedBase.startsWith(tagPrefix) && normalizedBase.length > tagPrefix.length
      ? normalizedBase.slice(tagPrefix.length)
      : normalizedBase;

  const name = withoutTag.charAt(0).toUpperCase() + withoutTag.slice(1);
  return `${name}Payload`;
}

function schemaToInlineInterface(schema, swagger, indentSpaces, visited = new Set()) {
  const merged = mergeSchemaAllOf(schema, swagger);
  const indent = ' '.repeat(indentSpaces);
  const propIndent = ' '.repeat(indentSpaces + 2);
  const required = new Set(Array.isArray(merged.required) ? merged.required : []);

  if (!merged.properties || typeof merged.properties !== 'object') {
    return '{}';
  }

  const lines = Object.keys(merged.properties)
    .sort()
    .map(name => {
      const propSchema = merged.properties[name];
      const optional = required.has(name) ? '' : '?';
      const type = schemaToInlineFieldType(propSchema, swagger, indentSpaces + 2, visited);
      return `${propIndent}${formatPropertyName(name)}${optional}: ${type};`;
    });

  return `{\n${lines.join('\n')}\n${indent}}`;
}

function schemaToInlineFieldType(schema, swagger, indentSpaces, visited = new Set()) {
  if (!schema || typeof schema !== 'object') {
    return 'unknown';
  }

  if (schema.$ref) {
    if (visited.has(schema.$ref)) {
      return 'unknown'; // circular ref guard
    }
    visited = new Set(visited);
    visited.add(schema.$ref);
    const resolved = resolveRef(swagger, schema.$ref);
    return schemaToInlineFieldType(resolved, swagger, indentSpaces, visited);
  }

  const nullable = isNullableSchema(schema);

  if (Array.isArray(schema.allOf) && schema.allOf.length > 0) {
    const merged = mergeSchemaAllOf(schema, swagger);
    if (merged.properties) {
      return withNullable(
        schemaToInlineInterface(merged, swagger, indentSpaces, visited),
        nullable
      );
    }
    return withNullable(
      schema.allOf
        .map(item => schemaToInlineFieldType(item, swagger, indentSpaces, visited))
        .join(' & '),
      nullable
    );
  }

  const unionSchemas =
    Array.isArray(schema.oneOf) && schema.oneOf.length > 0 ? schema.oneOf : schema.anyOf;
  if (Array.isArray(unionSchemas) && unionSchemas.length > 0) {
    return withNullable(
      unionSchemas
        .map(item => schemaToInlineFieldType(item, swagger, indentSpaces, visited))
        .join(' | '),
      nullable
    );
  }

  if (Array.isArray(schema.enum)) {
    return withNullable(
      schema.enum.map(value => JSON.stringify(value)).join(' | ') || 'unknown',
      nullable
    );
  }

  const type = getSchemaType(schema);

  if (type === 'array') {
    const itemType = schemaToInlineFieldType(schema.items, swagger, indentSpaces, visited);
    return withNullable(`Array<${itemType}>`, nullable);
  }

  if (type === 'object' || schema.properties) {
    if (schema.properties && typeof schema.properties === 'object') {
      return withNullable(
        schemaToInlineInterface(schema, swagger, indentSpaces, visited),
        nullable
      );
    }
    if (schema.additionalProperties && typeof schema.additionalProperties === 'object') {
      return withNullable(
        `Record<string, ${schemaToInlineFieldType(schema.additionalProperties, swagger, indentSpaces, visited)}>`,
        nullable
      );
    }
    return withNullable('Record<string, unknown>', nullable);
  }

  if (type === 'string') {
    return withNullable(schema.format === 'binary' ? 'Blob' : 'string', nullable);
  }
  if (type === 'integer' || type === 'number') {
    return withNullable('number', nullable);
  }
  if (type === 'boolean') {
    return withNullable('boolean', nullable);
  }

  return withNullable('unknown', nullable);
}

function mergeSchemaAllOf(schema, swagger) {
  if (!Array.isArray(schema.allOf) || schema.allOf.length === 0) {
    return schema;
  }

  const merged = { properties: {}, required: [...(schema.required ?? [])] };

  for (const subSchema of schema.allOf) {
    const resolved = subSchema.$ref ? resolveRef(swagger, subSchema.$ref) : subSchema;
    if (resolved.properties) {
      Object.assign(merged.properties, resolved.properties);
    }
    if (Array.isArray(resolved.required)) {
      merged.required.push(...resolved.required);
    }
  }

  if (schema.properties) {
    Object.assign(merged.properties, schema.properties);
  }

  return merged;
}

// ---------------------------------------------------------------------------
// Endpoint documentation
// ---------------------------------------------------------------------------

function buildEndpointDocComment(operation, method, path, operationId, parameters, swagger) {
  const lines = [];

  const summary = sanitizeDocText(operation.summary);
  const description = sanitizeDocText(operation.description);

  if (summary) {
    lines.push(summary);
  }

  if (description) {
    for (const line of description.split('\n')) {
      const trimmedLine = line.trim();
      if (!trimmedLine) {
        lines.push('');
        continue;
      }
      if (summary && trimmedLine === summary) {
        continue;
      }
      lines.push(trimmedLine);
    }
  }

  const requestBody = resolveMaybeRef(swagger, operation.requestBody);
  const requestDescription = sanitizeDocText(requestBody?.description);
  if (requestDescription) {
    if (lines.length > 0 && lines.at(-1) !== '') {
      lines.push('');
    }
    lines.push(`Request: ${requestDescription}`);
  }

  const parameterLines = (parameters ?? [])
    .map(parameter => {
      const parameterDescription = sanitizeDocText(parameter.description);
      if (!parameterDescription) {
        return '';
      }
      return `Param (${parameter.in}) ${parameter.name}: ${parameterDescription}`;
    })
    .filter(Boolean);

  if (parameterLines.length > 0) {
    if (lines.length > 0 && lines.at(-1) !== '') {
      lines.push('');
    }
    lines.push(...parameterLines);
  }

  const responseLines = Object.keys(operation.responses ?? {})
    .sort((a, b) => a.localeCompare(b))
    .map(code => {
      const response = resolveMaybeRef(swagger, operation.responses[code]);
      const responseDescription = sanitizeDocText(response?.description);
      if (!responseDescription) {
        return '';
      }
      return `Response ${code}: ${responseDescription}`;
    })
    .filter(Boolean);

  if (responseLines.length > 0) {
    if (lines.length > 0 && lines.at(-1) !== '') {
      lines.push('');
    }
    lines.push(...responseLines);
  }

  if (lines.length > 0 && lines.at(-1) !== '') {
    lines.push('');
  }

  const metadataLines = [`Backend: ${method.toUpperCase()} ${path}`, `OperationId: ${operationId}`];

  if (operation.deprecated === true) {
    metadataLines.push('@deprecated Deprecated in Swagger contract.');
  }

  lines.push(...metadataLines);

  const commentBody = lines
    .map(line => {
      if (!line) {
        return ' *';
      }
      return ` * ${line}`;
    })
    .join('\n');

  return `/**\n${commentBody}\n */\n`;
}

function sanitizeDocText(value) {
  if (typeof value !== 'string') {
    return '';
  }

  return value.replaceAll('\r\n', '\n').replaceAll('\r', '\n').replaceAll('*/', '* /').trim();
}

function warnForHeaderParameters(parameters, context) {
  const headerParameters = parameters.filter(parameter => parameter.in === 'header');
  if (headerParameters.length === 0) {
    return;
  }

  context.warnings.push(
    `Header parameters ignored for ${context.source}: ${headerParameters.map(parameter => parameter.name).join(', ')}.`
  );
}

function formatPropertyName(name) {
  return isValidIdentifier(name) ? name : toTsStringLiteral(name);
}

function toTsStringLiteral(value) {
  return `'${String(value)
    .replaceAll('\\', '\\\\')
    .replaceAll("'", "\\'")
    .replaceAll('\r', '\\r')
    .replaceAll('\n', '\\n')}'`;
}

function isValidIdentifier(value) {
  return /^[A-Za-z_$][\w$]*$/.test(value) && !RESERVED_WORDS.has(value);
}

function relativeImport(fromDir, toPathWithoutExtension) {
  const fromAbs = resolve(ROOT, fromDir);
  const toAbs = resolve(ROOT, toPathWithoutExtension);
  let importPath = relative(fromAbs, toAbs).replaceAll('\\', '/');

  if (!importPath.startsWith('.')) {
    importPath = `./${importPath}`;
  }

  return importPath;
}

function toModelFileName(schemaName) {
  const cleanName = schemaName.replace(/[^A-Za-z0-9_$]/g, '');
  return `${cleanName.charAt(0).toLowerCase()}${cleanName.slice(1)}`;
}

function toModelTypeName(schemaName) {
  const cleanName = schemaName.replace(/[^A-Za-z0-9_$]/g, '');
  return /^[A-Za-z_$]/.test(cleanName) ? cleanName : `Model${cleanName}`;
}

function toCamelCase(value) {
  const words = toWords(value);
  if (words.length === 0) {
    return '';
  }

  const [firstWord, ...rest] = words;
  return [firstWord.toLowerCase(), ...rest.map(toPascalCase)].join('');
}

function toPascalCase(value) {
  return `${value.charAt(0).toUpperCase()}${value.slice(1).toLowerCase()}`;
}

function toKebabCase(value) {
  return toWords(value).join('-').toLowerCase() || 'general';
}

function toWords(value) {
  return String(value)
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2')
    .replace(/[^A-Za-z0-9]+/g, ' ')
    .trim()
    .split(/\s+/)
    .filter(Boolean);
}

const RESERVED_WORDS = new Set([
  'abstract',
  'any',
  'as',
  'asserts',
  'bigint',
  'boolean',
  'break',
  'case',
  'catch',
  'class',
  'const',
  'constructor',
  'continue',
  'debugger',
  'declare',
  'default',
  'delete',
  'do',
  'else',
  'enum',
  'export',
  'extends',
  'false',
  'finally',
  'for',
  'from',
  'function',
  'get',
  'if',
  'implements',
  'import',
  'in',
  'infer',
  'instanceof',
  'interface',
  'is',
  'keyof',
  'let',
  'module',
  'namespace',
  'never',
  'new',
  'null',
  'number',
  'object',
  'of',
  'package',
  'private',
  'protected',
  'public',
  'readonly',
  'require',
  'return',
  'set',
  'static',
  'string',
  'super',
  'switch',
  'symbol',
  'this',
  'throw',
  'true',
  'try',
  'type',
  'typeof',
  'undefined',
  'unique',
  'unknown',
  'var',
  'void',
  'while',
  'with',
  'yield',
]);

