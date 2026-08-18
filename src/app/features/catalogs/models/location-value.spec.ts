import { LocationValue } from './location-value';

describe('LocationValue', () => {
  it('should represent nullable country, state and city ids', () => {
    const value: LocationValue = {
      countryCode: null,
      stateCode: null,
      cityCode: null,
    };

    expect(value.countryCode).toBeNull();
  });
});
