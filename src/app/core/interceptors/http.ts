import {
  HttpErrorResponse,
  HttpEvent,
  HttpInterceptorFn,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { LoaderService } from '@desarrolloort/ngx-utils';
import { Observable, of, throwError } from 'rxjs';
import { catchError, finalize, map, shareReplay, switchMap, tap } from 'rxjs/operators';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';
import { SHOW_GLOBAL_LOADER } from 'src/app/shared/api/core/api-http-client';

import { CAPTCHA_ACTION, CAPTCHA_HEADER, CaptchaTokenService } from '../services/captcha-token';
import { TelemetryService } from '../services/telemetry';

export const httpInterceptor: HttpInterceptorFn = (request, next) => {
  const services = {
    loader: inject(LoaderService),
    telemetry: inject(TelemetryService),
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

  const sendRequest = (req: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
    const telemetryStartedAt = services.telemetry.startHttpRequest(req);

    return next(req).pipe(
      tap({
        next: event => {
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

  const showLoader = request.context.get(SHOW_GLOBAL_LOADER);
  if (showLoader) {
    services.loader.show();
  }
  return addCaptchaHeader(request).pipe(
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

      return getSharedRefreshRequest(authSession).pipe(
        switchMap(() =>
          next(request).pipe(
            catchError(retryError => {
              if (retryError instanceof HttpErrorResponse && retryError.status === 401) {
                authSession.clearSession();
              }
              return throwError(() => retryError);
            })
          )
        )
      );
    })
  );
};
