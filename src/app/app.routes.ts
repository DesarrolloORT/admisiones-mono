import { Routes } from '@angular/router';

import { authMatchGuard } from './core/guards/auth';

export const routes: Routes = [
  {
    path: 'inicio',
    canMatch: [authMatchGuard],
    loadChildren: () => import('./features/home/home.routes').then(m => m.routes),
  },
  {
    path: 'inscripciones',
    canMatch: [authMatchGuard],
    loadChildren: () => import('./features/inscriptions/inscriptions.routes').then(m => m.routes),
  },
  {
    path: 'becas',
    canMatch: [authMatchGuard],
    loadChildren: () => import('./features/scholarships/scholarships.routes').then(m => m.routes),
  },
  {
    path: '',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.routes),
  },
  { path: '**', redirectTo: 'iniciar-sesion' },
];
