import {
  getAcademicCareerOptions,
  getAcademicProposalTerminology,
  getAcademicProposalTypes,
  isProfessionalUpdateLevel,
  isProfessionalUpdateType,
  toAcademicSeminarOption,
} from './academic-proposal';

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

  it('offers every proposal type before loading careers', () => {
    expect(getAcademicProposalTypes().map(option => option.value)).toEqual(['1', '2', '3']);
  });

  it('filters careers by proposal type', () => {
    expect(getAcademicCareerOptions(careers, '2')).toEqual([
      { value: '20', label: 'Analista Programador', school: 'Facultad de Ingeniería' },
    ]);
  });

  it('detects the professional update proposal by type and by product level', () => {
    expect(isProfessionalUpdateType('3')).toBe(true);
    expect(isProfessionalUpdateType('1')).toBe(false);
    expect(isProfessionalUpdateType('')).toBe(false);
    expect(isProfessionalUpdateLevel(3)).toBe(true);
    expect(isProfessionalUpdateLevel(4)).toBe(true);
    expect(isProfessionalUpdateLevel(1)).toBe(false);
    expect(isProfessionalUpdateLevel(null)).toBe(false);
  });

  it('uses Programa/Seminario terminology only for professional update', () => {
    expect(getAcademicProposalTerminology('3').careerLabel).toBe('Programa');
    expect(getAcademicProposalTerminology('3').startLabel).toBe('Seminario');
    for (const value of ['1', '2', '', '9']) {
      expect(getAcademicProposalTerminology(value).careerLabel).toBe('Carrera');
      expect(getAcademicProposalTerminology(value).startLabel).toBe('Comienzo');
    }
  });

  it('maps a seminar to a select option with its start date as description', () => {
    expect(
      toAcademicSeminarOption({
        idOferta: 300,
        idProceso: 200,
        nombre: 'Marco legal y tributario',
        fechaComienzo: '19/05/2026',
      })
    ).toEqual({ value: '300', label: 'Marco legal y tributario', description: '19/05/2026' });
  });

  it('shows only the date when the catalog sends an ISO date with time', () => {
    expect(
      toAcademicSeminarOption({
        idOferta: 300,
        idProceso: 200,
        nombre: 'Taller de equipos y liderazgo',
        fechaComienzo: '2026-10-16T00:00:00',
      }).description
    ).toBe('16/10/2026');
  });

  it('omits the description when the seminar has no start date', () => {
    expect(
      toAcademicSeminarOption({
        idOferta: 300,
        idProceso: 200,
        nombre: 'Renta fija',
        fechaComienzo: null,
      }).description
    ).toBeUndefined();
  });
});
