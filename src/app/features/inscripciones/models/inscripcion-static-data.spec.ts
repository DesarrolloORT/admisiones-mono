import { PAYMENT_OPTIONS } from './inscripcion-static-data';

describe('PAYMENT_OPTIONS', () => {
  it('keeps payment values stable for the flow policy', () => {
    expect(PAYMENT_OPTIONS.map(option => option.value)).toEqual([
      'cuenta-bancaria',
      'tarjeta-credito',
      'cuenta-personal',
      'banred',
      'abitab',
      'paganza',
    ]);
  });
});
