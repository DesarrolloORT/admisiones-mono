import { buildPendingPaymentSummary, MiInscripcion } from './mi-inscripcion';

describe('MiInscripcion', () => {
  it('keeps the process identifier required by the detail endpoint', () => {
    const inscription = createEnrollment({ estado: 'Pago pendiente' });

    expect(inscription.idProceso).toBe(200);
  });
});

describe('buildPendingPaymentSummary', () => {
  it('asks for the payment before the deadline of the only pending enrollment and allows navigation', () => {
    const summary = buildPendingPaymentSummary([
      createEnrollment({
        estado: 'Pago pendiente',
        fechaVencimientoPago: '2026-07-15',
        idProducto: 20,
        idProceso: 200,
      }),
      createEnrollment({ estado: 'Confirmada', fechaVencimientoPago: '2026-08-01' }),
    ]);

    expect(summary).toEqual({
      title: 'Inscripción pendiente de pago.',
      detail: 'Realizá el pago antes del 15/07/2026.',
      navigable: true,
      target: { idProducto: 20, idProceso: 200, estado: 'Pago pendiente' },
    });
  });

  it('allows navigation for the only pending enrollment even without a usable deadline', () => {
    const summary = buildPendingPaymentSummary([
      createEnrollment({
        estado: 'Pago pendiente',
        fechaVencimientoPago: null,
        idProducto: 20,
        idProceso: 200,
      }),
    ]);

    expect(summary).toEqual({
      title: 'Inscripción pendiente de pago.',
      detail: 'Consultá el detalle desde Mis carreras.',
      navigable: true,
      target: { idProducto: 20, idProceso: 200, estado: 'Pago pendiente' },
    });
  });

  it('collapses a repeated deadline into a single date but still pluralizes the title and blocks navigation', () => {
    const summary = buildPendingPaymentSummary([
      createEnrollment({
        estado: 'Pago pendiente',
        fechaVencimientoPago: '2026-07-15',
        idProducto: 20,
        idProceso: 200,
      }),
      createEnrollment({
        estado: 'Pago pendiente',
        fechaVencimientoPago: '2026-07-15',
        idProducto: 30,
        idProceso: 300,
      }),
    ]);

    expect(summary).toEqual({
      title: 'Inscripciones pendientes de pago.',
      detail: 'Las mismas vencerán el 15/07/2026.',
      navigable: false,
      target: null,
    });
  });

  it('lists both deadlines and pluralizes the title when two enrollments have distinct dates', () => {
    const summary = buildPendingPaymentSummary([
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-15' }),
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-20' }),
    ]);

    expect(summary).toEqual({
      title: 'Inscripciones pendientes de pago.',
      detail: 'Las mismas vencerán los días 15/07/2026 y 20/07/2026.',
      navigable: false,
      target: null,
    });
  });

  it('lists every deadline when more than two enrollments are pending', () => {
    const summary = buildPendingPaymentSummary([
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-15' }),
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-20' }),
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-25' }),
    ]);

    expect(summary.detail).toBe(
      'Las mismas vencerán los días 15/07/2026, 20/07/2026 y 25/07/2026.'
    );
    expect(summary.navigable).toBe(false);
  });

  it('ignores pending enrollments without a usable deadline when listing dates', () => {
    const summary = buildPendingPaymentSummary([
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: null }),
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: 'not-a-date' }),
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-15' }),
    ]);

    expect(summary.detail).toBe('Las mismas vencerán el 15/07/2026.');
    expect(summary.navigable).toBe(false);
  });

  it('falls back to the generic detail when no deadline is informed for multiple pending enrollments', () => {
    const summary = buildPendingPaymentSummary([
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: null }),
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: null }),
    ]);

    expect(summary.title).toBe('Inscripciones pendientes de pago.');
    expect(summary.detail).toBe('Consultá el detalle desde Mis carreras.');
    expect(summary.navigable).toBe(false);
  });
});

function createEnrollment(overrides: Partial<MiInscripcion>): MiInscripcion {
  return {
    idInscripto: 100,
    idOfertas: [300],
    idProducto: 20,
    idProceso: 200,
    idComienzo: 2,
    idTurno: 3,
    nombreProducto: 'Sistemas',
    nombreComienzo: 'Marzo 2027',
    nombreTurno: 'Noche',
    estado: 'Confirmada',
    fechaVencimientoPago: null,
    seminarios: [],
    ...overrides,
  };
}
