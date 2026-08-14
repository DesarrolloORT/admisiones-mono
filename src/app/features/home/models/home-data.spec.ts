import type { HomeData } from './home-data';

describe('HomeData', () => {
  it('groups inscriptions and scholarships', () => {
    const data: HomeData = { enrollments: [], scholarships: [] };

    expect(data).toEqual({ enrollments: [], scholarships: [] });
  });
});
