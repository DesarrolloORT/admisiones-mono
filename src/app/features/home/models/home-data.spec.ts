import type { HomeData } from './home-data';

describe('HomeData', () => {
  it('groups inscriptions and scholarships', () => {
    const data: HomeData = { inscripciones: [], becas: [] };

    expect(data).toEqual({ inscripciones: [], becas: [] });
  });
});
