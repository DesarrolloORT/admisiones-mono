import { FormControl } from '@angular/forms';

import type { AcademicProposalForm } from '../../catalogs/models/academic-proposal';
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

export interface InscripcionBackendSurvey {
  idProducto?: number | null;
  idProceso?: number | null;
  idTurno?: number | null;
  estadoEncuestaIniAdmision?: string | null;
  fechaProcesadoEncuestaIni?: string | null;
  producto?: { idNivelProducto?: number | null } | null;
  ultimoanioSecundariaEncuestaIni?: boolean | null;
  codigoTitulo?: number | null;
  ultimoAnioSextoEncuestaIni?: string | null;
  codigoInstitucionBac?: number | string | null;
  informarEncuestaIni?: string | null;
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
  tieneDerechoEncuesta: boolean;
  encuesta: InscripcionBackendSurvey | null;
  opcionesMotivosSeleccionados: Array<{
    idMotivo: number;
    nombreMotivo: string | null;
  }> | null;
}

export interface InscripcionInitialSurveyPayload {
  idProducto: number | null;
  idProceso: number | null;
  ultimoAnioSecundaria: number | null;
  codigoTitulo: number | null;
  ultimoAnioSexto: number | null;
  codigoInstitucionBac: number | null;
  informarEncuesta: string | null;
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
  trabajaActualmente: boolean | null;
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
  aceptoReglamentoEstudiantil: boolean;
  fechaAceptacion: string | null;
}

export interface InscripcionPreEnrollmentResponse {
  confirmada: boolean;
  fechaVencimientoPago: string | null;
  seniaInscripcion: number | null;
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

export type FormularioPropuesta = AcademicProposalForm;

export interface FormularioEducacion {
  cursaSecundaria: FormControl<string>;
  anioSecundaria: FormControl<string>;
  tipoBachillerato: FormControl<string>;
  orientacion: FormControl<string>;
  lugarSecundaria: FormControl<string>;
  departamento: FormControl<string>;
  institucionEducativa: FormControl<string>;
  estadoEducacionSuperior: FormControl<string>;
  formacionMadre: FormControl<string>;
  tituloOrtMadre: FormControl<string>;
  formacionPadre: FormControl<string>;
}

export interface FormularioDecisionAcademica {
  anioDecisionCarrera: FormControl<string>;
  apoyoDecision: FormControl<string[]>;
  anioDecisionOrt: FormControl<string>;
  otrasUniversidades: FormControl<string>;
  universidadesInformadas: FormControl<string[]>;
  certezaDecision: FormControl<string>;
  motivosOrt: FormControl<string[]>;
}

export interface FormularioExperienciaOrt {
  reunionAsesoramiento: FormControl<string>;
  calificacionAsesoramiento: FormControl<number | null>;
  visitoWeb: FormControl<string>;
  calificacionWeb: FormControl<number | null>;
  visitoSede: FormControl<string>;
  recuerdaPublicidad: FormControl<string>;
  mediosPublicidad: FormControl<string[]>;
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
  banco: FormControl<string>;
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
    formacionMadre: string;
    tituloOrtMadre: string;
    formacionPadre: string;
  };
  decisionAcademica: {
    anioDecisionCarrera: string;
    apoyoDecision: string[];
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
    recuerdaPublicidad: string;
    mediosPublicidad: string[];
  };
  situacionLaboral: {
    situacionLaboral: string;
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
