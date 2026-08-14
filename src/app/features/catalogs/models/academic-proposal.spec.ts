import {
  getAcademicDegreeProgramOptions,
  getAcademicProposalTerminology,
  getAcademicProposalTypes,
  isProfessionalUpdateLevel,
  isProfessionalUpdateType,
  toAcademicSeminarOption,
} from './academic-proposal';

describe('academic proposal options', () => {
  const degreePrograms = [
    {
      productId: 10,
      productLevelId: 1,
      productName: 'Ingeniería',
      productLevelName: 'Carrera universitaria',
    },
    {
      productId: 20,
      productLevelId: 2,
      productName: 'Analista Programador',
      productLevelName: 'Tecnicatura',
      schoolName: 'Facultad de Ingeniería',
    },
  ];

  it('offers every proposal type before loading careers', () => {
    expect(getAcademicProposalTypes().map(option => option.value)).toEqual(['1', '2', '3']);
  });

  it('filters careers by proposal type', () => {
    expect(getAcademicDegreeProgramOptions(degreePrograms, '2')).toEqual([
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
    expect(getAcademicProposalTerminology('3').degreeProgramLabel).toBe('Programa');
    expect(getAcademicProposalTerminology('3').startLabel).toBe('Seminario');
    for (const value of ['1', '2', '', '9']) {
      expect(getAcademicProposalTerminology(value).degreeProgramLabel).toBe('Carrera');
      expect(getAcademicProposalTerminology(value).startLabel).toBe('Comienzo');
    }
  });

  it('maps a seminar to a select option with its start date as description', () => {
    expect(
      toAcademicSeminarOption({
        offeringId: 300,
        admissionProcessId: 200,
        name: 'Marco legal y tributario',
        startDate: '19/05/2026',
      })
    ).toEqual({ value: '300', label: 'Marco legal y tributario', description: '19/05/2026' });
  });

  it('shows only the date when the catalog sends an ISO date with time', () => {
    expect(
      toAcademicSeminarOption({
        offeringId: 300,
        admissionProcessId: 200,
        name: 'Taller de equipos y liderazgo',
        startDate: '2026-10-16T00:00:00',
      }).description
    ).toBe('16/10/2026');
  });

  it('omits the description when the seminar has no start date', () => {
    expect(
      toAcademicSeminarOption({
        offeringId: 300,
        admissionProcessId: 200,
        name: 'Renta fija',
        startDate: null,
      }).description
    ).toBeUndefined();
  });
});
