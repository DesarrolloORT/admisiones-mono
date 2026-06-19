import { MiBeca } from './mi-beca';
import { MiInscripcion } from './mi-inscripcion';

export interface HomeData {
  inscripciones: MiInscripcion[];
  becas: MiBeca[];
}
