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
import { catchError, finalize, map, observeOn, switchMap, tap } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

import { CAPTCHA_ACTION, CAPTCHA_HEADER, CaptchaTokenService } from '../services/captcha-token';
import { TelemetryService } from '../services/telemetry';

export const CACHING_ENABLED = new HttpContextToken<boolean>(() => environment.CACHING_ENABLED);
const IGNORED_LOADER_URLS: string[] = [
  // * TODO: add URLs to ignore
];

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

  const handleLoader = (url: string): void => {
    const { loader } = services;
    const isURLToIgnore = IGNORED_LOADER_URLS.some(pattern => url.includes(pattern));
    if (isURLToIgnore) {
      loader.hide();
    } else {
      loader.show();
    }
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

  const handleResponse = (event: HttpEvent<unknown>): void => {
    if (
      event instanceof HttpResponse &&
      CacheUtils.canCacheRequest(request) &&
      request.context.get(CACHING_ENABLED)
    ) {
      const cacheKey = CacheUtils.createCacheKey(request.urlWithParams, request.body);
      services.cacheHandler.set(cacheKey, event, 300000);
    }
  };

  //* Check cache first
  const cacheKey = CacheUtils.createCacheKey(request.urlWithParams, request.body);
  const cachedResponse = services.cacheHandler.get(cacheKey);

  if (request.context.get(CACHING_ENABLED) && cachedResponse) {
    services.telemetry.trackCacheHit(request);
    return of(cachedResponse as HttpEvent<unknown>).pipe(observeOn(asyncScheduler));
  }

  //* Process request
  handleLoader(request.url);
  const processedRequest = processRequest(setHeaders(request));

  return addCaptchaHeader(processedRequest).pipe(
    switchMap(processedRequest => {
      request = services.telemetry.addHttpHeaders(processedRequest);
      const telemetryStartedAt = services.telemetry.startHttpRequest(request);

      return next(request).pipe(
        tap({
          next: event => {
            handleResponse(event);

            if (event instanceof HttpResponse) {
              services.telemetry.trackHttpResponse(request, event, telemetryStartedAt);
            }
          },
          error: (error: HttpErrorResponse) => {
            services.telemetry.trackHttpError(request, error, telemetryStartedAt);
          },
        })
      );
    }),
    catchError(error => {
      return throwError(() => error);
    }),
    finalize(() => services.loader.hide())
  );
};
