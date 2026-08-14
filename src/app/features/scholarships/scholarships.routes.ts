import { Routes } from '@angular/router';

import { Fbr } from './pages/fbr/fbr';
import { Scholarships } from './pages/scholarships/scholarships';

export const routes: Routes = [
  { path: '', component: Scholarships },
  { path: 'fbr', component: Fbr },
];
