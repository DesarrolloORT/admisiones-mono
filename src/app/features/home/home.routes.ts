import { Routes } from '@angular/router';

import { ChangePassword } from './pages/change-password/change-password';
import { Home } from './pages/home/home';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'cambiar-contrasena', component: ChangePassword },
];

