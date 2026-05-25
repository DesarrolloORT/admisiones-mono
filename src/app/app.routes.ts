import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth';

export const routes: Routes = [
  {
    path: 'inicio',
    canActivate: [authGuard],
    loadChildren: () => import('./features/home/home.routes').then(m => m.routes),
  },
  {
    path: '',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.routes),
  },
  { path: '**', redirectTo: 'iniciar-sesion' },
];

