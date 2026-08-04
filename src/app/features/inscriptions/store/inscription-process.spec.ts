import type { InscripcionPreEnrollmentResponse } from '../models/inscription-flow';
import { InscripcionProcessStore } from './inscription-process';

describe('InscripcionProcessStore', () => {
  let store: InscripcionProcessStore;

  beforeEach(() => {
    store = new InscripcionProcessStore();
  });

  it('starts the flow at the proposal step', () => {
    expect(store.flow.currentStep()).toBe('propuesta');
    expect(store.flow.currentIndex()).toBe(0);
    expect(store.flow.canGoBack()).toBe(false);
  });

  it('navigates through the enrollment steps', () => {
    expect(store.flow.next()).toBe(true);
    expect(store.flow.currentStep()).toBe('encuesta');
    expect(store.flow.currentIndex()).toBe(1);
    expect(store.flow.canGoBack()).toBe(true);

    expect(store.flow.next()).toBe(true);
    expect(store.flow.currentStep()).toBe('pago');
    expect(store.flow.next()).toBe(false);

    expect(store.flow.previous()).toBe(true);
    expect(store.flow.currentStep()).toBe('encuesta');
  });

  it('jumps to a step and resets to the beginning', () => {
    expect(store.flow.goTo('pago')).toBe(true);
    expect(store.flow.currentIndex()).toBe(2);

    store.flow.reset();

    expect(store.flow.currentStep()).toBe('propuesta');
    expect(store.flow.canGoBack()).toBe(false);
  });

  it('holds the pre-enrollment response, starting empty', () => {
    expect(store.preEnrollmentResponse()).toBeNull();

    const response: InscripcionPreEnrollmentResponse = {
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: '2027-03-04',
      seniaInscripcion: 15500,
      saldoCuenta: 1200,
      resumen: { carrera: 'Sistemas', comienzo: 'Marzo 2027', turno: 'Noche' },
    };
    store.preEnrollmentResponse.set(response);

    expect(store.preEnrollmentResponse()).toBe(response);
  });
});
