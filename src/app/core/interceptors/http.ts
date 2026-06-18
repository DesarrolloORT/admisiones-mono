import {
  HttpContextToken,
  HttpErrorResponse,
  HttpEvent,
  HttpInterceptorFn,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { CacheService, CacheUtils, LoaderService } from '@desarrolloort/ngx-utils';
import { asyncScheduler, Observable, of, throwError } from 'rxjs';
import { catchError, finalize, map, observeOn, shareReplay, switchMap, tap } from 'rxjs/operators';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';
import { SHOW_GLOBAL_LOADER } from 'src/app/shared/api/core/api-http-client';
import { environment } from 'src/environments/environment';

import { CAPTCHA_ACTION, CAPTCHA_HEADER, CaptchaTokenService } from '../services/captcha-token';
import { TelemetryService } from '../services/telemetry';

export const CACHING_ENABLED = new HttpContextToken<boolean>(() => environment.CACHING_ENABLED);

const DEFAULT_HEADERS = {
  'Content-Type': 'application/json',
  'X-Content-Type-Options': 'nosniff',
  'X-XSS-Protection': '1; mode=block',
  authorization: 'Basic Og==',
  castmanchecontrol: 'no-cache',
};

const LOGIN_URL_PATTERN = /\/login\/?$/i;

export const httpInterceptor: HttpInterceptorFn = (request, next) => {
  const services = {
    cacheHandler: inject(CacheService),
    loader: inject(LoaderService),
    telemetry: inject(TelemetryService),
  };

  const setHeaders = (req: HttpRequest<unknown>): HttpRequest<unknown> => {
    if (LOGIN_URL_PATTERN.test(req.url)) {
      return req;
    }

    return req.clone({ setHeaders: DEFAULT_HEADERS });
  };

  const processRequest = (req: HttpRequest<unknown>): HttpRequest<unknown> => {
    if (LOGIN_URL_PATTERN.test(req.url)) {
      return req;
    }

    const { method, body, responseType } = req;

    if ((method === 'PUT' || method === 'POST') && body && responseType === 'json') {
      return req.clone({ body: JSON.stringify(body) });
    }
    return req;
  };

  const addCaptchaHeader = (req: HttpRequest<unknown>): Observable<HttpRequest<unknown>> => {
    const captchaAction = req.context.get(CAPTCHA_ACTION);

    if (!captchaAction) {
      return of(req);
    }

    return inject(CaptchaTokenService)
      .execute(captchaAction)
      .pipe(
        map(token => {
          return req.clone({
            setHeaders: {
              [CAPTCHA_HEADER]: token,
            },
          });
        })
      );
  };

  const handleResponse = (req: HttpRequest<unknown>, event: HttpEvent<unknown>): void => {
    if (
      event instanceof HttpResponse &&
      CacheUtils.canCacheRequest(req) &&
      req.context.get(CACHING_ENABLED)
    ) {
      const cacheKey = CacheUtils.createCacheKey(req.urlWithParams, req.body);
      services.cacheHandler.set(cacheKey, event, 300000);
    }
  };

  const sendRequest = (req: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
    const telemetryStartedAt = services.telemetry.startHttpRequest(req);

    return next(req).pipe(
      tap({
        next: event => {
          handleResponse(req, event);

          if (event instanceof HttpResponse) {
            services.telemetry.trackHttpResponse(req, event, telemetryStartedAt);
          }
        },
        error: error => {
          services.telemetry.trackHttpError(req, error, telemetryStartedAt);
        },
      })
    );
  };

  //* Check cache first
  const cacheKey = CacheUtils.createCacheKey(request.urlWithParams, request.body);
  const cachedResponse = services.cacheHandler.get(cacheKey);

  if (request.context.get(CACHING_ENABLED) && cachedResponse) {
    services.telemetry.trackCacheHit(request);
    return of(cachedResponse as HttpEvent<unknown>).pipe(observeOn(asyncScheduler));
  }

  //* Process request
  const showLoader = request.context.get(SHOW_GLOBAL_LOADER);
  if (showLoader) {
    services.loader.show();
  }
  const processedRequest = processRequest(setHeaders(request));

  return addCaptchaHeader(processedRequest).pipe(
    switchMap(processedRequest => {
      const telemetryRequest = services.telemetry.addHttpHeaders(processedRequest);

      return sendRequest(telemetryRequest);
    }),
    finalize(() => {
      if (showLoader) {
        services.loader.hide();
      }
    })
  );
};

const AUTH_API_URL_PATTERN = /\/Auth\//i;
let activeRefreshRequest$: Observable<void> | null = null;

function shouldRefreshAccessToken(request: HttpRequest<unknown>, error: unknown): boolean {
  return (
    error instanceof HttpErrorResponse &&
    error.status === 401 &&
    request.withCredentials &&
    !AUTH_API_URL_PATTERN.test(request.url)
  );
}

function getSharedRefreshRequest(authSession: AuthSessionService): Observable<void> {
  if (activeRefreshRequest$) {
    return activeRefreshRequest$;
  }

  activeRefreshRequest$ = authSession.refreshAccessToken().pipe(
    catchError(error => {
      authSession.clearSession();
      return throwError(() => error);
    }),
    finalize(() => {
      activeRefreshRequest$ = null;
    }),
    shareReplay({ bufferSize: 1, refCount: false })
  );

  return activeRefreshRequest$;
}

export const authRefreshInterceptor: HttpInterceptorFn = (request, next) => {
  const authSession = inject(AuthSessionService);

  return next(request).pipe(
    catchError(error => {
      if (!shouldRefreshAccessToken(request, error)) {
        return throwError(() => error);
      }

      return getSharedRefreshRequest(authSession).pipe(switchMap(() => next(request)));
    })
  );
};
