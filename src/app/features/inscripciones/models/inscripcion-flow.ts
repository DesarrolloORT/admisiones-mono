import { FormControl } from '@angular/forms';

export type EscenarioInscripcion = 'primera-vez' | 'parcial' | 'encuesta-completa';

export type EstadoEncuestaInicial = 'no-iniciada' | 'en-progreso' | 'completa';

export type PantallaInscripcion =
  | 'propuesta'
  | 'encuesta'
  | 'lector-reglamento'
  | 'pago'
  | 'confirmacion-pago'
  | 'procesando'
  | 'reserva'
  | 'inscripcion-confirmada'
  | 'inscripcion-en-proceso';

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

export interface InscripcionBackendSurvey {
  idProducto?: number | null;
  idProceso?: number | null;
  idTurno?: number | null;
  estadoEncuestaIniAdmision?: string | null;
  fechaProcesadoEncuestaIni?: string | null;
  producto?: { idNivelProducto?: number | null } | null;
  ultimoanioSecundariaEncuestaIni?: boolean | null;
  codigoInstitucionBac?: number | string | null;
  nombreInstSecEncuestaIni?: string | null;
  tieneEducacionSuperiorEncuestaIni?: string | boolean | null;
  instruccionMadreEncuestaIni?: string | null;
  instruccionPadreEncuestaIni?: string | null;
  decisionCarreraEncuestaIni?: string | null;
  decisionUniverEncuestaIni?: string | null;
  inforOtrasAntesEncuestaIni?: string | boolean | null;
  nivelDecisionEncuestaIni?: boolean | null;
  asesoramientoOrtEncuestaIni?: string | boolean | null;
  vistaSitioWebOrtEncuestaIni?: string | boolean | null;
  vistaInstalacionesOrtEncuestaIni?: string | boolean | null;
  publicidadOrtEncuestaIni?: string | boolean | null;
}

export interface InscripcionInitialSurveyResponse {
  tieneDerechoEncuesta?: boolean;
  encuesta?: InscripcionBackendSurvey | null;
  opcionesMotivosSeleccionados?: Array<{ idMotivo?: number; nombreMotivo?: string | null }> | null;
}

export interface InscripcionInitialSurveyPayload {
  idProducto: number | null;
  idProceso: number | null;
  ultimoAnioSecundaria: number | null;
  instruccionPadre: number | null;
  instruccionMadre: number | null;
  decisionCarrera: number | null;
  decisionUniversidad: number | null;
  infoOtrasUniversidadesAntes: string | null;
  compartidoCon: number | null;
  tieneEducacionSuperior: boolean | null;
  nivelDecision: number | null;
  asesoramientoOrt: boolean | null;
  vistaSitioWebOrt: boolean | null;
  vistaInstalacionesOrt: boolean | null;
  publicidadOrt: boolean | null;
  trabajaActualmente: string | null;
  opcionesMotivosSeleccionados: Array<{ idMotivo: number; nombreMotivo: string }> | null;
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
  aceptoReglamentoEstudiantil?: boolean;
  fechaAceptacion?: string | null;
}

export interface InscripcionPreEnrollmentResponse {
  confirmada?: boolean | null;
  fechaVencimientoPago?: string | null;
  seniaInscripcion?: number | null;
  resumen?: {
    carrera?: string | null;
    comienzo?: string | null;
    turno?: string | null;
  } | null;
}
export interface OpcionInscripcion {
  value: string;
  label: string;
  icon?: string;
  hint?: string;
}

export interface MetadatosPasoInscripcion {
  number: 1 | 2 | 3;
  supportLabel: string;
}

export interface PasoInscripcion {
  id: 'propuesta' | 'encuesta' | 'pago';
  title: string;
  overline: string;
  status: 'completo' | 'actual' | 'pendiente';
}

export interface FormularioPropuesta {
  tipoPropuesta: FormControl<string>;
  carrera: FormControl<string>;
  comienzo: FormControl<string>;
  turno: FormControl<string>;
}

export interface FormularioEducacion {
  cursaSecundaria: FormControl<string>;
  lugarSecundaria: FormControl<string>;
  estadoEducacionSuperior: FormControl<string>;
  formacionMadre: FormControl<string>;
  formacionPadre: FormControl<string>;
}

export interface FormularioDecisionAcademica {
  anioDecisionCarrera: FormControl<string>;
  apoyoDecision: FormControl<string>;
  anioDecisionOrt: FormControl<string>;
  otrasUniversidades: FormControl<string>;
  certezaDecision: FormControl<string>;
  motivosOrt: FormControl<string>;
}

export interface FormularioExperienciaOrt {
  reunionAsesoramiento: FormControl<string>;
  visitoWeb: FormControl<string>;
  visitoSede: FormControl<string>;
  recuerdaPublicidad: FormControl<string>;
}

export interface FormularioSituacionLaboral {
  situacionLaboral: FormControl<string>;
}

export interface FormularioIdentidad {
  vencimientoDocumento: FormControl<Date | null>;
}

export interface FormularioReglamento {
  aceptaReglamento: FormControl<boolean>;
}

export interface FormularioPago {
  metodoPago: FormControl<MetodoPago | ''>;
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
    lugarSecundaria: string;
    estadoEducacionSuperior: string;
    formacionMadre: string;
    formacionPadre: string;
  };
  decisionAcademica: {
    anioDecisionCarrera: string;
    apoyoDecision: string;
    anioDecisionOrt: string;
    otrasUniversidades: string;
    certezaDecision: string;
    motivosOrt: string;
  };
  experienciaOrt: {
    reunionAsesoramiento: string;
    visitoWeb: string;
    visitoSede: string;
    recuerdaPublicidad: string;
  };
  situacionLaboral: {
    situacionLaboral: string;
  };
}

export interface BorradorInscripcion {
  version: 1;
  escenario: EscenarioInscripcion;
  pantalla: Exclude<
    PantallaInscripcion,
    'procesando' | 'reserva' | 'inscripcion-confirmada' | 'inscripcion-en-proceso'
  >;
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
}

export interface InstruccionReserva {
  title: string;
  description: string;
  items: readonly string[];
  help: string;
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

export const METADATOS_PASOS_INSCRIPCION: Record<
  'propuesta' | 'encuesta' | 'pago',
  MetadatosPasoInscripcion
> = {
  propuesta: {
    number: 1,
    supportLabel: 'Paso 1 de 3 - Propuesta académica',
  },
  encuesta: {
    number: 2,
    supportLabel: 'Paso 2 de 3 - Información personal',
  },
  pago: {
    number: 3,
    supportLabel: 'Paso 3 de 3 - Confirmación',
  },
};
