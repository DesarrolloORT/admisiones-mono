import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'inicio',
    loadChildren: () => import('./features/home/home.routes').then(m => m.routes),
  },
  {
    path: '',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.routes),
  },
  { path: '**', redirectTo: 'iniciar-sesion' },
];

