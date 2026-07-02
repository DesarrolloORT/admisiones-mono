import { detailToPreEnrollment, type InscripcionDetail } from './inscription-detail';

describe('detailToPreEnrollment', () => {
  it('reconstructs the payment context from a pending payment detail', () => {
    const detail: InscripcionDetail = {
      estado: 'Pago pendiente',
      detalle: null,
      pagoPendiente: {
        idInscripcion: 1072704,
        senia: 3339,
        saldoCuenta: 70000,
        fechaVencimientoPago: '2026-06-26T16:29:20',
        resumen: summary(),
      },
      confirmada: null,
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      confirmada: false,
      fechaVencimientoPago: '2026-06-26T16:29:20',
      seniaInscripcion: 3339,
      saldoCuenta: 70000,
      resumen: { carrera: 'Arquitectura', comienzo: 'Marzo-abril 2027', turno: 'Matutino' },
    });
  });

  it('reconstructs the summary for a confirmed enrollment without payment fields', () => {
    const detail: InscripcionDetail = {
      estado: 'Confirmada',
      detalle: null,
      pagoPendiente: null,
      confirmada: {
        numeroEstudiante: 397654,
        resumen: summary(),
        coordinadorAcademico: null,
        materiasPrimerSemestre: [],
      },
    };

    expect(detailToPreEnrollment(detail)).toEqual({
      confirmada: true,
      fechaVencimientoPago: null,
      seniaInscripcion: null,
      saldoCuenta: null,
      resumen: { carrera: 'Arquitectura', comienzo: 'Marzo-abril 2027', turno: 'Matutino' },
    });
  });

  it('returns null when there is no payment nor confirmation detail', () => {
    expect(
      detailToPreEnrollment({
        estado: 'En proceso',
        detalle: summary(),
        pagoPendiente: null,
        confirmada: null,
      })
    ).toBeNull();
  });

  function summary() {
    return {
      idOferta: 58563,
      idProducto: 719,
      carrera: 'Arquitectura',
      idComienzo: 1398,
      comienzo: 'Marzo-abril 2027',
      idTurno: 1,
      turno: 'Matutino',
    };
  }
});
