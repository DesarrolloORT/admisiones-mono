import {
  HttpContextToken,
  HttpErrorResponse,
  HttpEvent,
  HttpHeaders,
  HttpInterceptorFn,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { CacheService, CacheUtils, LoaderService } from '@desarrolloort/ngx-utils';
import { asyncScheduler, of, throwError } from 'rxjs';
import { catchError, finalize, observeOn, tap } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

import { ErrorHandling } from '../services/error-handling';

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

export const httpInterceptor: HttpInterceptorFn = (request, next) => {
  const services = {
    cacheHandler: inject(CacheService),
    loader: inject(LoaderService),
    errorHandler: inject(ErrorHandling),
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
    return req.clone({ headers: new HttpHeaders(DEFAULT_HEADERS) });
  };

  const processRequest = (req: HttpRequest<unknown>): HttpRequest<unknown> => {
    const { method, body, responseType } = req;

    if ((method === 'PUT' || method === 'POST') && body && responseType === 'json') {
      return req.clone({ body: JSON.stringify(body) });
    }
    return req;
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
    return of(cachedResponse as HttpEvent<unknown>).pipe(observeOn(asyncScheduler));
  }

  //* Process request
  handleLoader(request.url);
  request = processRequest(setHeaders(request));

  return next(request).pipe(
    finalize(() => services.loader.hide()),
    tap(handleResponse),
    catchError((error: HttpErrorResponse) => {
      services.errorHandler.handleErrorInUI(error);
      return throwError(() => error);
    })
  );
};
