import { Routes } from '@angular/router';

import { Inscripcion } from './pages/inscription/inscription';
import { inscriptionDetailResolver } from './resolvers/inscription-detail.resolver';
import { inscriptionInitialSurveyResolver } from './resolvers/inscription-initial-survey.resolver';

export const routes: Routes = [
  {
    path: '',
    component: Inscripcion,
    resolve: {
      initialSurvey: inscriptionInitialSurveyResolver,
      entry: inscriptionDetailResolver,
    },
  },
];
