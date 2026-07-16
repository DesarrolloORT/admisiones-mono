export type InscripcionEstado = 'Confirmada' | 'Pendiente' | 'Dada de baja' | string;

export interface MiInscripcion {
  idInscripto: number;
  idProducto: number;
  idProceso: number;
  idComienzo: number;
  idTurno: number;
  nombreProducto: string;
  nombreComienzo: string;
  nombreTurno: string;
  estado: InscripcionEstado;
}
