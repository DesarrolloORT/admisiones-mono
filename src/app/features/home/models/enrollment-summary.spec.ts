import { buildPendingPaymentSummary, EnrollmentSummary } from './enrollment-summary';

describe('EnrollmentSummary', () => {
  it('keeps the process identifier required by the detail endpoint', () => {
    const enrollment = createEnrollment({ status: 'Pago pendiente' });

    expect(enrollment.admissionProcessId).toBe(200);
  });
});

describe('buildPendingPaymentSummary', () => {
  it('asks for the payment before the deadline of the only pending enrollment and allows navigation', () => {
    const summary = buildPendingPaymentSummary([
      createEnrollment({
        status: 'Pago pendiente',
        paymentDueDate: '2026-07-15',
        productId: 20,
        admissionProcessId: 200,
      }),
      createEnrollment({ status: 'Confirmada', paymentDueDate: '2026-08-01' }),
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
        status: 'Pago pendiente',
        paymentDueDate: null,
        productId: 20,
        admissionProcessId: 200,
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
        status: 'Pago pendiente',
        paymentDueDate: '2026-07-15',
        productId: 20,
        admissionProcessId: 200,
      }),
      createEnrollment({
        status: 'Pago pendiente',
        paymentDueDate: '2026-07-15',
        productId: 30,
        admissionProcessId: 300,
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
      createEnrollment({ status: 'Pago pendiente', paymentDueDate: '2026-07-15' }),
      createEnrollment({ status: 'Pago pendiente', paymentDueDate: '2026-07-20' }),
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
      createEnrollment({ status: 'Pago pendiente', paymentDueDate: '2026-07-15' }),
      createEnrollment({ status: 'Pago pendiente', paymentDueDate: '2026-07-20' }),
      createEnrollment({ status: 'Pago pendiente', paymentDueDate: '2026-07-25' }),
    ]);

    expect(summary.detail).toBe(
      'Las mismas vencerán los días 15/07/2026, 20/07/2026 y 25/07/2026.'
    );
    expect(summary.navigable).toBe(false);
  });

  it('ignores pending enrollments without a usable deadline when listing dates', () => {
    const summary = buildPendingPaymentSummary([
      createEnrollment({ status: 'Pago pendiente', paymentDueDate: null }),
      createEnrollment({ status: 'Pago pendiente', paymentDueDate: 'not-a-date' }),
      createEnrollment({ status: 'Pago pendiente', paymentDueDate: '2026-07-15' }),
    ]);

    expect(summary.detail).toBe('Las mismas vencerán el 15/07/2026.');
    expect(summary.navigable).toBe(false);
  });

  it('falls back to the generic detail when no deadline is informed for multiple pending enrollments', () => {
    const summary = buildPendingPaymentSummary([
      createEnrollment({ status: 'Pago pendiente', paymentDueDate: null }),
      createEnrollment({ status: 'Pago pendiente', paymentDueDate: null }),
    ]);

    expect(summary.title).toBe('Inscripciones pendientes de pago.');
    expect(summary.detail).toBe('Consultá el detalle desde Mis carreras.');
    expect(summary.navigable).toBe(false);
  });
});

function createEnrollment(overrides: Partial<EnrollmentSummary>): EnrollmentSummary {
  return {
    enrollmentId: 100,
    offeringIds: [300],
    productId: 20,
    admissionProcessId: 200,
    productLevelId: 1,
    intakeId: 2,
    shiftId: 3,
    degreeProgramName: 'Sistemas',
    intakeName: 'Marzo 2027',
    shiftName: 'Noche',
    status: 'Confirmada',
    paymentDueDate: null,
    seminars: [],
    ...overrides,
  };
}
