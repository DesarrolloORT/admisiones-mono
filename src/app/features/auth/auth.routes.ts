import { Routes } from '@angular/router';

import { Login } from './pages/login/login';
import { RecoverAccess } from './pages/recover-access/recover-access';
import { Register } from './pages/register/register';
import { SetPassword } from './pages/set-password/set-password';
import { TwoFactorValidationPage } from './pages/two-factor-validation/two-factor-validation';

export const routes: Routes = [
  { path: '', redirectTo: 'iniciar-sesion', pathMatch: 'full' },
  { path: 'iniciar-sesion', component: Login },
  { path: 'registro', component: Register },
  { path: 'crear-password', component: SetPassword },
  { path: 'recuperar-acceso', component: RecoverAccess },
  { path: 'verificar-codigo', component: TwoFactorValidationPage },
];
