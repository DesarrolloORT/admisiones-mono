import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  operationResultInterceptor,
  ortApiErrorInterceptor,
  SUPPRESS_GLOBAL_ERROR,
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
        provideHttpClient(
          withXhr(),
          withInterceptors([ortApiErrorInterceptor, operationResultInterceptor])
        ),
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
      request: { name: string };
      response: { ok: boolean };
    }>({
      operationId: 'UpdatePerson',
      method: 'POST',
      path: '/people/{id}',
    });

    api
      .request(endpoint, {
        pathParams: { id: 'CI 123' },
        queryParams: { page: 2, search: null },
        body: { name: 'Ana' },
        headers: { 'X-Flow-Id': 'flow-123', 'X-Skip-Empty': null },
        withCredentials: true,
      })
      .subscribe(response => {
        expect(response.ok).toBe(true);
      });

    const expectedUrl = new URL('/people/CI%20123', environment.API_URL).toString();
    const request = httpController.expectOne(
      req => req.url === expectedUrl && req.params.get('page') === '2' && !req.params.has('search')
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual({ name: 'Ana' });
    expect(request.request.headers.get('X-Flow-Id')).toBe('flow-123');
    expect(request.request.headers.has('X-Skip-Empty')).toBe(false);

    request.flush({ ok: true });
  });

  it('should suppress global API errors by default', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { ok: boolean };
    }>({
      operationId: 'GetPerson',
      method: 'GET',
      path: '/person',
    });

    api.request(endpoint).subscribe();

    const request = httpController.expectOne(new URL('/person', environment.API_URL).toString());

    expect(request.request.context.get(SUPPRESS_GLOBAL_ERROR)).toBe(true);
    request.flush({ ok: true });
  });

  it('should attach captcha action to request context when requested', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: { documentNumber: string };
      response: { ok: boolean };
    }>({
      operationId: 'Login',
      method: 'POST',
      path: '/auth/login',
    });

    api
      .request(endpoint, {
        body: { documentNumber: '12345678' },
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
      operationId: 'SavePerson',
      method: 'POST',
      path: '/person',
    });

    api.request(endpoint, { showLoader: true }).subscribe();

    const request = httpController.expectOne(new URL('/person', environment.API_URL).toString());

    expect(request.request.context.get(SHOW_GLOBAL_LOADER)).toBe(true);
    request.flush({ ok: true });
  });

  it('should append repeated query params for array values', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: { status: string[] };
      request: never;
      response: string[];
    }>({
      operationId: 'SearchStatuses',
      method: 'GET',
      path: '/statuses',
    });

    api.request(endpoint, { queryParams: { status: ['active', 'pending'] } }).subscribe();

    const expectedUrl = new URL('/statuses', environment.API_URL).toString();
    const request = httpController.expectOne(req => req.url === expectedUrl);

    expect(request.request.params.getAll('status')).toEqual(['active', 'pending']);

    request.flush([]);
  });

  it('should request binary GET responses as blobs', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: Blob;
    }>({
      operationId: 'GetPhoto',
      method: 'GET',
      path: '/person/photo',
    });
    const response = new Blob(['photo'], { type: 'image/png' });

    api.request(endpoint, { responseType: 'blob' }).subscribe(blob => {
      expect(blob.size).toBe(response.size);
      expect(blob.type).toBe('image/png');
    });

    const request = httpController.expectOne(
      new URL('/person/photo', environment.API_URL).toString()
    );
    expect(request.request.responseType).toBe('blob');
    request.flush(response);
  });
  // El cliente no cachea: cada GET va a la red y devuelve el dato vigente. Antes existía un
  // `Map` de respuestas, pero solo se activaba para endpoints sin `requiresAuth` y todos los
  // generados que la app consume son autenticados, así que nunca acertaba en producción.
  it('should re-request GET endpoints instead of caching them', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { success?: boolean; httpCode?: number; data: string[] };
    }>({
      operationId: 'ListCountries',
      method: 'GET',
      path: '/countries',
    });
    const expectedUrl = new URL('/countries', environment.API_URL).toString();
    const responses: string[][] = [];

    api.list(endpoint).subscribe(data => responses.push(data));
    httpController
      .expectOne(expectedUrl)
      .flush({ success: true, httpCode: 200, data: ['Uruguay'] });

    api.list(endpoint).subscribe(data => responses.push(data));
    httpController
      .expectOne(expectedUrl)
      .flush({ success: true, httpCode: 200, data: ['Argentina'] });

    expect(responses).toEqual([['Uruguay'], ['Argentina']]);
  });

  it('should return data from operation result responses', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: {
        success?: boolean;
        httpCode?: number;
        data: { name: string; accepted: boolean; acceptanceDate: string | null };
        message?: string | null;
      };
    }>({
      operationId: 'GetPerson',
      method: 'GET',
      path: '/person',
    });

    api.data(endpoint).subscribe(data => {
      expect(data).toEqual({
        name: 'Ana',
        accepted: true,
        acceptanceDate: '2026-06-01',
      });
    });

    const request = httpController.expectOne(new URL('/person', environment.API_URL).toString());
    request.flush({
      success: true,
      httpCode: 200,
      data: { name: 'Ana', accepted: true, acceptanceDate: '2026-06-01' },
      message: null,
    });
  });

  it('should return operation result data and message when requested explicitly', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: {
        success?: boolean;
        httpCode?: number;
        data: { name: string };
        message?: string | null;
      };
    }>({
      operationId: 'EvaluateDocument',
      method: 'POST',
      path: '/registration/evaluate-document',
    });

    api.requestWithMessage(endpoint).subscribe(response => {
      expect(response).toEqual({
        data: { name: 'Ana' },
        message: 'Documento ya registrado.',
      });
    });

    const request = httpController.expectOne(
      new URL('/registration/evaluate-document', environment.API_URL).toString()
    );
    request.flush({
      success: true,
      httpCode: 200,
      data: { name: 'Ana' },
      message: 'Documento ya registrado.',
    });
  });

  it('should treat unsuccessful operation result responses as errors', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: {
        success?: boolean;
        httpCode?: number;
        data: null;
        errorCode?: string | null;
        message?: string | null;
      };
    }>({
      operationId: 'CreatePerson',
      method: 'POST',
      path: '/person',
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

    const request = httpController.expectOne(new URL('/person', environment.API_URL).toString());
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
      response: {
        success?: boolean;
        httpCode?: number;
        data: Array<{ id: number; name: string }> | null;
      };
    }>({
      operationId: 'ListPeople',
      method: 'GET',
      path: '/people',
    });

    api
      .list(endpoint, item => ({ id: item.id, label: item.name }))
      .subscribe(data => {
        expect(data).toEqual([{ id: 1, label: 'Ana' }]);
      });

    const request = httpController.expectOne(new URL('/people', environment.API_URL).toString());
    request.flush({ success: true, httpCode: 200, data: [{ id: 1, name: 'Ana' }] });
  });

  it('should treat single operation result data as a one item list', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { success?: boolean; httpCode?: number; data: { id: number; name: string } };
    }>({
      operationId: 'GetPerson',
      method: 'GET',
      path: '/person',
    });

    api.list(endpoint).subscribe(data => {
      expect(data).toEqual([{ id: 1, name: 'Ana' }]);
    });

    const request = httpController.expectOne(new URL('/person', environment.API_URL).toString());
    request.flush({ success: true, httpCode: 200, data: { id: 1, name: 'Ana' } });
  });

  it('should return an empty list when operation result data is null', () => {
    const endpoint = defineEndpoint<{
      pathParams: never;
      queryParams: never;
      request: never;
      response: { success?: boolean; httpCode?: number; data: Array<{ id: number }> | null };
    }>({
      operationId: 'ListPeople',
      method: 'GET',
      path: '/people',
    });

    api.list(endpoint).subscribe(data => {
      expect(data).toEqual([]);
    });

    const request = httpController.expectOne(new URL('/people', environment.API_URL).toString());
    request.flush({ success: true, httpCode: 200, data: null });
  });
});
