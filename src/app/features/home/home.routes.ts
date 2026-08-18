import { inject } from '@angular/core';
import { ResolveFn, Routes } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { HomeLayout } from './layouts/home-layout/home-layout';
import { HomeData } from './models/home-data';
import { ChangePassword } from './pages/change-password/change-password';
import { Home } from './pages/home/home';
import { PersonalData } from './pages/personal-data/personal-data';
import { HomeService } from './services/home';

export const homeResolver: ResolveFn<HomeData | null> = () => {
  const homeService = inject(HomeService);

  return homeService.getMyEnrollments().pipe(
    map(enrollments => ({ enrollments, scholarships: [] })),
    catchError(() => of(null))
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
