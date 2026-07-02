import { vi } from 'vitest';

vi.useRealTimers();

import {
  HttpContext,
  HttpErrorResponse,
  HttpHandlerFn,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { Injector, runInInjectionContext } from '@angular/core';
import { LoaderService } from '@desarrolloort/ngx-utils';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';

import { AuthSessionService } from '../../features/auth/services/auth-session';
import { SHOW_GLOBAL_LOADER } from '../../shared/api/core/api-http-client';
import { CAPTCHA_ACTION, CAPTCHA_HEADER, CaptchaTokenService } from '../services/captcha-token';
import { TelemetryService } from '../services/telemetry';
import { authRefreshInterceptor, httpInterceptor } from './http';
type LoaderServiceMock = {
  show: ReturnType<typeof vi.fn>;
  hide: ReturnType<typeof vi.fn>;
};
type CaptchaTokenServiceMock = {
  execute: ReturnType<typeof vi.fn>;
};
type AuthSessionServiceMock = {
  clearSession: ReturnType<typeof vi.fn>;
  refreshAccessToken: ReturnType<typeof vi.fn>;
};
type TelemetryServiceMock = {
  addHttpHeaders: ReturnType<typeof vi.fn>;
  startHttpRequest: ReturnType<typeof vi.fn>;
  trackHttpResponse: ReturnType<typeof vi.fn>;
  trackHttpError: ReturnType<typeof vi.fn>;
};

describe('HTTP interceptors', () => {
  let mockCaptcha: CaptchaTokenServiceMock;
  let mockAuthSession: AuthSessionServiceMock;
  let mockLoader: LoaderServiceMock;
  let mockTelemetry: TelemetryServiceMock;
  let injector: Injector;

  beforeEach(() => {
    mockCaptcha = {
      execute: vi.fn((action: string) => of(`${action}-captcha-token`)),
    };
    mockAuthSession = {
      clearSession: vi.fn(),
      refreshAccessToken: vi.fn().mockReturnValue(of(undefined)),
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
    };

    injector = Injector.create({
      providers: [
        { provide: AuthSessionService, useValue: mockAuthSession as unknown as AuthSessionService },
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

  function invokeAuthRefresh(req: HttpRequest<unknown>, next: HttpHandlerFn) {
    return runInInjectionContext(injector, () => authRefreshInterceptor(req, next));
  }

  it('adds telemetry without changing application headers or body', async () => {
    const resp = new HttpResponse({ status: 200, body: { ok: true } });
    const next = vi.fn().mockReturnValue(of(resp));
    const body = { value: 1 };

    const req = new HttpRequest('POST', '/api/data', body);

    const result = await firstValueFrom(invoke(req, next));

    expect(mockLoader.show).not.toHaveBeenCalled();
    expect(mockLoader.hide).not.toHaveBeenCalled();

    const intercepted = next.mock.calls[0][0] as HttpRequest<unknown>;
    expect(intercepted.body).toBe(body);
    expect(intercepted.headers.has('Content-Type')).toBe(false);
    expect(intercepted.headers.has('authorization')).toBe(false);
    expect(intercepted.headers.has('X-Content-Type-Options')).toBe(false);
    expect(intercepted.headers.get('x-correlation-id')).toBe('correlation-123');
    expect(mockTelemetry.addHttpHeaders).toHaveBeenCalledWith(
      expect.objectContaining({
        url: req.url,
      })
    );
    expect(mockTelemetry.startHttpRequest).toHaveBeenCalledWith(intercepted);
    expect(mockTelemetry.trackHttpResponse).toHaveBeenCalledWith(intercepted, resp, 100);

    expect(result).toBe(resp);
  });

  it('shows and hides the loader when the request opts in', async () => {
    const response = new HttpResponse({ status: 200 });
    const next = vi.fn().mockReturnValue(of(response));
    const request = new HttpRequest('POST', '/Auth/Login', null, {
      context: new HttpContext().set(SHOW_GLOBAL_LOADER, true),
    });

    await firstValueFrom(invoke(request, next));

    expect(mockLoader.show).toHaveBeenCalledOnce();
    expect(mockLoader.hide).toHaveBeenCalledOnce();
  });

  it('adds a captcha token header when the request declares a captcha action', async () => {
    const resp = new HttpResponse({ status: 200, body: { ok: true } });
    const next = vi.fn().mockReturnValue(of(resp));

    const req = new HttpRequest(
      'POST',
      '/Auth/Login',
      { user: 'ana' },
      {
        context: new HttpContext().set(CAPTCHA_ACTION, 'login'),
      }
    );

    await firstValueFrom(invoke(req, next));

    const intercepted = next.mock.calls[0][0] as HttpRequest<unknown>;
    expect(mockCaptcha.execute).toHaveBeenCalledWith('login');
    expect(intercepted.headers.get(CAPTCHA_HEADER)).toBe('login-captcha-token');
  });

  it('on error hides loader and rethrows', async () => {
    const httpErr = new HttpErrorResponse({ status: 500, statusText: 'Server Error' });
    const next = vi.fn().mockReturnValue(throwError(() => httpErr));

    const req = new HttpRequest('GET', '/api/fail', null, {
      context: new HttpContext().set(SHOW_GLOBAL_LOADER, true),
    });

    await expect(firstValueFrom(invoke(req, next))).rejects.toBe(httpErr);
    expect(mockLoader.hide).toHaveBeenCalled();
    expect(mockTelemetry.trackHttpError).toHaveBeenCalledWith(
      expect.any(HttpRequest),
      httpErr,
      100
    );
  });

  it('refreshes the access token and retries a credentialed request once after 401', async () => {
    const unauthorized = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
    const resp = new HttpResponse({ status: 200, body: { ok: true } });
    const next = vi
      .fn()
      .mockReturnValueOnce(throwError(() => unauthorized))
      .mockReturnValueOnce(of(resp));

    const req = new HttpRequest('GET', '/api/private', null, {
      withCredentials: true,
    });

    const result = await firstValueFrom(invokeAuthRefresh(req, next));

    expect(result).toBe(resp);
    expect(mockAuthSession.refreshAccessToken).toHaveBeenCalledTimes(1);
    expect(next).toHaveBeenCalledTimes(2);
    expect((next.mock.calls[1][0] as HttpRequest<unknown>).url).toBe('/api/private');
    expect(mockAuthSession.clearSession).not.toHaveBeenCalled();
  });

  it('shares one refresh call across concurrent 401 responses', async () => {
    const unauthorized = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
    const refresh$ = new Subject<void>();
    const firstResp = new HttpResponse({ status: 200, body: { id: 1 } });
    const secondResp = new HttpResponse({ status: 200, body: { id: 2 } });
    const next = vi
      .fn()
      .mockReturnValueOnce(throwError(() => unauthorized))
      .mockReturnValueOnce(throwError(() => unauthorized))
      .mockReturnValueOnce(of(firstResp))
      .mockReturnValueOnce(of(secondResp));

    mockAuthSession.refreshAccessToken.mockReturnValue(refresh$.asObservable());

    const firstReq = new HttpRequest('GET', '/api/private/1', null, {
      withCredentials: true,
    });
    const secondReq = new HttpRequest('GET', '/api/private/2', null, {
      withCredentials: true,
    });

    const firstResult = firstValueFrom(invokeAuthRefresh(firstReq, next));
    const secondResult = firstValueFrom(invokeAuthRefresh(secondReq, next));

    expect(mockAuthSession.refreshAccessToken).toHaveBeenCalledTimes(1);

    refresh$.next();
    refresh$.complete();

    await expect(firstResult).resolves.toBe(firstResp);
    await expect(secondResult).resolves.toBe(secondResp);
    expect(next).toHaveBeenCalledTimes(4);
  });

  it('does not refresh non-credentialed or auth endpoint 401 responses', async () => {
    const unauthorized = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });

    const publicNext = vi.fn().mockReturnValue(throwError(() => unauthorized));
    const publicReq = new HttpRequest('GET', '/api/public', null, {});

    await expect(firstValueFrom(invokeAuthRefresh(publicReq, publicNext))).rejects.toBe(
      unauthorized
    );

    const loginNext = vi.fn().mockReturnValue(throwError(() => unauthorized));
    const loginReq = new HttpRequest(
      'POST',
      '/Auth/Login',
      { user: 'ana' },
      {
        withCredentials: true,
      }
    );

    await expect(firstValueFrom(invokeAuthRefresh(loginReq, loginNext))).rejects.toBe(unauthorized);
    expect(mockAuthSession.refreshAccessToken).not.toHaveBeenCalled();
  });

  it('clears the local session when refresh fails', async () => {
    const unauthorized = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
    const refreshError = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
    const next = vi.fn().mockReturnValue(throwError(() => unauthorized));

    mockAuthSession.refreshAccessToken.mockReturnValue(throwError(() => refreshError));

    const req = new HttpRequest('GET', '/api/private', null, {
      withCredentials: true,
    });

    await expect(firstValueFrom(invokeAuthRefresh(req, next))).rejects.toBe(refreshError);
    expect(mockAuthSession.clearSession).toHaveBeenCalledTimes(1);
    expect(next).toHaveBeenCalledTimes(1);
  });

  it('clears the session and does not refresh again after a second 401', async () => {
    const unauthorized = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
    const next = vi.fn().mockReturnValue(throwError(() => unauthorized));
    const request = new HttpRequest('GET', '/api/private', null, {
      withCredentials: true,
    });

    await expect(firstValueFrom(invokeAuthRefresh(request, next))).rejects.toBe(unauthorized);

    expect(mockAuthSession.refreshAccessToken).toHaveBeenCalledOnce();
    expect(mockAuthSession.clearSession).toHaveBeenCalledOnce();
    expect(next).toHaveBeenCalledTimes(2);
  });

  it('clears the local session once when a shared refresh fails', async () => {
    const unauthorized = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
    const refreshError = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
    const refresh$ = new Subject<void>();
    const next = vi.fn().mockReturnValue(throwError(() => unauthorized));

    mockAuthSession.refreshAccessToken.mockReturnValue(refresh$.asObservable());

    const firstRequest = new HttpRequest('GET', '/api/private/1', null, {
      withCredentials: true,
    });
    const secondRequest = new HttpRequest('GET', '/api/private/2', null, {
      withCredentials: true,
    });

    const firstResult = firstValueFrom(invokeAuthRefresh(firstRequest, next));
    const secondResult = firstValueFrom(invokeAuthRefresh(secondRequest, next));

    refresh$.error(refreshError);

    await expect(firstResult).rejects.toBe(refreshError);
    await expect(secondResult).rejects.toBe(refreshError);
    expect(mockAuthSession.refreshAccessToken).toHaveBeenCalledTimes(1);
    expect(mockAuthSession.clearSession).toHaveBeenCalledTimes(1);
  });
});
