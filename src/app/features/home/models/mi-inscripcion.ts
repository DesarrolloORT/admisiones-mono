export type InscripcionEstado = 'Confirmada' | 'Pendiente' | 'Cancelada' | string;

export interface MiInscripcion {
  idProducto: number;
  idComienzo: number;
  idTurno: number;
  nombreProducto: string;
  nombreComienzo: string;
  nombreTurno: string;
  estado: InscripcionEstado;
}

