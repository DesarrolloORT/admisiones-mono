import { Fbr } from './pages/fbr/fbr';
import { Scholarships } from './pages/scholarships/scholarships';
import { routes } from './scholarships.routes';

describe('scholarships routes', () => {
  it('maps the empty path to the scholarships shell', () => {
    expect(routes).toContainEqual({ path: '', component: Scholarships });
  });

  it('maps the fbr path to the scholarship process page', () => {
    expect(routes).toContainEqual({ path: 'fbr', component: Fbr });
  });
});
