import { Routes } from '@angular/router';

import { authGuard, authMatchGuard } from './core/guards/auth';

export const routes: Routes = [
  {
    path: 'inicio',
    canMatch: [authMatchGuard],
    canActivate: [authGuard],
    loadChildren: () => import('./features/home/home.routes').then(m => m.routes),
  },
  {
    path: 'inscripciones',
    canMatch: [authMatchGuard],
    canActivate: [authGuard],
    loadChildren: () => import('./features/inscriptions/inscriptions.routes').then(m => m.routes),
  },
  {
    path: 'becas',
    canMatch: [authMatchGuard],
    canActivate: [authGuard],
    loadChildren: () => import('./features/becas/becas.routes').then(m => m.routes),
  },
  {
    path: '',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.routes),
  },
  { path: '**', redirectTo: 'iniciar-sesion' },
];
