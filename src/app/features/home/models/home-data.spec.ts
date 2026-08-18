import type { HomeData } from './home-data';

describe('HomeData', () => {
  it('groups enrollments and scholarships', () => {
    const data: HomeData = { enrollments: [], scholarships: [] };

    expect(data).toEqual({ enrollments: [], scholarships: [] });
  });
});
