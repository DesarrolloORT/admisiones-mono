import type { ScholarshipStatus } from './scholarship-summary';

describe('BecaEstado', () => {
  it('accepts known scholarship states', () => {
    const status: ScholarshipStatus = 'Aceptada';

    expect(status).toBe('Aceptada');
  });
});
