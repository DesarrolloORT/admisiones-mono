export type BecaEstado = 'En proceso' | 'Consulta' | 'Aceptada' | 'Estudio';

export interface MiBeca {
  id: number;
  nombreBeca: string;
  nombreCarrera: string;
  estado: BecaEstado;
  cierrePostulacion: string;
  fechaPrueba: string;
  resultadoPrueba: string;
  beneficio: string;
  fechaResultados: string;
}
