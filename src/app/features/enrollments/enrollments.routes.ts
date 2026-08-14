import { Routes } from '@angular/router';

import { Layout } from './pages/layout';
import { enrollmentDetailResolver } from './resolvers/enrollment-detail.resolver';
import { enrollmentInitialSurveyResolver } from './resolvers/enrollment-initial-survey.resolver';

export const routes: Routes = [
  {
    path: '',
    component: Layout,
    resolve: {
      initialSurvey: enrollmentInitialSurveyResolver,
      entry: enrollmentDetailResolver,
    },
  },
];
