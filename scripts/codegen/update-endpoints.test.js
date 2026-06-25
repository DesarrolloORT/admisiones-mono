import assert from 'node:assert/strict';
import test from 'node:test';

import { generateEndpointFiles } from './update-endpoints.js';

const OPTIONS = {
  outputDir: 'src/app/shared/api/generated/endpoints',
  modelsDir: 'src/app/shared/api/generated/models',
};

test('generates a union for every documented 2xx response schema', () => {
  const generation = generateEndpointFiles(
    swaggerWithOperation('/Auth/Login', 'post', {
      tags: ['Auth'],
      operationId: 'POST /Auth/Login',
      summary: '[Público] Login',
      responses: {
        200: jsonRef('DtoAuthenticationResponseOperationResult'),
        202: jsonRef('DtoLogin2FARequiredOperationResult'),
        400: { description: 'Bad Request' },
      },
    }),
    OPTIONS
  );

  const content = fileContent(generation, 'auth.endpoints.ts');

  assert.match(
    content,
    /response: DtoAuthenticationResponseOperationResult \| DtoLogin2FARequiredOperationResult;/
  );
});

test('generates void for 204-only responses', () => {
  const generation = generateEndpointFiles(
    swaggerWithOperation('/Session', 'delete', {
      tags: ['Session'],
      operationId: 'DELETE /Session',
      summary: '[Público] Logout',
      responses: {
        204: { description: 'No Content' },
      },
    }),
    OPTIONS
  );

  const content = fileContent(generation, 'session.endpoints.ts');

  assert.match(content, /response: void;/);
});

test('deduplicates repeated 2xx response schemas', () => {
  const generation = generateEndpointFiles(
    swaggerWithOperation('/Demo', 'post', {
      tags: ['Demo'],
      operationId: 'POST /Demo',
      summary: '[Público] Demo',
      responses: {
        200: jsonRef('DemoOperationResult'),
        201: jsonRef('DemoOperationResult'),
      },
    }),
    OPTIONS
  );

  const content = fileContent(generation, 'demo.endpoints.ts');

  assert.match(content, /response: DemoOperationResult;/);
  assert.doesNotMatch(content, /DemoOperationResult \| DemoOperationResult/);
});

test('keeps unknown when a 2xx response has no schema', () => {
  const generation = generateEndpointFiles(
    swaggerWithOperation('/Demo', 'get', {
      tags: ['Demo'],
      operationId: 'GET /Demo',
      summary: '[Público] Demo',
      responses: {
        200: { description: 'OK' },
      },
    }),
    OPTIONS
  );

  const content = fileContent(generation, 'demo.endpoints.ts');

  assert.match(content, /response: unknown;/);
  assert.match(generation.warnings.join('\n'), /Response schema could not be resolved/);
});

function swaggerWithOperation(path, method, operation) {
  return {
    paths: {
      [path]: {
        [method]: operation,
      },
    },
  };
}

function jsonRef(schemaName) {
  return {
    description: 'OK',
    content: {
      'application/json': {
        schema: { $ref: `#/components/schemas/${schemaName}` },
      },
    },
  };
}

function fileContent(generation, fileName) {
  const file = generation.files.find(item => item.name === fileName);
  assert.ok(file, `${fileName} was generated`);
  return file.content;
}
