import { Routes } from '@angular/router';

import { Inscripcion } from './pages/inscripcion/inscripcion';
import { inscripcionDetailResolver } from './resolvers/inscripcion-detail.resolver';
import { inscripcionInitialSurveyResolver } from './resolvers/inscripcion-initial-survey.resolver';

export const routes: Routes = [
  {
    path: '',
    component: Inscripcion,
    resolve: {
      initialSurvey: inscripcionInitialSurveyResolver,
      inscriptionDetail: inscripcionDetailResolver,
    },
  },
];
