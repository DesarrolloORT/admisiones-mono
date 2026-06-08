import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth';

export const routes: Routes = [
  {
    path: 'inicio',
    loadChildren: () => import('./features/home/home.routes').then(m => m.routes),
  },
  {
    path: 'inscripciones',
    canActivate: [authGuard],
    loadChildren: () => import('./features/inscripciones/inscripciones.routes').then(m => m.routes),
  },
  {
    path: '',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.routes),
  },
  { path: '**', redirectTo: 'iniciar-sesion' },
];

