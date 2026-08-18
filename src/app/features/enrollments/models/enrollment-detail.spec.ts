import { detailToPreEnrollment, type EnrollmentDetail } from './enrollment-detail';

describe('detailToPreEnrollment', () => {
  it('reconstructs the payment context from a pending payment detail', () => {
    const detail: EnrollmentDetail = {
      status: 'Pago pendiente',
      summary: null,
      interests: [],
      pendingPayment: {
        enrollmentId: 1072704,
        deposit: 3339,
        accountBalance: 70000,
        paymentDueDate: '2026-06-26T16:29:20',
        summary: summary(),
        seminars: [seminar()],
      },
      minimumDeposit: null,
      confirmed: null,
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      enrollmentId: 1072704,
      confirmed: false,
      paymentDueDate: '2026-06-26T16:29:20',
      enrollmentDeposit: 3339,
      accountBalance: 70000,
      summary: { degreeProgram: 'Arquitectura', intake: 'Marzo-abril 2027', shift: 'Matutino' },
      seminars: [seminar()],
    });
  });

  it('reconstructs the deposit amount from seniaMinima when the method was chosen', () => {
    const detail: EnrollmentDetail = {
      status: 'Pago pendiente',
      summary: null,
      interests: [],
      pendingPayment: null,
      minimumDeposit: {
        paymentMethod: 'ABITAB',
        documentNumber: '12345678',
        personCode: 555,
        deposit: 3339,
      },
      confirmed: null,
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      enrollmentId: null,
      confirmed: false,
      paymentDueDate: null,
      enrollmentDeposit: 3339,
      accountBalance: null,
      summary: null,
      seminars: [],
    });
  });

  it('reconstructs the summary for a confirmed enrollment without payment fields', () => {
    const detail: EnrollmentDetail = {
      status: 'Confirmada',
      summary: null,
      interests: [],
      pendingPayment: null,
      minimumDeposit: null,
      confirmed: {
        studentNumber: 397654,
        summary: summary(),
        academicCoordinator: null,
        courseCoordinator: null,
        enrollments: [],
      },
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      enrollmentId: null,
      confirmed: true,
      paymentDueDate: null,
      enrollmentDeposit: null,
      accountBalance: null,
      summary: { degreeProgram: 'Arquitectura', intake: 'Marzo-abril 2027', shift: 'Matutino' },
      seminars: [],
    });
  });

  it('prefers the pending payment over the confirmation when both are present', () => {
    const detail: EnrollmentDetail = {
      status: 'Pago pendiente',
      summary: null,
      interests: [],
      pendingPayment: {
        enrollmentId: 1072704,
        deposit: 1000,
        accountBalance: 500,
        paymentDueDate: '2026-06-26T16:29:20',
        summary: summary(),
        seminars: [],
      },
      minimumDeposit: null,
      confirmed: {
        studentNumber: 397654,
        summary: {
          ...summary(),
          degreeProgram: 'Diseño',
          intake: 'Agosto 2027',
          shift: 'Nocturno',
        },
        academicCoordinator: null,
        courseCoordinator: null,
        enrollments: [],
      },
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      enrollmentId: 1072704,
      confirmed: true,
      paymentDueDate: '2026-06-26T16:29:20',
      enrollmentDeposit: 1000,
      accountBalance: 500,
      summary: { degreeProgram: 'Arquitectura', intake: 'Marzo-abril 2027', shift: 'Matutino' },
      seminars: [],
    });
  });

  it('keeps a null summary when the pending payment has none', () => {
    const detail: EnrollmentDetail = {
      status: 'Pago pendiente',
      summary: null,
      interests: [],
      pendingPayment: {
        enrollmentId: 1072704,
        deposit: 3339,
        accountBalance: null,
        paymentDueDate: '2026-06-26T16:29:20',
        summary: null,
        seminars: [],
      },
      minimumDeposit: null,
      confirmed: null,
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      enrollmentId: 1072704,
      confirmed: false,
      paymentDueDate: '2026-06-26T16:29:20',
      enrollmentDeposit: 3339,
      accountBalance: null,
      summary: null,
      seminars: [],
    });
  });

  it('prefers the pending payment deposit over seniaMinima and falls back when missing', () => {
    const withDeposit = (deposit: number | null): EnrollmentDetail => ({
      status: 'Pago pendiente',
      summary: null,
      interests: [],
      pendingPayment: {
        enrollmentId: 1072704,
        deposit,
        accountBalance: null,
        paymentDueDate: null,
        summary: null,
        seminars: [],
      },
      minimumDeposit: {
        paymentMethod: 'ABITAB',
        documentNumber: '12345678',
        personCode: 555,
        deposit: 3339,
      },
      confirmed: null,
    });

    expect(detailToPreEnrollment(withDeposit(1000))?.enrollmentDeposit).toBe(1000);
    expect(detailToPreEnrollment(withDeposit(null))?.enrollmentDeposit).toBe(3339);
  });

  it('returns null when there is no payment nor confirmation detail', () => {
    expect(
      detailToPreEnrollment({
        status: 'En proceso',
        summary: summary(),
        interests: [],
        pendingPayment: null,
        minimumDeposit: null,
        confirmed: null,
      })
    ).toBeNull();
  });

  function summary() {
    return {
      offeringId: 58563,
      productId: 719,
      degreeProgram: 'Arquitectura',
      intake: 'Marzo-abril 2027',
      shift: 'Matutino',
    };
  }

  function seminar() {
    return {
      enrollmentId: 1072704,
      offeringId: 58563,
      name: 'Seminario de Arquitectura',
      intake: 'Marzo-abril 2027',
      shift: 'Matutino',
    };
  }
});
