import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  operationResultInterceptor,
  ortApiErrorInterceptor,
  provideOrtApiErrorHandling,
} from '@desarrolloort/ngx-utils';
import { environment } from 'src/environments/environment';

import { CAPTCHA_ACTION } from '../../../core/services/captcha-token';
import { defineEndpoint } from './api-endpoint';
import { ApiHttpClient, SHOW_GLOBAL_LOADER } from './api-http-client';

describe('ApiHttpClient', () => {
  let api: ApiHttpClient;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([ortApiErrorInterceptor, operationResultInterceptor])),
        provideHttpClientTesting(),
        ...provideOrtApiErrorHandling({ config: { logErrors: false } }),
      ],
    });

    api = TestBed.inject(ApiHttpClient);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  it('should execute an endpoint with body, path params, query params and credentials', () => {
    const endpoint = defineEndpoint<{
      pathParams: { id: string };
      queryParams: { page: number; search?: string | null };
      request: { nombre: string };
      response: { ok: boolean };
    }>({
      operationId: 'ActualizarPersona',
      method: 'POST',
      path: '/personas/{id}',
    });

    api
      .request(endpoint, {
        pathParams: { id: 'CI 123' },
        queryParams: { page: 2, search: null },
        body: { nombre: 'Ana' },
        headers: { 'X-Flow-Id': 'flow-123', 'X-Skip-Empty': null },
        withCredentials: true,
      })
      .subscribe(response => {
        expect(response.ok).toBe(true);
      });

    const expectedUrl = new URL('/personas/CI%20123', environment.API_URL).toString();
    const request = httpController.expectOne(
      req => req.url === expectedUrl && req.params.get('page') === '2' && !req.params.has('search')
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual({ nombre: 'Ana' });
    expect(request.request.headers.get('X-Flow-Id')).toBe('flow-123');
    expect(request.request.headers.has('X-Skip-Empty')).toBe(false);

    request.flush({ ok: true });
  });

  it('should attach captcha action to request context when requested', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: { documento: string };
      response: { ok: boolean };
    }>({
      operationId: 'Login',
      method: 'POST',
      path: '/auth/login',
    });

    api
      .request(endpoint, {
        body: { documento: '12345678' },
        captchaAction: ' login ',
      })
      .subscribe(response => {
        expect(response.ok).toBe(true);
      });

    const request = httpController.expectOne(
      new URL('/auth/login', environment.API_URL).toString()
    );

    expect(request.request.context.get(CAPTCHA_ACTION)).toBe('login');

    request.flush({ ok: true });
  });

  it('should enable the global loader when requested', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { ok: boolean };
    }>({
      operationId: 'GuardarPersona',
      method: 'POST',
      path: '/persona',
    });

    api.request(endpoint, { showLoader: true }).subscribe();

    const request = httpController.expectOne(new URL('/persona', environment.API_URL).toString());

    expect(request.request.context.get(SHOW_GLOBAL_LOADER)).toBe(true);
    request.flush({ ok: true });
  });

  it('should append repeated query params for array values', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: { estado: string[] };
      request: never;
      response: string[];
    }>({
      operationId: 'BuscarEstados',
      method: 'GET',
      path: '/estados',
    });

    api.request(endpoint, { queryParams: { estado: ['activo', 'pendiente'] } }).subscribe();

    const expectedUrl = new URL('/estados', environment.API_URL).toString();
    const request = httpController.expectOne(req => req.url === expectedUrl);

    expect(request.request.params.getAll('estado')).toEqual(['activo', 'pendiente']);

    request.flush([]);
  });

  it('should request binary GET responses as blobs', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: Blob;
    }>({
      operationId: 'ObtenerFoto',
      method: 'GET',
      path: '/persona/foto',
    });
    const response = new Blob(['photo'], { type: 'image/png' });

    api.request(endpoint, { cache: false, responseType: 'blob' }).subscribe(blob => {
      expect(blob.size).toBe(response.size);
      expect(blob.type).toBe('image/png');
    });

    const request = httpController.expectOne(
      new URL('/persona/foto', environment.API_URL).toString()
    );
    expect(request.request.responseType).toBe('blob');
    request.flush(response);
  });
  it('should cache GET endpoints without params by default', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { data: string[] };
    }>({
      operationId: 'ListarPaises',
      method: 'GET',
      path: '/paises',
    });
    const expectedUrl = new URL('/paises', environment.API_URL).toString();
    const responses: string[][] = [];

    api.list(endpoint).subscribe(data => responses.push(data));
    api.list(endpoint).subscribe(data => responses.push(data));

    const request = httpController.expectOne(expectedUrl);
    request.flush({ success: true, httpCode: 200, data: ['Uruguay'] });

    api.list(endpoint).subscribe(data => responses.push(data));

    httpController.expectNone(expectedUrl);
    expect(responses).toEqual([['Uruguay'], ['Uruguay'], ['Uruguay']]);
  });

  it('should not cache GET endpoints with query params by default', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: { estado: string };
      request: never;
      response: string[];
    }>({
      operationId: 'BuscarEstados',
      method: 'GET',
      path: '/estados',
    });
    const expectedUrl = new URL('/estados', environment.API_URL).toString();

    api.request(endpoint, { queryParams: { estado: 'activo' } }).subscribe();

    const firstRequest = httpController.expectOne(
      req => req.url === expectedUrl && req.params.get('estado') === 'activo'
    );
    firstRequest.flush([]);

    api.request(endpoint, { queryParams: { estado: 'activo' } }).subscribe();

    const secondRequest = httpController.expectOne(
      req => req.url === expectedUrl && req.params.get('estado') === 'activo'
    );
    secondRequest.flush([]);
  });

  it('should clear cached GET responses', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { data: string[] };
    }>({
      operationId: 'ListarPaises',
      method: 'GET',
      path: '/paises',
    });
    const expectedUrl = new URL('/paises', environment.API_URL).toString();
    const responses: string[][] = [];

    api.list(endpoint).subscribe(data => responses.push(data));

    const firstRequest = httpController.expectOne(expectedUrl);
    firstRequest.flush({ success: true, httpCode: 200, data: ['Uruguay'] });

    api.clearCache();
    api.list(endpoint).subscribe(data => responses.push(data));

    const secondRequest = httpController.expectOne(expectedUrl);
    secondRequest.flush({ success: true, httpCode: 200, data: ['Argentina'] });

    expect(responses).toEqual([['Uruguay'], ['Argentina']]);
  });

  it('should return data from operation result responses', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { data: { nombre: string }; message?: string | null };
    }>({
      operationId: 'ObtenerPersona',
      method: 'GET',
      path: '/persona',
    });

    api.data(endpoint).subscribe(data => {
      expect(data).toEqual({ nombre: 'Ana' });
    });

    const request = httpController.expectOne(new URL('/persona', environment.API_URL).toString());
    request.flush({ success: true, httpCode: 200, data: { nombre: 'Ana' }, message: null });
  });

  it('should return operation result data and message when requested explicitly', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { data: { nombre: string }; message?: string | null };
    }>({
      operationId: 'EvaluarDocumento',
      method: 'POST',
      path: '/registro/evaluar-documento',
    });

    api.requestWithMessage(endpoint).subscribe(response => {
      expect(response).toEqual({
        data: { nombre: 'Ana' },
        message: 'Documento ya registrado.',
      });
    });

    const request = httpController.expectOne(
      new URL('/registro/evaluar-documento', environment.API_URL).toString()
    );
    request.flush({
      success: true,
      httpCode: 200,
      data: { nombre: 'Ana' },
      message: 'Documento ya registrado.',
    });
  });

  it('should treat unsuccessful operation result responses as errors', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { data: null; errorCode?: string | null; message?: string | null };
    }>({
      operationId: 'CrearPersona',
      method: 'POST',
      path: '/persona',
    });

    api.request(endpoint).subscribe({
      error: error => {
        expect(error).toEqual(
          expect.objectContaining({
            status: 409,
            errorCode: 'USER_EXISTS',
            message: 'Ya existe un usuario.',
            isOperationResult: true,
          })
        );
      },
    });

    const request = httpController.expectOne(new URL('/persona', environment.API_URL).toString());
    request.flush({
      success: false,
      httpCode: 409,
      errorCode: 'USER_EXISTS',
      message: 'Ya existe un usuario.',
      data: null,
    });
  });

  it('should map operation result data as a list', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { data: Array<{ id: number; nombre: string }> | null };
    }>({
      operationId: 'ListarPersonas',
      method: 'GET',
      path: '/personas',
    });

    api
      .list(endpoint, item => ({ id: item.id, label: item.nombre }))
      .subscribe(data => {
        expect(data).toEqual([{ id: 1, label: 'Ana' }]);
      });

    const request = httpController.expectOne(new URL('/personas', environment.API_URL).toString());
    request.flush({ success: true, httpCode: 200, data: [{ id: 1, nombre: 'Ana' }] });
  });

  it('should treat single operation result data as a one item list', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { data: { id: number; nombre: string } };
    }>({
      operationId: 'ObtenerPersona',
      method: 'GET',
      path: '/persona',
    });

    api.list(endpoint).subscribe(data => {
      expect(data).toEqual([{ id: 1, nombre: 'Ana' }]);
    });

    const request = httpController.expectOne(new URL('/persona', environment.API_URL).toString());
    request.flush({ success: true, httpCode: 200, data: { id: 1, nombre: 'Ana' } });
  });

  it('should return an empty list when operation result data is null', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { data: Array<{ id: number }> | null };
    }>({
      operationId: 'ListarPersonas',
      method: 'GET',
      path: '/personas',
    });

    api.list(endpoint).subscribe(data => {
      expect(data).toEqual([]);
    });

    const request = httpController.expectOne(new URL('/personas', environment.API_URL).toString());
    request.flush({ success: true, httpCode: 200, data: null });
  });
});
