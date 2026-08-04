export type InscripcionEstado = 'Confirmada' | 'Pendiente' | 'Dada de baja';

export interface MiInscripcionSeminario {
  idInscripto: number;
  idOferta: number;
  descripcionOferta: string;
  idComienzo: number;
  idTurno: number;
  nombreComienzo: string;
  nombreTurno: string;
}

export interface MiInscripcion {
  idInscripto: number;
  idOfertas: number[];
  idProducto: number;
  idProceso: number;
  idComienzo: number;
  idTurno: number;
  nombreProducto: string;
  nombreComienzo: string;
  nombreTurno: string;
  estado: InscripcionEstado;
  seminarios: MiInscripcionSeminario[];
}
