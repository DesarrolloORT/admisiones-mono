import { LocationStrategy, PathLocationStrategy } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
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

import { routes } from './app.routes';
import { httpInterceptor } from './core/interceptors/http';
import { AppApiErrorNotifier } from './core/services/api-error-notifier';
import { TelemetryService } from './core/services/telemetry';

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
      withInterceptors([httpInterceptor, ortApiErrorInterceptor, operationResultInterceptor])
    ),
    ...provideOrtApiErrorHandling({
      notifier: { provide: ApiErrorNotifier, useClass: AppApiErrorNotifier },
    }),
    { provide: LOCALE_ID, useValue: 'es-UY' },
    { provide: LocationStrategy, useClass: PathLocationStrategy },
  ],
};
