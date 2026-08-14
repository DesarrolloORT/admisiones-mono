import type { EnrollmentPreEnrollmentResponse, EnrollmentReservationData } from './enrollment-flow';
import {
  buildReservationInstructions,
  buildSeminarsSummary,
  buildSummaryItems,
  formatEnrollmentAmount,
  formatPaymentDeadline,
} from './enrollment-flow-view';

const PRE_ENROLLMENT: EnrollmentPreEnrollmentResponse = {
  confirmed: false,
  paymentDueDate: '2027-03-04',
  enrollmentDeposit: 15500,
  accountBalance: null,
  summary: null,
};

const RESERVATION: EnrollmentReservationData = {
  documentNumber: '1.234.567-8',
  personCode: 34692671,
};

const PROFESSIONAL_UPDATE_CONTEXT = {
  response: {
    confirmed: true,
    paymentDueDate: '2027-03-04',
    enrollmentDeposit: 15500,
    accountBalance: 70000,
    summary: { degreeProgram: 'Actualización en IA', intake: null, shift: null },
  },
  selectedCareer: '',
  selectedStart: '',
  selectedShift: '',
  careerOptions: [],
  startOptions: [],
  shiftOptions: [],
  isProfessionalUpdate: true,
};

describe('enrollment flow view', () => {
  it('uses backend summary values and formats payment data', () => {
    const items = buildSummaryItems({
      response: {
        confirmed: true,
        paymentDueDate: '2027-03-04',
        enrollmentDeposit: 15500,
        accountBalance: 70000,
        summary: { degreeProgram: 'Sistemas', intake: 'Agosto', shift: 'Nocturno' },
      },
      selectedCareer: '',
      selectedStart: '',
      selectedShift: '',
      careerOptions: [],
      startOptions: [],
      shiftOptions: [],
      isProfessionalUpdate: false,
      seminars: [],
    });

    expect(items.map(item => item.label)).toEqual(['Carrera', 'Comienzo', 'Turno']);
    expect(items.map(item => item.value)).toEqual(['Sistemas', 'Agosto', 'Nocturno']);
    expect(formatPaymentDeadline('2027-03-04')).toBe('04/03/2027');
    expect(formatEnrollmentAmount(15500)).toBe('$ 15.500');
  });

  it('collapses the summary to a single Programa row for Actualización profesional', () => {
    const items = buildSummaryItems({
      ...PROFESSIONAL_UPDATE_CONTEXT,
      seminars: [
        { idEnrollment: 1, name: 'Seminario A', intake: 'Marzo', shift: 'Noche' },
        { idEnrollment: 2, name: 'Seminario B', intake: 'Abril', shift: 'Mañana' },
      ],
    });

    expect(items).toEqual([{ icon: 'school', label: 'Programa', value: 'Actualización en IA' }]);
  });

  it('adds the Comienzo row when Actualización profesional has a single seminario', () => {
    const items = buildSummaryItems({
      ...PROFESSIONAL_UPDATE_CONTEXT,
      seminars: [{ idEnrollment: 1, name: 'Seminario A', intake: 'Marzo', shift: 'Noche' }],
    });

    expect(items).toEqual([
      { icon: 'school', label: 'Programa', value: 'Actualización en IA' },
      { icon: 'calendar_today', label: 'Comienzo', value: 'Marzo' },
    ]);
  });

  it('keeps the single Programa row when Actualización profesional has no seminarios', () => {
    const items = buildSummaryItems({ ...PROFESSIONAL_UPDATE_CONTEXT, seminars: [] });

    expect(items).toEqual([{ icon: 'school', label: 'Programa', value: 'Actualización en IA' }]);
  });

  it('builds one seminario row per inscripción, falling back for missing data', () => {
    const seminars = buildSeminarsSummary({
      confirmed: true,
      paymentDueDate: null,
      enrollmentDeposit: null,
      accountBalance: null,
      summary: null,
      seminars: [
        {
          idEnrollment: 1,
          offeringId: 10,
          name: 'Seminario A',
          intake: 'Marzo',
          shift: 'Noche',
        },
        { idEnrollment: 2, offeringId: 11, name: null, intake: null, shift: null },
      ],
    });

    expect(seminars).toEqual([
      { idEnrollment: 1, name: 'Seminario A', intake: 'Marzo', shift: 'Noche' },
      { idEnrollment: 2, name: 'No informado', intake: 'No informado', shift: 'No informado' },
    ]);
  });

  it('returns no seminario rows when the response has none', () => {
    expect(buildSeminarsSummary(null)).toEqual([]);
    expect(
      buildSeminarsSummary({
        confirmed: true,
        paymentDueDate: null,
        enrollmentDeposit: null,
        accountBalance: null,
        summary: null,
      })
    ).toEqual([]);
  });

  it('does not invent missing or invalid payment data', () => {
    expect(formatPaymentDeadline(null)).toBe('No informado');
    expect(formatPaymentDeadline('2027-02-30')).toBe('No informado');
    expect(formatPaymentDeadline('not-a-date')).toBe('No informado');
    expect(formatEnrollmentAmount(null)).toBe('');
    expect(formatEnrollmentAmount(-1)).toBe('');
    expect(formatEnrollmentAmount(0)).toBe('$ 0');
  });

  it('builds abitab reservation instructions with the real backend data', () => {
    const abitab = buildReservationInstructions('abitab', PRE_ENROLLMENT, RESERVATION);
    expect(abitab.description).toContain('04/03/2027');
    expect(abitab.intro).toContain('local habilitado');
    expect(abitab.items).toEqual([
      { label: 'Cédula de identidad', value: '1.234.567-8' },
      { label: 'Número de estudiante', value: '34692671' },
      { label: 'Monto a pagar', value: '$ 15.500' },
    ]);
  });

  it('builds paganza reservation instructions with student number and amount', () => {
    const paganza = buildReservationInstructions('paganza', PRE_ENROLLMENT, RESERVATION);
    expect(paganza.intro).toContain('Paganza');
    expect(paganza.items).toEqual([
      { label: 'Número de estudiante', value: '34692671' },
      { label: 'Monto a pagar', value: '$ 15.500' },
    ]);
  });

  it('omits reservation items when the backend data is unknown', () => {
    const instructions = buildReservationInstructions('abitab', PRE_ENROLLMENT, null);

    expect(instructions.items).toEqual([{ label: 'Monto a pagar', value: '$ 15.500' }]);
  });

  it('replaces payment instructions with the office contact message when the deposit is 0', () => {
    const instructions = buildReservationInstructions(
      'abitab',
      {
        ...PRE_ENROLLMENT,
        enrollmentDeposit: 0,
      },
      RESERVATION
    );

    expect(instructions.title).toBe('¡Inscripción reservada!');
    expect(instructions.description).toBe(
      'Para continuar el proceso de inscripciones debe comunicarse con la oficina de Admisiones.'
    );
    expect(instructions.intro).toBe('');
    expect(instructions.items).toEqual([]);
    expect(instructions.help).toBe('');
  });

  it('omits unknown reservation data instead of inventing it', () => {
    const instructions = buildReservationInstructions(null, null, null);

    expect(instructions.title).toBe('¡Inscripción reservada!');
    expect(instructions.description).toContain('Realizá el pago de la seña');
    expect(instructions.items).toEqual([]);
    expect(JSON.stringify(instructions)).not.toContain('397654');
  });
});
