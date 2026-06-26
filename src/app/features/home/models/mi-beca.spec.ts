import type { BecaEstado } from './mi-beca';

describe('BecaEstado', () => {
  it('accepts known scholarship states', () => {
    const estado: BecaEstado = 'Aceptada';

    expect(estado).toBe('Aceptada');
  });
});
