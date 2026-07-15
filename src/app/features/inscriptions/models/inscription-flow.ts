import type { InscripcionConfirmedDetail } from './inscription-detail';

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
  'cuenta-bancaria' | 'cuenta-personal' | 'banred' | 'geopay' | 'abitab' | 'paganza';

export type MetodoPagoApi =
  'CUENTA_PERSONAL' | 'ABITAB' | 'PAGANZA' | 'BANRED' | 'GEOPAY' | 'SISTARBANC';

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
  recursaAnioBachillerato: boolean | null;
  vecesRecursaAnioBachillerato: number | null;
  institucionSecundariaId: number | null;
  ubicacionSecundariaId: number | null;
  nombreInstitucionSecundaria: string | null;
  estadoEducacionSuperiorPreviaId: number | null;
  nivelFormacionMadreId: number | null;
  nivelFormacionPadreId: number | null;
  madreEgresadaOrt: boolean | null;
  padreEgresadoOrt: boolean | null;
  anioDecisionCarreraId: number | null;
  anioDecisionOrtId: number | null;
  seInformoEnOtrasUniversidades: boolean | null;
  apoyoDecisionId: number | null;
  nivelDecisionId: number | null;
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
  universidadesConsideradasOtros: string[];
  universidadesEducacionSuperior: number[];
  universidadesEducacionSuperiorOtros: string[];
  opcionesMotivosSeleccionados: number[];
  opcionesPublicidadSeleccionadas: number[];
}

export interface InscripcionInitialSurveyPayload {
  carreraId: number | null;
  comienzoId: number | null;
  orientacionBachilleratoId: number | null;
  anioBachillerato: number | null;
  cursaSecundariaActualmente: boolean | null;
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
  universidadConsideradaOtros: string[] | null;
  universidadEducacionSuperiorIds: number[] | null;
  universidadEducacionSuperiorOtros: string[] | null;
  publicidadOrtIds: number[] | null;
  motivoEleccionOrtIds: number[] | null;
}

export interface InscripcionConfirmPreEnrollmentPayload {
  aceptoReglamento: boolean;
  idOfertaSeleccionada: number;
}

export interface InscripcionIdentityUploadFile {
  nombreArchivo: string;
  archivo: string;
}

export interface InscripcionIdentityDocumentUploadPayload {
  fecha: string;
  frente: InscripcionIdentityUploadFile;
  dorso: InscripcionIdentityUploadFile;
}

export interface InscripcionIdentityPhotoUploadPayload {
  archivoAdjunto: InscripcionIdentityUploadFile;
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
  idInscripcion?: number | null;
  confirmada: boolean;
  enEspera?: boolean;
  fechaVencimientoPago: string | null;
  seniaInscripcion: number | null;
  saldoCuenta: number | null;
  resumen: {
    carrera: string | null;
    comienzo: string | null;
    turno: string | null;
  } | null;
}

export interface InscripcionPaymentPayload {
  idInscripcion: number;
  metodoPago: MetodoPago;
  idBancoSistarbanc: string | null;
}

export interface InscripcionPaymentMessage {
  clave: string | null;
  valor: string | null;
}

export interface InscripcionPaymentResponse {
  success: boolean;
  resultado: string | null;
  urlPago: string | null;
  parametrosEncriptados: string | null;
  mensajes: InscripcionPaymentMessage[];
  confirmada: InscripcionConfirmedDetail | null;
  message: string | null;
  errorCode: string | null;
}

// Datos que el backend informa para pagar una reserva (Abitab/Paganza). Llegan
// en el bloque seniaMinima del Detalle; la pantalla de reserva los muestra.
export interface InscripcionReservationData {
  cedula: string | null;
  codigoPersona: number | null;
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

export interface ItemResumenInscripcion {
  icon: string;
  label: string;
  value: string;
}

export interface ContactoCoordinador {
  role: string;
  name: string;
  email: string;
}

export interface ItemInstruccionReserva {
  label: string;
  value: string;
}

export interface InstruccionReserva {
  title: string;
  description: string;
  intro: string;
  items: readonly ItemInstruccionReserva[];
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
