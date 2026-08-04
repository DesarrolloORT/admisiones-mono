import assert from 'node:assert/strict';
import test from 'node:test';

import { sanitizeOpenApiSchemaNames } from './update-models.js';

test('normalizes invalid schema names and matching refs', () => {
  const swagger = {
    components: {
      schemas: {
        'StringString<>f__AnonymousType1': { type: 'object' },
        Wrapper: {
          properties: {
            value: { $ref: '#/components/schemas/StringString<>f__AnonymousType1' },
            encoded: { $ref: '#/components/schemas/StringString%3C%3Ef__AnonymousType1' },
          },
        },
      },
    },
  };

  const result = sanitizeOpenApiSchemaNames(swagger);

  assert.deepEqual(result.renamedSchemas, [
    { from: 'StringString<>f__AnonymousType1', to: 'StringStringf__AnonymousType1' },
  ]);
  assert.ok(swagger.components.schemas.StringStringf__AnonymousType1);
  assert.equal(swagger.components.schemas['StringString<>f__AnonymousType1'], undefined);
  assert.equal(
    swagger.components.schemas.Wrapper.properties.value.$ref,
    '#/components/schemas/StringStringf__AnonymousType1'
  );
  assert.equal(
    swagger.components.schemas.Wrapper.properties.encoded.$ref,
    '#/components/schemas/StringStringf__AnonymousType1'
  );
});
