import { ScholarshipProcess } from './pages/scholarship-process/scholarship-process';
import { Scholarships } from './pages/scholarships/scholarships';
import { routes } from './scholarships.routes';

describe('scholarships routes', () => {
  it('maps the empty path to the scholarships shell', () => {
    expect(routes).toContainEqual({ path: '', component: Scholarships });
  });

  it('sends the four scholarships to the same process page', () => {
    const processRoutes = routes.filter(route => route.component === ScholarshipProcess);

    expect(processRoutes.map(route => route.path)).toEqual(['fbr', 'fexa', 'fcl', 'fbc']);
    expect(processRoutes.map(route => route.data?.['kind'])).toEqual(['fbr', 'fexa', 'fcl', 'fbc']);
  });
});
