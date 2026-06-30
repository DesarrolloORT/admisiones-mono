import type { InscripcionStep } from './inscripcion-process';

export type EscenarioInscripcion = 'primera-vez' | 'parcial' | 'encuesta-completa';

export type EstadoEncuestaInicial = 'no-iniciada' | 'en-progreso' | 'completa';

export type SeccionEncuestaId =
  | 'educacion'
  | 'decision-academica'
  | 'experiencia-ort'
  | 'situacion-laboral'
  | 'identidad'
  | 'reglamento';

export type EstadoSeccionEncuesta = 'pendiente' | 'activa' | 'completa';

export type MetodoPago =
  | 'cuenta-bancaria'
  | 'tarjeta-credito'
  | 'cuenta-personal'
  | 'banred'
  | 'abitab'
  | 'paganza';

export type ResultadoPago = 'confirmada' | 'reservada' | 'en-proceso';

export interface InscripcionInitialSurvey {
  carreraId: number | null;
  comienzoId: number | null;
  turnoId: number | null;
  nivelProductoId: number | null;
  completa: boolean;
  seccionActiva: SeccionEncuestaId | null;
  cursaSecundaria: boolean | null;
  orientacionBachilleratoId: number | null;
  anioBachilleratoId: number | null;
  institucionSecundariaId: number | null;
  ubicacionSecundariaId: number | null;
  nombreInstitucionSecundaria: string | null;
  tieneEducacionSuperior: boolean | null;
  nivelFormacionMadreId: number | null;
  nivelFormacionPadreId: number | null;
  madreEgresadaOrt: boolean | null;
  padreEgresadoOrt: boolean | null;
  anioDecisionCarreraId: number | null;
  anioDecisionOrtId: number | null;
  seInformoEnOtrasUniversidades: boolean | null;
  apoyoPadres: boolean | null;
  apoyoOtros: boolean | null;
  apoyoAmigosFamiliares: boolean | null;
  apoyoNadie: boolean | null;
  apoyoAmigoPropuesta: boolean | null;
  decisionConfirmada: boolean | null;
  tuvoAsesoramientoOrt: boolean | null;
  valoracionAsesoramientoOrt: number | null;
  visitoSitioWebOrt: boolean | null;
  valoracionSitioWebOrt: number | null;
  visitoInstalacionesOrt: boolean | null;
  valoracionInstalacionesOrt: number | null;
  recuerdaPublicidadOrt: boolean | null;
}

export interface InscripcionInitialSurveyResponse {
  tieneDerechoEncuesta: boolean;
  encuesta: InscripcionInitialSurvey | null;
  universidadesConsideradas: number[];
  universidadesEducacionSuperior: number[];
  opcionesMotivosSeleccionados: number[];
  opcionesPublicidadSeleccionadas: number[];
}

export interface InscripcionInitialSurveyPayload {
  carreraId: number | null;
  comienzoId: number | null;
  orientacionBachilleratoId: number | null;
  anioBachillerato: number | null;
  vecesRecursaAnioBachillerato: number | null;
  recursaAnioBachillerato: boolean | null;
  nivelFormacionPadreTutorId: number | null;
  nivelFormacionMadreTutorId: number | null;
  anioDecisionCarreraId: number | null;
  anioDecisionOrtId: number | null;
  seInformoEnOtrasUniversidades: boolean | null;
  informacionOtrasUniversidadesLinea1: string | null;
  informacionOtrasUniversidadesLinea2: string | null;
  apoyoDecisionId: number | null;
  institucionSecundariaId: number | null;
  autorizaInformarEncuesta: boolean | null;
  nombreInstitucionSecundaria: string | null;
  ubicacionUltimoAnioSecundariaId: number | null;
  estadoEducacionSuperiorPreviaId: number | null;
  nivelDecisionId: number | null;
  tuvoAsesoramientoOrt: boolean | null;
  valoracionAsesoramientoOrtId: number | null;
  visitoSitioWebOrt: boolean | null;
  valoracionSitioWebOrtId: number | null;
  visitoInstalacionesOrt: boolean | null;
  valoracionInstalacionesOrtId: number | null;
  recuerdaPublicidadOrt: boolean | null;
  madreTutorEgresadoOrt: boolean | null;
  padreTutorEgresadoOrt: boolean | null;
  trabajaActualmente: boolean | null;
  tipoJornadaId: number | null;
  universidadConsideradaIds: number[] | null;
  universidadEducacionSuperiorIds: number[] | null;
  publicidadOrtIds: number[] | null;
  motivoEleccionOrtIds: number[] | null;
}

export interface InscripcionConfirmPreEnrollmentPayload {
  aceptoReglamento: boolean;
  idOfertaSeleccionada: number;
}

export interface InscripcionProductInterestPayload {
  idOferta: number;
  idProcesoSeleccionado: number;
  idProducto: number;
}

export interface InscripcionStudentRegulationAcceptance {
  aceptoReglamentoEstudiantil: boolean;
  fechaAceptacion: string | null;
}

export interface InscripcionPreEnrollmentResponse {
  confirmada: boolean;
  fechaVencimientoPago: string | null;
  seniaInscripcion: number | null;
  saldoCuenta: number | null;
  resumen: {
    carrera: string | null;
    comienzo: string | null;
    turno: string | null;
  } | null;
}

export interface InscripcionIdentityDocumentFile {
  archivo: string | null;
  nombreArchivo: string | null;
}

export interface InscripcionIdentityDocument {
  frente: InscripcionIdentityDocumentFile | null;
  dorso: InscripcionIdentityDocumentFile | null;
  fechaVencimiento: string | null;
}
export interface OpcionInscripcion {
  value: string;
  label: string;
  icon?: string;
  hint?: string;
}

export interface ArchivosIdentidad {
  frente: File | null;
  dorso: File | null;
  selfie: File | null;
}

export interface ValoresPropuesta {
  tipoPropuesta: string;
  carrera: string;
  comienzo: string;
  turno: string;
}

export interface ValoresEncuesta {
  educacion: {
    cursaSecundaria: string;
    anioSecundaria: string;
    tipoBachillerato: string;
    orientacion: string;
    lugarSecundaria: string;
    departamento: string;
    institucionEducativa: string;
    estadoEducacionSuperior: string;
    universidadesEducacionSuperior: string[];
    formacionMadre: string;
    tituloOrtMadre: string;
    formacionPadre: string;
    tituloOrtPadre: string;
  };
  decisionAcademica: {
    anioDecisionCarrera: string;
    apoyoDecision: string;
    anioDecisionOrt: string;
    otrasUniversidades: string;
    universidadesInformadas: string[];
    certezaDecision: string;
    motivosOrt: string[];
  };
  experienciaOrt: {
    reunionAsesoramiento: string;
    calificacionAsesoramiento: number | null;
    visitoWeb: string;
    calificacionWeb: number | null;
    visitoSede: string;
    calificacionSede: number | null;
    recuerdaPublicidad: string;
    mediosPublicidad: string[];
  };
  situacionLaboral: {
    situacionLaboral: string;
    tipoJornadaLaboral: string;
  };
}

export interface BorradorInscripcion {
  version: 2;
  escenario: EscenarioInscripcion;
  paso: InscripcionStep;
  seccionActiva: SeccionEncuestaId;
  seccionesCompletas: SeccionEncuestaId[];
  propuesta: ValoresPropuesta;
  encuesta: ValoresEncuesta;
  identidad: {
    vencimientoDocumento: string;
  };
  reglamento: {
    aceptaReglamento: boolean;
  };
  pago: {
    metodoPago: MetodoPago | '';
  };
  preinscripcion: InscripcionPreEnrollmentResponse | null;
}

export interface EnvioInscripcion {
  escenario: EscenarioInscripcion;
  estadoEncuestaInicial: EstadoEncuestaInicial;
  propuesta: ValoresPropuesta;
  encuesta: ValoresEncuesta | null;
  identidad: {
    vencimientoDocumento: string;
    frenteAdjunto: boolean;
    dorsoAdjunto: boolean;
    selfieAdjunta: boolean;
  };
  reglamentoAceptado: boolean;
  metodoPago: MetodoPago;
}

export interface ItemResumenInscripcion {
  icon: string;
  label: string;
  value: string;
}

export interface ContactoCoordinador {
  role: string;
  name: string;
  email: string;
  imageUrl?: string;
}

export interface InstruccionReserva {
  title: string;
  description: string;
  items: readonly string[];
  help: string;
}

export interface StudentServiceLink {
  label: string;
  icon: string;
  url: string;
}

export const SECCIONES_ENCUESTA: readonly SeccionEncuestaId[] = [
  'educacion',
  'decision-academica',
  'experiencia-ort',
  'situacion-laboral',
  'identidad',
  'reglamento',
];

export const SECCIONES_ENCUESTA_COMPLETA: readonly SeccionEncuestaId[] = [
  'identidad',
  'reglamento',
];
