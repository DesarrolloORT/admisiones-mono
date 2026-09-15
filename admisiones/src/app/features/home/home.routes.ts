import { inject } from '@angular/core';
import { ResolveFn, Routes } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { getApiErrorMessage } from 'src/app/shared/errors/api-error-message';

import { HomeApi } from './api/home.api';
import { HomeLayout } from './layouts/home-layout/home-layout';
import { DEFAULT_HOME_LOAD_ERROR, HomeResolved } from './models/home-data';
import { ChangePassword } from './pages/change-password/change-password';
import { Home } from './pages/home/home';
import { PersonalData } from './pages/personal-data/personal-data';

export const homeResolver: ResolveFn<HomeResolved> = () => {
  const homeApi = inject(HomeApi);

  return forkJoin({
    enrollments: homeApi.getMyEnrollments(),
    // Las becas no bloquean el dashboard: si fallan, la pantalla se dibuja con las
    // inscripciones y la seccion de becas queda vacia.
    scholarships: homeApi.getMyScholarships().pipe(catchError(() => of([]))),
  }).pipe(
    catchError((error: unknown) =>
      of({ loadError: getApiErrorMessage(error, DEFAULT_HOME_LOAD_ERROR) })
    )
  );
};

export const routes: Routes = [
  {
    path: '',
    component: HomeLayout,
    children: [
      {
        path: '',
        component: Home,
        resolve: { homeData: homeResolver },
        runGuardsAndResolvers: 'always',
      },
      { path: 'datos-personales', component: PersonalData },
      { path: 'cambiar-contrasena', component: ChangePassword },
    ],
  },
];
