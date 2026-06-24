import type { MiInscripcion } from './mi-inscripcion';

describe('MiInscripcion', () => {
  it('keeps the process identifier required by the detail endpoint', () => {
    const inscription: MiInscripcion = {
      idProducto: 20,
      idProceso: 200,
      idComienzo: 2,
      idTurno: 3,
      nombreProducto: 'Sistemas',
      nombreComienzo: 'Marzo 2027',
      nombreTurno: 'Noche',
      estado: 'Pago pendiente',
    };

    expect(inscription.idProceso).toBe(200);
  });
});
