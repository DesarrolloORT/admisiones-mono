import { Routes } from '@angular/router';

import {
  RECOVER_ACCESS_EMAIL_CONFIRMATION,
  REGISTER_EMAIL_CONFIRMATION,
  REGISTER_REQUEST_EMAIL_CONFIRMATION,
  TWO_FACTOR_EMAIL_CONFIRMATION,
} from './models/email-confirmation';
import { EmailConfirmation } from './pages/email-confirmation/email-confirmation';
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
  {
    path: 'confirmacion-correo/registro',
    component: EmailConfirmation,
    data: { confirmation: REGISTER_EMAIL_CONFIRMATION },
  },
  {
    path: 'confirmacion-correo/solicitud-registro',
    component: EmailConfirmation,
    data: { confirmation: REGISTER_REQUEST_EMAIL_CONFIRMATION },
  },
  {
    path: 'confirmacion-correo/recuperar-acceso',
    component: EmailConfirmation,
    data: { confirmation: RECOVER_ACCESS_EMAIL_CONFIRMATION },
  },
  {
    path: 'confirmacion-correo/verificar-codigo',
    component: EmailConfirmation,
    data: { confirmation: TWO_FACTOR_EMAIL_CONFIRMATION },
  },
];
