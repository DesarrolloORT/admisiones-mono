import { Routes } from '@angular/router';

import { Layout } from './pages/layout';
import { inscriptionDetailResolver } from './resolvers/inscription-detail.resolver';
import { inscriptionInitialSurveyResolver } from './resolvers/inscription-initial-survey.resolver';

export const routes: Routes = [
  {
    path: '',
    component: Layout,
    resolve: {
      initialSurvey: inscriptionInitialSurveyResolver,
      entry: inscriptionDetailResolver,
    },
  },
];
