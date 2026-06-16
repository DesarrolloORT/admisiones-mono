import { vi } from 'vitest';

vi.useRealTimers();

import {
  HttpContext,
  HttpErrorResponse,
  HttpHandlerFn,
  HttpHeaders,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { Injector, runInInjectionContext } from '@angular/core';
import { CacheService, CacheUtils, LoaderService } from '@desarrolloort/ngx-utils';
import { firstValueFrom, of, throwError } from 'rxjs';

import { CAPTCHA_ACTION, CAPTCHA_HEADER, CaptchaTokenService } from '../services/captcha-token';
import { TelemetryService } from '../services/telemetry';
import { CACHING_ENABLED, httpInterceptor } from './http';

type CacheServiceMock = {
  get: ReturnType<typeof vi.fn>;
  set: ReturnType<typeof vi.fn>;
};
type LoaderServiceMock = {
  show: ReturnType<typeof vi.fn>;
  hide: ReturnType<typeof vi.fn>;
};
type CaptchaTokenServiceMock = {
  execute: ReturnType<typeof vi.fn>;
};
type TelemetryServiceMock = {
  addHttpHeaders: ReturnType<typeof vi.fn>;
  startHttpRequest: ReturnType<typeof vi.fn>;
  trackHttpResponse: ReturnType<typeof vi.fn>;
  trackHttpError: ReturnType<typeof vi.fn>;
  trackCacheHit: ReturnType<typeof vi.fn>;
};

describe('httpInterceptor', () => {
  let mockCacheService: CacheServiceMock;
  let mockCaptcha: CaptchaTokenServiceMock;
  let mockLoader: LoaderServiceMock;
  let mockTelemetry: TelemetryServiceMock;
  let injector: Injector;

  beforeEach(() => {
    mockCacheService = {
      get: vi.fn(),
      set: vi.fn(),
    };
    mockCaptcha = {
      execute: vi.fn((action: string) => of(`${action}-captcha-token`)),
    };
    mockLoader = {
      show: vi.fn(),
      hide: vi.fn(),
    };
    mockTelemetry = {
      addHttpHeaders: vi.fn((req: HttpRequest<unknown>) =>
        req.clone({ setHeaders: { 'x-correlation-id': 'correlation-123' } })
      ),
      startHttpRequest: vi.fn(() => 100),
      trackHttpResponse: vi.fn(),
      trackHttpError: vi.fn(),
      trackCacheHit: vi.fn(),
    };

    // Stub out CacheUtils
    vi.spyOn(CacheUtils, 'createCacheKey').mockReturnValue('cache-key');
    vi.spyOn(CacheUtils, 'canCacheRequest').mockReturnValue(true);

    injector = Injector.create({
      providers: [
        { provide: CacheService, useValue: mockCacheService as unknown as CacheService },
        { provide: CaptchaTokenService, useValue: mockCaptcha as unknown as CaptchaTokenService },
        { provide: LoaderService, useValue: mockLoader as unknown as LoaderService },
        { provide: TelemetryService, useValue: mockTelemetry as unknown as TelemetryService },
      ],
    });
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  function invoke(req: HttpRequest<unknown>, next: HttpHandlerFn) {
    // wrap call in Angular's DI context
    return runInInjectionContext(injector, () => httpInterceptor(req, next));
  }

  it('returns cached response immediately when caching is enabled and cache hits', async () => {
    const cached = new HttpResponse({ body: { foo: 'bar' } });
    mockCacheService.get.mockReturnValue(cached);

    const req = new HttpRequest('GET', '/test', null, {
      context: new HttpContext().set(CACHING_ENABLED, true),
    });
    const next = vi.fn();

    const response = await firstValueFrom(invoke(req, next));

    expect(response).toBe(cached);
    expect(mockCacheService.get).toHaveBeenCalledWith('cache-key');
    expect(mockTelemetry.trackCacheHit).toHaveBeenCalledWith(req);
    expect(next).not.toHaveBeenCalled();
  });

  it('sets headers and shows/hides loader for a normal GET', async () => {
    mockCacheService.get.mockReturnValue(undefined);
    const resp = new HttpResponse({ status: 200, body: { ok: true } });
    const next = vi.fn().mockReturnValue(of(resp));

    const req = new HttpRequest('GET', '/api/data', null, {
      context: new HttpContext().set(CACHING_ENABLED, false),
      headers: new HttpHeaders({ 'X-Flow-Id': 'flow-123' }),
    });

    const result = await firstValueFrom(invoke(req, next));

    // loader
    expect(mockLoader.show).toHaveBeenCalled();
    expect(mockLoader.hide).toHaveBeenCalled();

    // headers
    const intercepted = next.mock.calls[0][0] as HttpRequest<unknown>;
    expect(intercepted.headers.get('Content-Type')).toBe('application/json');
    expect(intercepted.headers.get('X-Flow-Id')).toBe('flow-123');
    expect(intercepted.headers.get('authorization')).toBe('Basic Og==');
    expect(intercepted.headers.get('x-correlation-id')).toBe('correlation-123');
    expect(mockTelemetry.addHttpHeaders).toHaveBeenCalledWith(
      expect.objectContaining({
        url: req.url,
      })
    );
    expect(mockTelemetry.startHttpRequest).toHaveBeenCalledWith(intercepted);
    expect(mockTelemetry.trackHttpResponse).toHaveBeenCalledWith(intercepted, resp, 100);

    // response unchanged
    expect(result).toBe(resp);
  });

  it('adds a captcha token header when the request declares a captcha action', async () => {
    mockCacheService.get.mockReturnValue(undefined);
    const resp = new HttpResponse({ status: 200, body: { ok: true } });
    const next = vi.fn().mockReturnValue(of(resp));

    const req = new HttpRequest(
      'POST',
      '/Auth/Login',
      { user: 'ana' },
      {
        context: new HttpContext().set(CACHING_ENABLED, false).set(CAPTCHA_ACTION, 'login'),
      }
    );

    await firstValueFrom(invoke(req, next));

    const intercepted = next.mock.calls[0][0] as HttpRequest<unknown>;
    expect(mockCaptcha.execute).toHaveBeenCalledWith('login');
    expect(intercepted.headers.get(CAPTCHA_HEADER)).toBe('login-captcha-token');
  });

  it('stringifies POST/PUT bodies when responseType is json', async () => {
    mockCacheService.get.mockReturnValue(undefined);
    const resp = new HttpResponse({ status: 201, body: { created: true } });
    const next = vi.fn().mockReturnValue(of(resp));

    const payload = { a: 1 };
    const req = new HttpRequest('POST', '/api/create', payload, {
      responseType: 'json',
      context: new HttpContext().set(CACHING_ENABLED, false),
    });

    const result = await firstValueFrom(invoke(req, next));
    const passed = next.mock.calls[0][0] as HttpRequest<unknown>;

    expect(passed.body).toBe(JSON.stringify(payload));
    expect(result).toBe(resp);
  });

  it('caches successful responses when caching is enabled', async () => {
    mockCacheService.get.mockReturnValue(undefined);
    const resp = new HttpResponse({ status: 200, body: { cached: true } });
    const next = vi.fn().mockReturnValue(of(resp));

    const req = new HttpRequest('GET', '/api/cache-me', null, {
      context: new HttpContext().set(CACHING_ENABLED, true),
    });

    const result = await firstValueFrom(invoke(req, next));

    expect(result).toBe(resp);
    expect(CacheUtils.canCacheRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        url: req.urlWithParams,
        method: req.method,
      })
    );
    expect(mockCacheService.set).toHaveBeenCalledWith('cache-key', resp, 300000);
  });

  it('on error hides loader and rethrows', async () => {
    mockCacheService.get.mockReturnValue(undefined);
    const httpErr = new HttpErrorResponse({ status: 500, statusText: 'Server Error' });
    const next = vi.fn().mockReturnValue(throwError(() => httpErr));

    const req = new HttpRequest('GET', '/api/fail', null, {
      context: new HttpContext().set(CACHING_ENABLED, false),
    });

    await expect(firstValueFrom(invoke(req, next))).rejects.toBe(httpErr);
    expect(mockLoader.hide).toHaveBeenCalled();
    expect(mockTelemetry.trackHttpError).toHaveBeenCalledWith(
      expect.any(HttpRequest),
      httpErr,
      100
    );
  });
});
