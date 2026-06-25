import { routes } from './becas.routes';
import { Becas } from './pages/becas/becas';
import { Fbr } from './pages/fbr/fbr';

describe('becas routes', () => {
  it('maps the empty path to the scholarships shell', () => {
    expect(routes).toContainEqual({ path: '', component: Becas });
  });

  it('maps the fbr path to the scholarship process page', () => {
    expect(routes).toContainEqual({ path: 'fbr', component: Fbr });
  });
});
