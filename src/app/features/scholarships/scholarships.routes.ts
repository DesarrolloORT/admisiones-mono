import { Routes } from '@angular/router';

import { ScholarshipProcess } from './pages/scholarship-process/scholarship-process';
import { Scholarships } from './pages/scholarships/scholarships';

/**
 * Las cuatro becas comparten la misma página de proceso: lo único que cambia es
 * la beca, que llega por `data.kind` y decide qué secciones y validadores
 * aplican.
 */
export const routes: Routes = [
  { path: '', component: Scholarships },
  { path: 'fbr', component: ScholarshipProcess, data: { kind: 'fbr' } },
  { path: 'fexa', component: ScholarshipProcess, data: { kind: 'fexa' } },
  { path: 'fcl', component: ScholarshipProcess, data: { kind: 'fcl' } },
  { path: 'fbc', component: ScholarshipProcess, data: { kind: 'fbc' } },
];
