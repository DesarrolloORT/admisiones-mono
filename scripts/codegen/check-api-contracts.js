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
  const bodyViolations = findAdapterBodyLaunderingViolations(program, root);
  const bodyShapeViolations = findAdapterRequestBodyShapeViolations(program, root);
  const assertionViolations = findAdapterUnsafeAssertionViolations(program, root);
  const responseViolations = findUnknownResponseViolations(
    resolve(root, generatedEndpointsDir),
    root
  );

  return [
    ...adapterViolations,
    ...bodyViolations,
    ...bodyShapeViolations,
    ...assertionViolations,
    ...responseViolations,
  ];
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

        const returnType = checker.getReturnTypeOfSignature(signature);
        const returnLeak = findGeneratedType(returnType, checker, root);
        if (returnLeak) {
          violations.push({
            file: toProjectPath(sourceFile.fileName, root),
            line: sourceFile.getLineAndCharacterOfPosition(member.name.getStart()).line + 1,
            message: `El método público ${methodName} expone un tipo generated en su retorno (${returnLeak}).`,
          });
        }

        const returnUnsafe = findUnsafeType(returnType, checker, root);
        if (returnUnsafe) {
          violations.push({
            file: toProjectPath(sourceFile.fileName, root),
            line: sourceFile.getLineAndCharacterOfPosition(member.name.getStart()).line + 1,
            message: `El método público ${methodName} expone ${returnUnsafe} en su retorno.`,
          });
        }
        for (const parameter of member.parameters) {
          const parameterType = checker.getTypeAtLocation(parameter);
          const parameterLeak = findGeneratedType(parameterType, checker, root);
          if (parameterLeak) {
            violations.push({
              file: toProjectPath(sourceFile.fileName, root),
              line: sourceFile.getLineAndCharacterOfPosition(parameter.getStart()).line + 1,
              message: `El método público ${methodName} expone un tipo generated en el parámetro ${parameter.name.getText(sourceFile)} (${parameterLeak}).`,
            });
          }

          const parameterUnsafe = findUnsafeType(parameterType, checker, root);
          if (parameterUnsafe) {
            violations.push({
              file: toProjectPath(sourceFile.fileName, root),
              line: sourceFile.getLineAndCharacterOfPosition(parameter.getStart()).line + 1,
              message: `El método público ${methodName} expone ${parameterUnsafe} en el parámetro ${parameter.name.getText(sourceFile)}.`,
            });
          }
        }
      }
    }
  }

  return violations;
}

export function findAdapterBodyLaunderingViolations(program, root) {
  const violations = [];

  for (const sourceFile of program.getSourceFiles()) {
    const fileName = normalizePath(sourceFile.fileName);
    if (!isEndpointAdapter(fileName, root)) continue;

    violations.push(
      ...findBodyLaunderingInSource(sourceFile.text, sourceFile.fileName).map(violation => ({
        ...violation,
        file: toProjectPath(sourceFile.fileName, root),
      }))
    );
  }

  return violations;
}

export function findAdapterRequestBodyShapeViolations(program, root) {
  const checker = program.getTypeChecker();
  const violations = [];

  for (const sourceFile of program.getSourceFiles()) {
    const fileName = normalizePath(sourceFile.fileName);
    if (!isEndpointAdapter(fileName, root)) continue;

    const visit = node => {
      const bodyProperty = getRequestBodyProperty(node);
      const bodyInitializer = getBodyInitializer(bodyProperty);
      if (bodyInitializer) {
        const expectedType = getExpectedRequestBodyType(node, bodyInitializer, checker);
        const actualType = checker.getTypeAtLocation(bodyInitializer);
        if (expectedType && !isLooseType(expectedType) && !isLooseType(actualType)) {
          const expectedProperties = new Set(
            checker.getPropertiesOfType(expectedType).map(property => property.getName())
          );
          const unexpectedProperties = checker
            .getPropertiesOfType(actualType)
            .map(property => property.getName())
            .filter(property => !expectedProperties.has(property));

          if (unexpectedProperties.length) {
            violations.push({
              file: toProjectPath(sourceFile.fileName, root),
              line: sourceFile.getLineAndCharacterOfPosition(bodyInitializer.getStart()).line + 1,
              message: `El body contiene propiedades fuera del contrato generated (${unexpectedProperties.join(', ')}). Corregí el mapper o el passthrough contra el request actual.`,
            });
          }
        }
      }

      ts.forEachChild(node, visit);
    };

    visit(sourceFile);
  }

  return violations;
}

export function findAdapterUnsafeAssertionViolations(program, root) {
  const violations = [];

  for (const sourceFile of program.getSourceFiles()) {
    const fileName = normalizePath(sourceFile.fileName);
    if (!isEndpointAdapter(fileName, root)) continue;

    violations.push(
      ...findUnsafeAssertionsInSource(sourceFile.text, sourceFile.fileName).map(violation => ({
        ...violation,
        file: toProjectPath(sourceFile.fileName, root),
      }))
    );
  }

  return violations;
}

export function findUnsafeAssertionsInSource(sourceText, fileName = 'adapter.endpoint.ts') {
  const sourceFile = ts.createSourceFile(fileName, sourceText, ts.ScriptTarget.Latest, true);
  const violations = [];

  const visit = node => {
    if (
      ts.isAsExpression(node) &&
      ts.isAsExpression(node.expression) &&
      isTopLevelUnknown(node.expression.type)
    ) {
      violations.push({
        line: sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile)).line + 1,
        message:
          'El adapter usa doble assertion as unknown as; corregí el contrato generado o discriminá con tipos reales.',
      });
    }

    ts.forEachChild(node, visit);
  };

  visit(sourceFile);
  return violations;
}
export function findBodyLaunderingInSource(sourceText, fileName = 'adapter.endpoint.ts') {
  const sourceFile = ts.createSourceFile(fileName, sourceText, ts.ScriptTarget.Latest, true);
  const violations = [];

  const visit = node => {
    const bodyProperty = getRequestBodyProperty(node);
    const bodyInitializer = getBodyInitializer(bodyProperty);
    if (bodyInitializer) {
      for (const call of findCallExpressions(bodyInitializer)) {
        violations.push({
          line: sourceFile.getLineAndCharacterOfPosition(call.getStart(sourceFile)).line + 1,
          message:
            'El body del adapter transforma valores con una llamada; mové el mapeo a mappers/facade y dejá el body como passthrough para que TS valide contra el contrato generado.',
        });
      }
    }

    ts.forEachChild(node, visit);
  };

  visit(sourceFile);
  return violations;
}

function getRequestBodyProperty(node) {
  if (!ts.isCallExpression(node)) return null;

  const callee = node.expression;
  if (
    !ts.isPropertyAccessExpression(callee) ||
    !['request', 'data', 'requestWithMessage'].includes(callee.name.text)
  ) {
    return null;
  }

  const options = node.arguments[1];
  if (!options || !ts.isObjectLiteralExpression(options)) return null;

  return (
    options.properties.find(property => {
      if (ts.isPropertyAssignment(property) || ts.isShorthandPropertyAssignment(property)) {
        return ts.isIdentifier(property.name) && property.name.text === 'body';
      }
      return false;
    }) ?? null
  );
}

function getBodyInitializer(bodyProperty) {
  if (!bodyProperty) return null;
  return ts.isPropertyAssignment(bodyProperty) ? bodyProperty.initializer : bodyProperty.name;
}

function getExpectedRequestBodyType(call, bodyProperty, checker) {
  const endpoint = call.arguments[0];
  if (endpoint) {
    const endpointType = checker.getTypeAtLocation(endpoint);
    const definition = checker.getTypeOfPropertyOfType(endpointType, '__types');
    const definitionType = definition ? checker.getNonNullableType(definition) : null;
    const requestType = definitionType
      ? checker.getTypeOfPropertyOfType(definitionType, 'request')
      : null;
    if (requestType) return checker.getNonNullableType(requestType);
  }

  const signature = checker.getResolvedSignature(call);
  const optionsParameter = signature?.getParameters()[1];
  if (!optionsParameter) return null;

  const optionsType = checker.getTypeOfSymbolAtLocation(optionsParameter, call);
  const bodySymbol = checker.getPropertyOfType(optionsType, 'body');
  if (!bodySymbol) return null;

  return checker.getNonNullableType(checker.getTypeOfSymbolAtLocation(bodySymbol, bodyProperty));
}

function isLooseType(type) {
  return Boolean(type.flags & (ts.TypeFlags.Any | ts.TypeFlags.Unknown | ts.TypeFlags.Never));
}

function findCallExpressions(node) {
  const calls = [];

  const visit = current => {
    if (ts.isCallExpression(current) || ts.isNewExpression(current)) {
      calls.push(current);
    }
    ts.forEachChild(current, visit);
  };

  visit(node);
  return calls;
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

function findUnsafeType(type, checker, root, seen = new Set()) {
  if (!type || seen.has(type)) return null;
  seen.add(type);

  if (type.flags & ts.TypeFlags.Any) return 'any';
  if (type.flags & ts.TypeFlags.Unknown) return 'unknown';

  if (type.isUnionOrIntersection()) {
    for (const child of type.types) {
      const unsafe = findUnsafeType(child, checker, root, seen);
      if (unsafe) return unsafe;
    }
  }

  if (type.flags & ts.TypeFlags.Object) {
    const typeArguments = checker.getTypeArguments(type);
    for (const argument of typeArguments) {
      const unsafe = findUnsafeType(argument, checker, root, seen);
      if (unsafe) return unsafe;
    }
  }

  const symbol = type.aliasSymbol ?? type.getSymbol();
  const declarationFile = symbol?.declarations?.[0]?.getSourceFile().fileName;
  if (declarationFile && normalizePath(declarationFile).startsWith(`${normalizePath(root)}/src/`)) {
    for (const property of checker.getPropertiesOfType(type)) {
      const declaration = property.valueDeclaration ?? property.declarations?.[0];
      if (!declaration) continue;
      const unsafe = findUnsafeType(
        checker.getTypeOfSymbolAtLocation(property, declaration),
        checker,
        root,
        seen
      );
      if (unsafe) return unsafe;
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
