import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { relative, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

import ts from 'typescript';

const ROOT = resolve(fileURLToPath(new URL('../..', import.meta.url)));
const GENERATED_SEGMENT = '/src/app/shared/api/generated/';

export function checkApiContracts({
  root = ROOT,
  tsconfigPath = 'tsconfig.app.json',
  generatedEndpointsDir = 'src/app/shared/api/generated/endpoints',
} = {}) {
  const program = createProgram(root, tsconfigPath);
  const adapterViolations = findAdapterContractViolations(program, root);
  const responseViolations = findUnknownResponseViolations(
    resolve(root, generatedEndpointsDir),
    root
  );

  return [...adapterViolations, ...responseViolations];
}

export function findAdapterContractViolations(program, root) {
  const checker = program.getTypeChecker();
  const violations = [];

  for (const sourceFile of program.getSourceFiles()) {
    const fileName = normalizePath(sourceFile.fileName);
    if (!isEndpointAdapter(fileName, root)) continue;

    for (const statement of sourceFile.statements) {
      if (!ts.isClassDeclaration(statement)) continue;

      for (const member of statement.members) {
        if (!ts.isMethodDeclaration(member) || !isPublic(member)) continue;

        const methodName = member.name.getText(sourceFile);
        const signature = checker.getSignatureFromDeclaration(member);
        if (!signature) continue;

        const returnLeak = findGeneratedType(
          checker.getReturnTypeOfSignature(signature),
          checker,
          root
        );
        if (returnLeak) {
          violations.push({
            file: toProjectPath(sourceFile.fileName, root),
            line: sourceFile.getLineAndCharacterOfPosition(member.name.getStart()).line + 1,
            message: `El método público ${methodName} expone un tipo generated en su retorno (${returnLeak}).`,
          });
        }

        for (const parameter of member.parameters) {
          const parameterLeak = findGeneratedType(
            checker.getTypeAtLocation(parameter),
            checker,
            root
          );
          if (!parameterLeak) continue;

          violations.push({
            file: toProjectPath(sourceFile.fileName, root),
            line: sourceFile.getLineAndCharacterOfPosition(parameter.getStart()).line + 1,
            message: `El método público ${methodName} expone un tipo generated en el parámetro ${parameter.name.getText(sourceFile)} (${parameterLeak}).`,
          });
        }
      }
    }
  }

  return violations;
}

export function findUnknownResponsesInSource(sourceText, fileName = 'generated.endpoints.ts') {
  const sourceFile = ts.createSourceFile(fileName, sourceText, ts.ScriptTarget.Latest, true);
  const violations = [];

  const visit = node => {
    if (ts.isCallExpression(node) && node.typeArguments?.length) {
      const definition = node.typeArguments[0];
      if (ts.isTypeLiteralNode(definition)) {
        const response = definition.members.find(
          member =>
            ts.isPropertySignature(member) &&
            member.name &&
            member.name.getText(sourceFile) === 'response'
        );

        if (response && ts.isPropertySignature(response) && isTopLevelUnknown(response.type)) {
          const declaration = findEndpointDeclaration(node);
          const endpointName = declaration?.name.getText(sourceFile) ?? 'endpoint generado';
          violations.push({
            line: sourceFile.getLineAndCharacterOfPosition(response.getStart()).line + 1,
            message: `${endpointName} tiene response: unknown; Swagger debe publicar un schema 2xx tipado.`,
          });
        }
      }
    }

    ts.forEachChild(node, visit);
  };

  visit(sourceFile);
  return violations;
}

function createProgram(root, tsconfigPath) {
  const configFile = resolve(root, tsconfigPath);
  const config = ts.readConfigFile(configFile, ts.sys.readFile);
  if (config.error) {
    throw new Error(ts.flattenDiagnosticMessageText(config.error.messageText, '\n'));
  }

  const parsed = ts.parseJsonConfigFileContent(config.config, ts.sys, root, undefined, configFile);
  if (parsed.errors.length) {
    throw new Error(
      parsed.errors
        .map(error => ts.flattenDiagnosticMessageText(error.messageText, '\n'))
        .join('\n')
    );
  }

  return ts.createProgram({ rootNames: parsed.fileNames, options: parsed.options });
}

function findGeneratedType(type, checker, root, seen = new Set()) {
  if (!type || seen.has(type)) return null;
  seen.add(type);

  const symbol = type.aliasSymbol ?? type.getSymbol();
  const generatedDeclaration = symbol?.declarations?.find(declaration =>
    normalizePath(declaration.getSourceFile().fileName).includes(GENERATED_SEGMENT)
  );
  if (generatedDeclaration) {
    return toProjectPath(generatedDeclaration.getSourceFile().fileName, root);
  }

  if (type.isUnionOrIntersection()) {
    for (const child of type.types) {
      const leak = findGeneratedType(child, checker, root, seen);
      if (leak) return leak;
    }
  }

  if (type.flags & ts.TypeFlags.Object) {
    const typeArguments = checker.getTypeArguments(type);
    for (const argument of typeArguments) {
      const leak = findGeneratedType(argument, checker, root, seen);
      if (leak) return leak;
    }
  }

  const declarationFile = symbol?.declarations?.[0]?.getSourceFile().fileName;
  if (declarationFile && normalizePath(declarationFile).startsWith(`${normalizePath(root)}/src/`)) {
    for (const property of checker.getPropertiesOfType(type)) {
      const declaration = property.valueDeclaration ?? property.declarations?.[0];
      if (!declaration) continue;
      const leak = findGeneratedType(
        checker.getTypeOfSymbolAtLocation(property, declaration),
        checker,
        root,
        seen
      );
      if (leak) return leak;
    }
  }

  return null;
}

function findUnknownResponseViolations(directory, root) {
  if (!existsSync(directory)) return [];

  return findFiles(directory, file => file.endsWith('.endpoints.ts')).flatMap(file =>
    findUnknownResponsesInSource(readFileSync(file, 'utf8'), file).map(violation => ({
      ...violation,
      file: toProjectPath(file, root),
    }))
  );
}

function findFiles(directory, predicate) {
  return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const path = resolve(directory, entry.name);
    if (entry.isDirectory()) return findFiles(path, predicate);
    return entry.isFile() && predicate(path) ? [path] : [];
  });
}

function isEndpointAdapter(fileName, root) {
  const featureRoot = `${normalizePath(root)}/src/app/features/`;
  return (
    fileName.startsWith(featureRoot) &&
    fileName.includes('/endpoints/') &&
    fileName.endsWith('.endpoint.ts')
  );
}

function isPublic(member) {
  return !member.modifiers?.some(
    modifier =>
      modifier.kind === ts.SyntaxKind.PrivateKeyword ||
      modifier.kind === ts.SyntaxKind.ProtectedKeyword
  );
}

function isTopLevelUnknown(type) {
  if (!type) return false;
  if (type.kind === ts.SyntaxKind.UnknownKeyword) return true;
  if (ts.isParenthesizedTypeNode(type)) return isTopLevelUnknown(type.type);
  if (ts.isUnionTypeNode(type)) return type.types.some(isTopLevelUnknown);
  return false;
}

function findEndpointDeclaration(node) {
  const parent = node.parent;
  return parent && ts.isVariableDeclaration(parent) ? parent : null;
}

function normalizePath(path) {
  return resolve(path).replaceAll('\\', '/');
}

function toProjectPath(path, root) {
  return relative(root, path).replaceAll('\\', '/');
}

function printViolations(violations) {
  if (!violations.length) {
    console.log('✓ Contratos API encapsulados y responses generadas tipadas.');
    return;
  }

  console.error('✗ Se encontraron contratos API inestables:');
  for (const violation of violations) {
    console.error(`  ${violation.file}:${violation.line} ${violation.message}`);
  }
}

const isMain = process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url;
if (isMain) {
  try {
    const violations = checkApiContracts();
    printViolations(violations);
    process.exitCode = violations.length ? 1 : 0;
  } catch (error) {
    console.error(`✗ No se pudieron validar los contratos API: ${error.message}`);
    process.exitCode = 1;
  }
}
