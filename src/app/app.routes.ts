import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./features/sandbox/sandbox.routes').then(m => m.routes),
  },
  { path: '**', redirectTo: '' },
];

