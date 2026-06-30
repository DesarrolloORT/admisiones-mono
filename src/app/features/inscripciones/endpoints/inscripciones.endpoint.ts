import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getInscripcionesDetalleEndpoint,
  getInscripcionesEncuestaInicialEndpoint,
  getInscripcionesReglamentoEstudiantilEndpoint,
  postInscripcionesConfirmarPreInscripcionEndpoint,
  postInscripcionesEncuestaInicialEndpoint,
  postInscripcionesInteresProductoEndpoint,
} from 'src/app/shared/api/generated/endpoints/inscripciones.endpoints';
import {
  getPersonaDocumentoEndpoint,
  getPersonaFotoEndpoint,
} from 'src/app/shared/api/generated/endpoints/persona.endpoints';
import type { DtoEncuestaIniAdmisionDevart } from 'src/app/shared/api/generated/models/dtoEncuestaIniAdmisionDevart';

import type { InscripcionDetail, InscripcionSummary } from '../models/inscripcion-detail';
import type {
  InscripcionConfirmPreEnrollmentPayload,
  InscripcionIdentityDocument,
  InscripcionInitialSurvey,
  InscripcionInitialSurveyPayload,
  InscripcionInitialSurveyResponse,
  InscripcionPreEnrollmentResponse,
  InscripcionProductInterestPayload,
  InscripcionStudentRegulationAcceptance,
  SeccionEncuestaId,
} from '../models/inscripcion-flow';

@Injectable({
  providedIn: 'root',
})
export class InscripcionesEndpoint {
  private readonly api = inject(ApiHttpClient);

  public getDetail(idProducto: number, idProceso: number): Observable<InscripcionDetail> {
    return this.api
      .request(getInscripcionesDetalleEndpoint, {
        queryParams: { idProducto, idProceso },
        cache: false,
        showLoader: true,
      })
      .pipe(
        map(response => ({
          estado: response.estado ?? null,
          detalle: this.toSummary(response.detalle),
          pagoPendiente: response.pagoPendiente
            ? {
                idInscripcion: response.pagoPendiente.idInscripcion ?? null,
                senia: response.pagoPendiente?.senia ?? null,
                saldoCuenta: response.pagoPendiente.estadoCuenta?.saldoActual ?? null,
                fechaVencimientoPago: response.pagoPendiente.fechaVencimientoPago ?? null,
                resumen: this.toSummary(response.pagoPendiente.resumen),
              }
            : null,
          confirmada: response.confirmada
            ? {
                numeroEstudiante: response.confirmada.numeroEstudiante ?? null,
                resumen: this.toSummary(response.confirmada.resumen),
                coordinadorAcademico: response.confirmada.coordinadorAcademico
                  ? {
                      nombre: response.confirmada.coordinadorAcademico.nombre ?? null,
                      email: response.confirmada.coordinadorAcademico.email ?? null,
                    }
                  : null,
                materiasPrimerSemestre:
                  response.confirmada.materiasPrimerSemestre?.map(materia => ({
                    idMateria: materia.idMateria ?? null,
                    nombre: materia.nombre ?? null,
                  })) ?? [],
              }
            : null,
        }))
      );
  }

  public getIdentityDocument(): Observable<InscripcionIdentityDocument> {
    return this.api.request(getPersonaDocumentoEndpoint, { cache: false }).pipe(
      map(document => ({
        frente: document.frente
          ? {
              archivo: document.frente.archivo ?? null,
              nombreArchivo: document.frente.nombreArchivo ?? null,
            }
          : null,
        dorso: document.dorso
          ? {
              archivo: document.dorso.archivo ?? null,
              nombreArchivo: document.dorso.nombreArchivo ?? null,
            }
          : null,
        fechaVencimiento: document.fechaVencimiento ?? null,
      }))
    );
  }

  public getIdentityPhoto(): Observable<Blob> {
    return this.api.request(getPersonaFotoEndpoint, { cache: false, responseType: 'blob' });
  }

  public getInitialSurvey(): Observable<InscripcionInitialSurveyResponse> {
    return this.api.request(getInscripcionesEncuestaInicialEndpoint, { cache: false }).pipe(
      map(response => ({
        tieneDerechoEncuesta: response.tieneDerechoEncuesta === true,
        encuesta: response.encuesta ? this.toInitialSurvey(response.encuesta) : null,
        universidadesConsideradas: this.toSelectedCompanyIds(response.universidadesConsideradas),
        universidadesEducacionSuperior: this.toSelectedCompanyIds(
          response.universidadesEducacionSuperior
        ),
        opcionesMotivosSeleccionados:
          response.opcionesMotivosSeleccionados?.flatMap(item => {
            const id = item.motivoOpcionesAdmision?.idMotivo ?? item.idMotivo;
            return id === undefined ? [] : [id];
          }) ?? [],
        opcionesPublicidadSeleccionadas:
          response.opcionesPublicidadSeleccionadas?.flatMap(item => {
            const id = item.publicidadOpcionesAdmision?.idPublicidad ?? item.idPublicidad;
            return id === undefined ? [] : [id];
          }) ?? [],
      }))
    );
  }

  public saveInitialSurvey(payload: InscripcionInitialSurveyPayload): Observable<boolean> {
    const body = {
      carreraId: payload.carreraId,
      comienzoId: payload.comienzoId,
      orientacionBachilleratoId: payload.orientacionBachilleratoId,
      anioBachillerato: payload.anioBachillerato,
      vecesRecursaAnioBachillerato: payload.vecesRecursaAnioBachillerato,
      recursaAnioBachillerato: payload.recursaAnioBachillerato,
      nivelFormacionPadreTutorId: payload.nivelFormacionPadreTutorId,
      nivelFormacionMadreTutorId: payload.nivelFormacionMadreTutorId,
      anioDecisionCarreraId: payload.anioDecisionCarreraId,
      anioDecisionOrtId: payload.anioDecisionOrtId,
      seInformoEnOtrasUniversidades: payload.seInformoEnOtrasUniversidades,
      informacionOtrasUniversidadesLinea1: payload.informacionOtrasUniversidadesLinea1,
      informacionOtrasUniversidadesLinea2: payload.informacionOtrasUniversidadesLinea2,
      apoyoDecisionId: payload.apoyoDecisionId,
      institucionSecundariaId: payload.institucionSecundariaId,
      autorizaInformarEncuesta: payload.autorizaInformarEncuesta,
      nombreInstitucionSecundaria: payload.nombreInstitucionSecundaria,
      ubicacionUltimoAnioSecundariaId: payload.ubicacionUltimoAnioSecundariaId,
      estadoEducacionSuperiorPreviaId: payload.estadoEducacionSuperiorPreviaId,
      nivelDecisionId: payload.nivelDecisionId,
      tuvoAsesoramientoOrt: payload.tuvoAsesoramientoOrt,
      valoracionAsesoramientoOrtId: payload.valoracionAsesoramientoOrtId,
      visitoSitioWebOrt: payload.visitoSitioWebOrt,
      valoracionSitioWebOrtId: payload.valoracionSitioWebOrtId,
      visitoInstalacionesOrt: payload.visitoInstalacionesOrt,
      valoracionInstalacionesOrtId: payload.valoracionInstalacionesOrtId,
      recuerdaPublicidadOrt: payload.recuerdaPublicidadOrt,
      madreTutorEgresadoOrt: payload.madreTutorEgresadoOrt,
      padreTutorEgresadoOrt: payload.padreTutorEgresadoOrt,
      trabajaActualmente: payload.trabajaActualmente,
      tipoJornadaId: payload.tipoJornadaId,
      universidadConsideradaIds: payload.universidadConsideradaIds,
      universidadEducacionSuperiorIds: payload.universidadEducacionSuperiorIds,
      publicidadOrtIds: payload.publicidadOrtIds,
      motivoEleccionOrtIds: payload.motivoEleccionOrtIds,
    };

    return this.api
      .request(postInscripcionesEncuestaInicialEndpoint, {
        body,
        showLoader: true,
      })
      .pipe(
        map(result => result === true),
        tap(() => this.api.clearCache())
      );
  }

  public getStudentRegulationAcceptance(): Observable<InscripcionStudentRegulationAcceptance> {
    return this.api.request(getInscripcionesReglamentoEstudiantilEndpoint, { cache: false }).pipe(
      map(acceptance => ({
        aceptoReglamentoEstudiantil: acceptance.aceptoReglamentoEstudiantil === true,
        fechaAceptacion: acceptance.fechaAceptacion ?? null,
      }))
    );
  }

  public confirmPreEnrollment(
    payload: InscripcionConfirmPreEnrollmentPayload
  ): Observable<InscripcionPreEnrollmentResponse> {
    return this.api
      .request(postInscripcionesConfirmarPreInscripcionEndpoint, {
        body: {
          aceptoReglamento: payload.aceptoReglamento,
          idOfertaSeleccionada: payload.idOfertaSeleccionada,
        },
        showLoader: true,
      })
      .pipe(
        map(response => ({
          confirmada: response.confirmada === true,
          fechaVencimientoPago: response.fechaVencimientoPago ?? null,
          seniaInscripcion: response?.senia ?? null,
          saldoCuenta: response.estadoCuenta?.saldoActual ?? null,
          resumen: response.resumen
            ? {
                carrera: response.resumen.carrera ?? null,
                comienzo: response.resumen.comienzo ?? null,
                turno: response.resumen.turno ?? null,
              }
            : null,
        })),
        tap(() => this.api.clearCache())
      );
  }

  public registerProductInterest(payload: InscripcionProductInterestPayload): Observable<boolean> {
    return this.api
      .request(postInscripcionesInteresProductoEndpoint, {
        body: {
          idOferta: payload.idOferta,
          idProcesoSeleccionado: payload.idProcesoSeleccionado,
          idProducto: payload.idProducto,
        },
        showLoader: true,
      })
      .pipe(map(result => result === true));
  }

  private toInitialSurvey(survey: DtoEncuestaIniAdmisionDevart): InscripcionInitialSurvey {
    return {
      carreraId: survey.idProducto ?? null,
      comienzoId: survey.idProceso ?? null,
      turnoId: survey.idTurno ?? null,
      nivelProductoId: survey.producto?.idNivelProducto ?? null,
      completa:
        survey.estadoEncuestaIniAdmision === 'completa' || survey.fechaProcesadoEncuestaIni != null,
      seccionActiva: toSurveySection(survey.estadoEncuestaIniAdmision),
      cursaSecundaria: survey.ultimoanioSecundariaEncuestaIni ?? null,
      orientacionBachilleratoId: survey.codigoTitulo ?? null,
      anioBachilleratoId: toNumber(survey.ultimoAnioSextoEncuestaIni),
      institucionSecundariaId: survey.codigoInstitucionBac ?? null,
      ubicacionSecundariaId: toNumber(survey.informarEncuestaIni),
      nombreInstitucionSecundaria: survey.nombreInstSecEncuestaIni ?? null,
      tieneEducacionSuperior: toBoolean(survey.tieneEducacionSuperiorEncuestaIni),
      nivelFormacionMadreId: toNumber(survey.instruccionMadreEncuestaIni),
      nivelFormacionPadreId: toNumber(survey.instruccionPadreEncuestaIni),
      madreEgresadaOrt: toBoolean(survey.instruccionMadreOrtEncuestaIni),
      padreEgresadoOrt: toBoolean(survey.instruccionPadreOrtEncuestaIni),
      anioDecisionCarreraId: toNumber(survey.decisionCarreraEncuestaIni),
      anioDecisionOrtId: toNumber(survey.decisionUniverEncuestaIni),
      seInformoEnOtrasUniversidades: toBoolean(survey.inforOtrasAntesEncuestaIni),
      apoyoPadres: toBoolean(survey.comparPadresEncuestaIni),
      apoyoOtros: toBoolean(survey.comparOtrosEncuestaIni),
      apoyoAmigosFamiliares: toBoolean(survey.comparAmigoFamEncuestaIni),
      apoyoNadie: toBoolean(survey.comparNadieEncuestaIni),
      apoyoAmigoPropuesta: toBoolean(survey.comparAmigoPropEncuestaIni),
      decisionConfirmada: survey.nivelDecisionEncuestaIni ?? null,
      tuvoAsesoramientoOrt: toBoolean(survey.asesoramientoOrtEncuestaIni),
      valoracionAsesoramientoOrt: toNumber(survey.valoracionAsesoramientoOrtEncuestaIni),
      visitoSitioWebOrt: toBoolean(survey.vistaSitioWebOrtEncuestaIni),
      valoracionSitioWebOrt: toNumber(survey.valoracionSitioWebOrtEncuestaIni),
      visitoInstalacionesOrt: toBoolean(survey.vistaInstalacionesOrtEncuestaIni),
      valoracionInstalacionesOrt: toNumber(survey.valoracionInstalacionesOrtEncuestaIni),
      recuerdaPublicidadOrt: toBoolean(survey.publicidadOrtEncuestaIni),
    };
  }

  private toSelectedCompanyIds(
    items:
      | Array<{
          codigoEmpresa?: number | null;
          nombreOtraEmpresa?: string | null;
          empresa?: { codigoEmpresa: number; nombre: string };
        }>
      | null
      | undefined
  ): number[] {
    return (items ?? []).flatMap(item => {
      const id = item.codigoEmpresa ?? item.empresa?.codigoEmpresa ?? null;
      return id === null ? [] : [id];
    });
  }

  private toSummary(
    summary:
      | {
          idOferta?: number;
          idProducto?: number;
          carrera?: string | null;
          idComienzo?: number;
          comienzo?: string | null;
          idTurno?: number;
          turno?: string | null;
        }
      | null
      | undefined
  ): InscripcionSummary | null {
    return summary
      ? {
          idOferta: summary.idOferta ?? null,
          idProducto: summary.idProducto ?? null,
          carrera: summary.carrera ?? null,
          idComienzo: summary.idComienzo ?? null,
          comienzo: summary.comienzo ?? null,
          idTurno: summary.idTurno ?? null,
          turno: summary.turno ?? null,
        }
      : null;
  }
}

function toBoolean(value: unknown): boolean | null {
  if (value === true || value === 'S') return true;
  if (value === false || value === 'N') return false;
  return null;
}

function toNumber(value: unknown): number | null {
  if (value === null || value === undefined || value === '') return null;
  const number = Number(value);
  return Number.isFinite(number) ? number : null;
}

function toSurveySection(value: string | null | undefined): SeccionEncuestaId | null {
  switch (value) {
    case 'educacion':
    case 'decision-academica':
    case 'experiencia-ort':
    case 'situacion-laboral':
    case 'identidad':
    case 'reglamento':
      return value;
    default:
      return null;
  }
}
