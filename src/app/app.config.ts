import { LocationStrategy, PathLocationStrategy, registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeEsUy from '@angular/common/locales/es-UY';
import {
  ApplicationConfig,
  importProvidersFrom,
  inject,
  LOCALE_ID,
  provideAppInitializer,
  provideZonelessChangeDetection,
} from '@angular/core';
import {
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withRouterConfig,
} from '@angular/router';
import {
  ApiErrorNotifier,
  operationResultInterceptor,
  ortApiErrorInterceptor,
  provideOrtApiErrorHandling,
  UiUtils,
} from '@desarrolloort/ngx-utils';
import { RECAPTCHA_V3_SITE_KEY, RecaptchaV3Module } from 'ng-recaptcha-2';
import { environment } from 'src/environments/environment';

import { routes } from './app.routes';
import { authRefreshInterceptor, httpInterceptor } from './core/interceptors/http';
import { AppApiErrorNotifier } from './core/services/api-error-notifier';
import { TelemetryService } from './core/services/telemetry';

registerLocaleData(localeEsUy);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withRouterConfig({ paramsInheritanceStrategy: 'always', onSameUrlNavigation: 'reload' }),
      withInMemoryScrolling({ scrollPositionRestoration: 'top', anchorScrolling: 'enabled' })
    ),
    provideAppInitializer(() => UiUtils.initializeMaterialSymbols()),
    provideAppInitializer(() => inject(TelemetryService).initialize()),
    provideHttpClient(
      withInterceptors([
        httpInterceptor,
        ortApiErrorInterceptor,
        authRefreshInterceptor,
        operationResultInterceptor,
      ])
    ),
    importProvidersFrom(RecaptchaV3Module),
    { provide: RECAPTCHA_V3_SITE_KEY, useValue: environment.RECAPTCHA_KEY },
    ...provideOrtApiErrorHandling({
      notifier: { provide: ApiErrorNotifier, useClass: AppApiErrorNotifier },
    }),
    { provide: LOCALE_ID, useValue: 'es-UY' },
    { provide: LocationStrategy, useClass: PathLocationStrategy },
  ],
};
