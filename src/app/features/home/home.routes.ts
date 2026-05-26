import { Routes } from '@angular/router';

import { HomeLayout } from './layouts/home-layout/home-layout';
import { ChangePassword } from './pages/change-password/change-password';
import { Home } from './pages/home/home';
import { PersonalData } from './pages/personal-data/personal-data';

export const routes: Routes = [
  {
    path: '',
    component: HomeLayout,
    children: [
      { path: '', component: Home },
      { path: 'datos-personales', component: PersonalData },
      { path: 'cambiar-contrasena', component: ChangePassword },
    ],
  },
];
