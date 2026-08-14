import { PAYMENT_OPTIONS } from './enrollment-static-data';

describe('PAYMENT_OPTIONS', () => {
  it('keeps payment values stable for the flow policy', () => {
    expect(PAYMENT_OPTIONS.map(option => option.value)).toEqual([
      'bank-account',
      'personal-account',
      'banred',
      'geopay',
      'abitab',
      'paganza',
    ]);
  });
});
