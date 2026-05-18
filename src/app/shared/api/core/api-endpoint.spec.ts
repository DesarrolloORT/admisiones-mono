import { defineEndpoint } from './api-endpoint';

describe('defineEndpoint', () => {
  it('should keep endpoint metadata unchanged at runtime', () => {
    const endpoint = defineEndpoint<{
      pathParams: { id: number };
      queryParams: never;
      request: never;
      response: { ok: boolean };
    }>({
      operationId: 'ObtenerPersona',
      method: 'GET',
      path: '/personas/{id}',
    });

    expect(endpoint).toEqual({
      operationId: 'ObtenerPersona',
      method: 'GET',
      path: '/personas/{id}',
    });
  });
});
