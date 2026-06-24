import {
  buildSummaryItems,
  formatInscriptionAmount,
  formatPaymentDeadline,
} from './inscripcion-flow-view';

describe('inscripcion flow view', () => {
  it('uses backend summary values and formats payment data', () => {
    const items = buildSummaryItems({
      response: {
        confirmada: true,
        fechaVencimientoPago: '2027-03-04',
        seniaInscripcion: 15500,
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
});
