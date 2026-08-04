import { PAYMENT_OPTIONS } from './inscription-static-data';

describe('PAYMENT_OPTIONS', () => {
  it('keeps payment values stable for the flow policy', () => {
    expect(PAYMENT_OPTIONS.map(option => option.value)).toEqual([
      'cuenta-bancaria',
      'cuenta-personal',
      'banred',
      'geopay',
      'abitab',
      'paganza',
    ]);
  });
});
