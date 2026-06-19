import { inject } from '@angular/core';
import { ResolveFn, Routes } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { HomeEndpoint } from './endpoints/home.endpoint';
import { HomeLayout } from './layouts/home-layout/home-layout';
import { HomeData } from './models/home-data';
import { ChangePassword } from './pages/change-password/change-password';
import { Home } from './pages/home/home';
import { PersonalData } from './pages/personal-data/personal-data';

export const homeResolver: ResolveFn<HomeData | null> = () => {
  const endpoint = inject(HomeEndpoint);

  return forkJoin({
    inscripciones: endpoint.getMisInscripciones(),
    becas: endpoint.getMisBecas(),
  }).pipe(catchError(() => of(null)));
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
