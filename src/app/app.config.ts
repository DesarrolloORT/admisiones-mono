import { LocationStrategy, PathLocationStrategy } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  LOCALE_ID,
  provideAppInitializer,
  provideZonelessChangeDetection,
} from '@angular/core';
import { DateAdapter, MAT_DATE_FORMATS, MAT_DATE_LOCALE } from '@angular/material/core';
import { MatPaginatorIntl } from '@angular/material/paginator';
import {
  MAT_MOMENT_DATE_ADAPTER_OPTIONS,
  MomentDateAdapter,
} from '@angular/material-moment-adapter';
import {
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withRouterConfig,
} from '@angular/router';
import { DateUtils, PaginationUtils, UiUtils } from '@desarrolloort/ngx-utils';

import { routes } from './app.routes';
import { httpInterceptor } from './core/interceptors/http';

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
    provideHttpClient(withInterceptors([httpInterceptor])),
    { provide: LOCALE_ID, useValue: 'es-UY' },
    { provide: MAT_DATE_LOCALE, useValue: 'es-UY' },
    { provide: LocationStrategy, useClass: PathLocationStrategy },
    { provide: MatPaginatorIntl, useValue: PaginationUtils.createPaginatorIntl() },
    { provide: MAT_DATE_FORMATS, useValue: DateUtils.getLocalizedDateFormats() },
    {
      provide: DateAdapter,
      useClass: MomentDateAdapter,
      deps: [MAT_DATE_LOCALE, MAT_MOMENT_DATE_ADAPTER_OPTIONS],
    },
  ],
};
