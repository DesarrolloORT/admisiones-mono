import { getAcademicCareerOptions, getAvailableAcademicProposalTypes } from './academic-proposal';

describe('academic proposal options', () => {
  const careers = [
    {
      idProducto: 10,
      idNivelProducto: 1,
      nombreProducto: 'Ingeniería',
      nombreNivelProducto: 'Carrera universitaria',
    },
    {
      idProducto: 20,
      idNivelProducto: 2,
      nombreProducto: 'Analista Programador',
      nombreNivelProducto: 'Tecnicatura',
      nombreEscuela: 'Facultad de Ingeniería',
    },
  ];

  it('offers only proposal types present in the careers catalog', () => {
    expect(getAvailableAcademicProposalTypes(careers).map(option => option.value)).toEqual([
      '1',
      '2',
    ]);
  });

  it('filters careers by proposal type', () => {
    expect(getAcademicCareerOptions(careers, '2')).toEqual([
      { value: '20', label: 'Analista Programador', school: 'Facultad de Ingeniería' },
    ]);
  });
});
