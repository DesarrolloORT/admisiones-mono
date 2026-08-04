import { getOptionLabel, toCatalogOptions } from './inscription-flow-options';

describe('inscription flow options', () => {
  it('maps catalog items to form options', () => {
    expect(toCatalogOptions([{ id: 10, label: 'Opción' }])).toEqual([
      { value: '10', label: 'Opción' },
    ]);
  });

  it('uses a fallback for an unknown option', () => {
    expect(getOptionLabel([], 'missing', 'Sin selección')).toBe('Sin selección');
  });
});
