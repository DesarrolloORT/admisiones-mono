import { Routes } from '@angular/router';

import { Inscripcion } from './pages/inscripcion/inscripcion';
import { inscripcionInitialSurveyResolver } from './resolvers/inscripcion-initial-survey.resolver';

export const routes: Routes = [
  {
    path: '',
    component: Inscripcion,
    resolve: { initialSurvey: inscripcionInitialSurveyResolver },
  },
];
