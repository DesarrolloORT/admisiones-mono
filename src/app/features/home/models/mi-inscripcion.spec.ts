import { buildPendingPaymentDetail, MiInscripcion } from './mi-inscripcion';

describe('MiInscripcion', () => {
  it('keeps the process identifier required by the detail endpoint', () => {
    const inscription = createEnrollment({ estado: 'Pago pendiente' });

    expect(inscription.idProceso).toBe(200);
  });
});

describe('buildPendingPaymentDetail', () => {
  it('asks for the payment before the deadline of the only pending enrollment', () => {
    const detail = buildPendingPaymentDetail([
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-15' }),
      createEnrollment({ estado: 'Confirmada', fechaVencimientoPago: '2026-08-01' }),
    ]);

    expect(detail).toBe('Realizá el pago antes del 15/07/2026.');
  });

  it('lists both deadlines when two enrollments are pending', () => {
    const detail = buildPendingPaymentDetail([
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-15' }),
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-20' }),
    ]);

    expect(detail).toBe(
      'Tus inscripciones pendientes de pago vencerán los días 15/07/2026 y 20/07/2026.'
    );
  });

  it('lists every deadline when more than two enrollments are pending', () => {
    const detail = buildPendingPaymentDetail([
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-15' }),
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-20' }),
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-25' }),
    ]);

    expect(detail).toBe(
      'Tus inscripciones pendientes de pago vencerán los días 15/07/2026, 20/07/2026 y 25/07/2026.'
    );
  });

  it('ignores pending enrollments without a usable deadline', () => {
    const detail = buildPendingPaymentDetail([
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: null }),
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: 'not-a-date' }),
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-15' }),
    ]);

    expect(detail).toBe('Realizá el pago antes del 15/07/2026.');
  });

  it('falls back to the generic detail when no deadline is informed', () => {
    const detail = buildPendingPaymentDetail([
      createEnrollment({ estado: 'Pago pendiente', fechaVencimientoPago: null }),
    ]);

    expect(detail).toBe('Consultá el detalle desde Mis carreras.');
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
