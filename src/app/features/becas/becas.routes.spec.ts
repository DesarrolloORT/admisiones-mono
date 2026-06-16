import { routes } from './becas.routes';
import { Becas } from './pages/becas/becas';

describe('becas routes', () => {
  it('maps the empty path to the scholarships shell', () => {
    expect(routes).toEqual([{ path: '', component: Becas }]);
  });
});
