import { detailToPreEnrollment, type InscripcionDetail } from './inscription-detail';

describe('detailToPreEnrollment', () => {
  it('reconstructs the payment context from a pending payment detail', () => {
    const detail: InscripcionDetail = {
      estado: 'Pago pendiente',
      detalle: null,
      intereses: [],
      pagoPendiente: {
        idInscripcion: 1072704,
        senia: 3339,
        saldoCuenta: 70000,
        fechaVencimientoPago: '2026-06-26T16:29:20',
        resumen: summary(),
        seminarios: [seminario()],
      },
      seniaMinima: null,
      confirmada: null,
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: '2026-06-26T16:29:20',
      seniaInscripcion: 3339,
      saldoCuenta: 70000,
      resumen: { carrera: 'Arquitectura', comienzo: 'Marzo-abril 2027', turno: 'Matutino' },
      seminarios: [seminario()],
    });
  });

  it('reconstructs the deposit amount from seniaMinima when the method was chosen', () => {
    const detail: InscripcionDetail = {
      estado: 'Pago pendiente',
      detalle: null,
      intereses: [],
      pagoPendiente: null,
      seniaMinima: { metodoPago: 'ABITAB', cedula: '12345678', codigoPersona: 555, senia: 3339 },
      confirmada: null,
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      idInscripcion: null,
      confirmada: false,
      fechaVencimientoPago: null,
      seniaInscripcion: 3339,
      saldoCuenta: null,
      resumen: null,
      seminarios: [],
    });
  });

  it('reconstructs the summary for a confirmed enrollment without payment fields', () => {
    const detail: InscripcionDetail = {
      estado: 'Confirmada',
      detalle: null,
      intereses: [],
      pagoPendiente: null,
      seniaMinima: null,
      confirmada: {
        numeroEstudiante: 397654,
        resumen: summary(),
        coordinadorAcademico: null,
        coordinadorCursos: null,
        inscripciones: [],
      },
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      idInscripcion: null,
      confirmada: true,
      fechaVencimientoPago: null,
      seniaInscripcion: null,
      saldoCuenta: null,
      resumen: { carrera: 'Arquitectura', comienzo: 'Marzo-abril 2027', turno: 'Matutino' },
      seminarios: [],
    });
  });

  it('prefers the pending payment over the confirmation when both are present', () => {
    const detail: InscripcionDetail = {
      estado: 'Pago pendiente',
      detalle: null,
      intereses: [],
      pagoPendiente: {
        idInscripcion: 1072704,
        senia: 1000,
        saldoCuenta: 500,
        fechaVencimientoPago: '2026-06-26T16:29:20',
        resumen: summary(),
        seminarios: [],
      },
      seniaMinima: null,
      confirmada: {
        numeroEstudiante: 397654,
        resumen: { ...summary(), carrera: 'Diseño', comienzo: 'Agosto 2027', turno: 'Nocturno' },
        coordinadorAcademico: null,
        coordinadorCursos: null,
        inscripciones: [],
      },
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      idInscripcion: 1072704,
      confirmada: true,
      fechaVencimientoPago: '2026-06-26T16:29:20',
      seniaInscripcion: 1000,
      saldoCuenta: 500,
      resumen: { carrera: 'Arquitectura', comienzo: 'Marzo-abril 2027', turno: 'Matutino' },
      seminarios: [],
    });
  });

  it('keeps a null summary when the pending payment has none', () => {
    const detail: InscripcionDetail = {
      estado: 'Pago pendiente',
      detalle: null,
      intereses: [],
      pagoPendiente: {
        idInscripcion: 1072704,
        senia: 3339,
        saldoCuenta: null,
        fechaVencimientoPago: '2026-06-26T16:29:20',
        resumen: null,
        seminarios: [],
      },
      seniaMinima: null,
      confirmada: null,
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: '2026-06-26T16:29:20',
      seniaInscripcion: 3339,
      saldoCuenta: null,
      resumen: null,
      seminarios: [],
    });
  });

  it('prefers the pending payment deposit over seniaMinima and falls back when missing', () => {
    const withDeposit = (senia: number | null): InscripcionDetail => ({
      estado: 'Pago pendiente',
      detalle: null,
      intereses: [],
      pagoPendiente: {
        idInscripcion: 1072704,
        senia,
        saldoCuenta: null,
        fechaVencimientoPago: null,
        resumen: null,
        seminarios: [],
      },
      seniaMinima: { metodoPago: 'ABITAB', cedula: '12345678', codigoPersona: 555, senia: 3339 },
      confirmada: null,
    });

    expect(detailToPreEnrollment(withDeposit(1000))?.seniaInscripcion).toBe(1000);
    expect(detailToPreEnrollment(withDeposit(null))?.seniaInscripcion).toBe(3339);
  });

  it('returns null when there is no payment nor confirmation detail', () => {
    expect(
      detailToPreEnrollment({
        estado: 'En proceso',
        detalle: summary(),
        intereses: [],
        pagoPendiente: null,
        seniaMinima: null,
        confirmada: null,
      })
    ).toBeNull();
  });

  function summary() {
    return {
      idOferta: 58563,
      idProducto: 719,
      carrera: 'Arquitectura',
      comienzo: 'Marzo-abril 2027',
      turno: 'Matutino',
    };
  }

  function seminario() {
    return {
      idInscripcion: 1072704,
      idOferta: 58563,
      nombre: 'Seminario de Arquitectura',
      comienzo: 'Marzo-abril 2027',
      turno: 'Matutino',
    };
  }
});
