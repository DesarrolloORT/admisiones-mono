import { homeResolver, routes } from './home.routes';

describe('home routes', () => {
  it('should resolve home data whenever Home is activated', () => {
    const homeRoute = routes[0].children?.find(route => route.path === '');

    expect(homeRoute?.resolve?.['homeData']).toBe(homeResolver);
    expect(homeRoute?.runGuardsAndResolvers).toBe('always');
  });
});
