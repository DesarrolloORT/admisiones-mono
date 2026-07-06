import { Routes } from '@angular/router';

import { Becas } from './pages/becas/becas';
import { Fbc } from './pages/fbc/fbc';
import { Fbr } from './pages/fbr/fbr';
import { Fcl } from './pages/fcl/fcl';
import { Fexa } from './pages/fexa/fexa';

export const routes: Routes = [
  { path: '', component: Becas },
  { path: 'fbr', component: Fbr },
  { path: 'fexa', component: Fexa },
  { path: 'fcl', component: Fcl },
  { path: 'fbc', component: Fbc },
];
