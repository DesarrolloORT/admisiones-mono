import type {
  InscripcionPreEnrollmentResponse,
  InscripcionReservationData,
} from './inscription-flow';
import {
  buildReservationInstructions,
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
    });

    expect(items.map(item => item.value)).toEqual(['Sistemas', 'Agosto', 'Nocturno']);
    expect(formatPaymentDeadline('2027-03-04')).toBe('04/03/2027');
    expect(formatInscriptionAmount(15500)).toBe('$ 15.500');
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

  it('omits unknown reservation data instead of inventing it', () => {
    const instructions = buildReservationInstructions(null, null, null);

    expect(instructions.title).toBe('¡Inscripción reservada!');
    expect(instructions.description).toContain('Realizá el pago de la seña');
    expect(instructions.items).toEqual([]);
    expect(JSON.stringify(instructions)).not.toContain('397654');
  });
});
