import { Routes } from '@angular/router';

import { Becas } from './pages/becas/becas';
import { Fbr } from './pages/fbr/fbr';

export const routes: Routes = [
  { path: '', component: Becas },
  { path: 'fbr', component: Fbr },
];
