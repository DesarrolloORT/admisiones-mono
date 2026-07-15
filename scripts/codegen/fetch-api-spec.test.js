import assert from 'node:assert/strict';
import { readFileSync, rmSync } from 'node:fs';
import { createServer } from 'node:http';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';

import { fetchApiSpec } from './fetch-api-spec.js';

const SWAGGER = { openapi: '3.0.1', paths: {}, components: { schemas: {} } };
const CONTRACTS_INDEX = [{ name: 'persona.json', url: '/contracts/persona.json' }];
const PERSONA_CONTRACT = { title: 'Persona', fields: [] };

function startFakeApi(routes) {
  return new Promise(resolvePromise => {
    const server = createServer((request, response) => {
      const body = routes[request.url];
      if (body === undefined) {
        response.writeHead(404).end();
        return;
      }
      response.writeHead(200, { 'Content-Type': 'application/json' });
      response.end(JSON.stringify(body));
    });
    server.listen(0, '127.0.0.1', () => {
      resolvePromise({ server, origin: `http://127.0.0.1:${server.address().port}` });
    });
  });
}

test('fetchApiSpec guarda swagger y contratos en el snapshot', async () => {
  const { server, origin } = await startFakeApi({
    '/swagger/v1/swagger.json': SWAGGER,
    '/contracts': CONTRACTS_INDEX,
    '/contracts/persona.json': PERSONA_CONTRACT,
  });
  const outputDir = join(tmpdir(), `api-spec-test-${process.pid}-${Date.now()}`);

  try {
    const summary = await fetchApiSpec({
      origin,
      swaggerPath: '/swagger/v1/swagger.json',
      contractsPath: '/contracts',
      outputDir,
    });

    assert.equal(summary.contractCount, 1);
    assert.deepEqual(JSON.parse(readFileSync(join(outputDir, 'swagger.json'), 'utf-8')), SWAGGER);
    assert.deepEqual(
      JSON.parse(readFileSync(join(outputDir, 'contracts', 'index.json'), 'utf-8')),
      CONTRACTS_INDEX
    );
    assert.deepEqual(
      JSON.parse(readFileSync(join(outputDir, 'contracts', 'persona.json'), 'utf-8')),
      PERSONA_CONTRACT
    );
  } finally {
    server.close();
    rmSync(outputDir, { recursive: true, force: true });
  }
});

test('fetchApiSpec reporta la etapa cuando el indice de contratos es invalido', async () => {
  const { server, origin } = await startFakeApi({
    '/swagger/v1/swagger.json': SWAGGER,
    '/contracts': { not: 'an array' },
  });
  const outputDir = join(tmpdir(), `api-spec-test-invalid-${process.pid}-${Date.now()}`);

  try {
    await assert.rejects(
      fetchApiSpec({
        origin,
        swaggerPath: '/swagger/v1/swagger.json',
        contractsPath: '/contracts',
        outputDir,
      }),
      error => {
        assert.equal(error.stage, 'downloading contracts index');
        assert.match(error.message, /must return an array/);
        return true;
      }
    );
  } finally {
    server.close();
    rmSync(outputDir, { recursive: true, force: true });
  }
});
