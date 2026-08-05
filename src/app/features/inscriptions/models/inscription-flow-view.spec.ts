import type {
  InscripcionPreEnrollmentResponse,
  InscripcionReservationData,
} from './inscription-flow';
import {
  buildReservationInstructions,
  buildSeminariosSummary,
  buildSummaryItems,
  formatInscriptionAmount,
  formatPaymentDeadline,
} from './inscription-flow-view';

const PRE_ENROLLMENT: InscripcionPreEnrollmentResponse = {
  confirmada: false,
  fechaVencimientoPago: '2027-03-04',
  seniaInscripcion: 15500,
  saldoCuenta: null,
  resumen: null,
};

const RESERVATION: InscripcionReservationData = {
  cedula: '1.234.567-8',
  codigoPersona: 34692671,
};

describe('inscription flow view', () => {
  it('uses backend summary values and formats payment data', () => {
    const items = buildSummaryItems({
      response: {
        confirmada: true,
        fechaVencimientoPago: '2027-03-04',
        seniaInscripcion: 15500,
        saldoCuenta: 70000,
        resumen: { carrera: 'Sistemas', comienzo: 'Agosto', turno: 'Nocturno' },
      },
      selectedCareer: '',
      selectedStart: '',
      selectedTurno: '',
      careerOptions: [],
      startOptions: [],
      turnoOptions: [],
      isProfessionalUpdate: false,
    });

    expect(items.map(item => item.label)).toEqual(['Carrera', 'Comienzo', 'Turno']);
    expect(items.map(item => item.value)).toEqual(['Sistemas', 'Agosto', 'Nocturno']);
    expect(formatPaymentDeadline('2027-03-04')).toBe('04/03/2027');
    expect(formatInscriptionAmount(15500)).toBe('$ 15.500');
  });

  it('collapses the summary to a single Programa row for Actualización profesional', () => {
    const items = buildSummaryItems({
      response: {
        confirmada: true,
        fechaVencimientoPago: '2027-03-04',
        seniaInscripcion: 15500,
        saldoCuenta: 70000,
        resumen: { carrera: 'Actualización en IA', comienzo: null, turno: null },
      },
      selectedCareer: '',
      selectedStart: '',
      selectedTurno: '',
      careerOptions: [],
      startOptions: [],
      turnoOptions: [],
      isProfessionalUpdate: true,
    });

    expect(items).toEqual([{ icon: 'school', label: 'Programa', value: 'Actualización en IA' }]);
  });

  it('builds one seminario row per inscripción, falling back for missing data', () => {
    const seminarios = buildSeminariosSummary({
      confirmada: true,
      fechaVencimientoPago: null,
      seniaInscripcion: null,
      saldoCuenta: null,
      resumen: null,
      seminarios: [
        {
          idInscripcion: 1,
          idOferta: 10,
          nombre: 'Seminario A',
          comienzo: 'Marzo',
          turno: 'Noche',
        },
        { idInscripcion: 2, idOferta: 11, nombre: null, comienzo: null, turno: null },
      ],
    });

    expect(seminarios).toEqual([
      { idInscripcion: 1, nombre: 'Seminario A', comienzo: 'Marzo', turno: 'Noche' },
      { idInscripcion: 2, nombre: 'No informado', comienzo: 'No informado', turno: 'No informado' },
    ]);
  });

  it('returns no seminario rows when the response has none', () => {
    expect(buildSeminariosSummary(null)).toEqual([]);
    expect(
      buildSeminariosSummary({
        confirmada: true,
        fechaVencimientoPago: null,
        seniaInscripcion: null,
        saldoCuenta: null,
        resumen: null,
      })
    ).toEqual([]);
  });

  it('does not invent missing or invalid payment data', () => {
    expect(formatPaymentDeadline(null)).toBe('No informado');
    expect(formatPaymentDeadline('2027-02-30')).toBe('No informado');
    expect(formatPaymentDeadline('not-a-date')).toBe('No informado');
    expect(formatInscriptionAmount(null)).toBe('No informado');
    expect(formatInscriptionAmount(-1)).toBe('No informado');
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
        seniaInscripcion: 0,
      },
      RESERVATION
    );

    expect(instructions.title).toBe('¡Inscripción reservada!');
    expect(instructions.description).toContain('comunicarse con la oficina');
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
