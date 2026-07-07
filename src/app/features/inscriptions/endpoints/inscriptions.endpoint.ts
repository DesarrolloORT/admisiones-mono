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
  postInscripcionesPagarEndpoint,
} from 'src/app/shared/api/generated/endpoints/inscripciones.endpoints';
import {
  getPersonaDocumentoEndpoint,
  getPersonaFotoEndpoint,
  postPersonaSubirDocumentoEndpoint,
  postPersonaSubirFotoEndpoint,
} from 'src/app/shared/api/generated/endpoints/persona.endpoints';
import type { DtoEncuestaInicialLectura } from 'src/app/shared/api/generated/models/dtoEncuestaInicialLectura';

import type { InscripcionDetail, InscripcionSummary } from '../models/inscription-detail';
import type {
  InscripcionConfirmPreEnrollmentPayload,
  InscripcionIdentityDocument,
  InscripcionIdentityDocumentUploadPayload,
  InscripcionIdentityPhotoUploadPayload,
  InscripcionInitialSurvey,
  InscripcionInitialSurveyPayload,
  InscripcionInitialSurveyResponse,
  InscripcionPaymentPayload,
  InscripcionPaymentResponse,
  InscripcionPreEnrollmentResponse,
  InscripcionProductInterestPayload,
  InscripcionStudentRegulationAcceptance,
  SeccionEncuestaId,
} from '../models/inscription-flow';
import { buildPaymentPayload } from '../models/inscription-flow-mappers';

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
                senia: response.pagoPendiente.senia ?? null,
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

  public uploadIdentityDocument(
    payload: InscripcionIdentityDocumentUploadPayload
  ): Observable<boolean> {
    return this.api
      .request(postPersonaSubirDocumentoEndpoint, {
        body: {
          fecha: payload.fecha,
          frente: payload.frente,
          dorso: payload.dorso,
        },
        showLoader: true,
      })
      .pipe(tap(() => this.api.clearCache()));
  }

  public uploadIdentityPhoto(payload: InscripcionIdentityPhotoUploadPayload): Observable<boolean> {
    return this.api
      .request(postPersonaSubirFotoEndpoint, {
        body: { archivoAdjunto: payload.archivoAdjunto },
        showLoader: true,
      })
      .pipe(tap(() => this.api.clearCache()));
  }

  public getInitialSurvey(): Observable<InscripcionInitialSurveyResponse> {
    return this.api.request(getInscripcionesEncuestaInicialEndpoint, { cache: false }).pipe(
      map(response => {
        const survey = response.encuesta;

        return {
          tieneDerechoEncuesta: response.tieneDerechoEncuesta === true,
          encuesta: survey ? this.toInitialSurvey(survey) : null,
          universidadesConsideradas: survey?.universidadConsideradaIds ?? [],
          universidadesConsideradasOtros: survey?.universidadConsideradaOtros ?? [],
          universidadesEducacionSuperior: survey?.universidadEducacionSuperiorIds ?? [],
          universidadesEducacionSuperiorOtros: survey?.universidadEducacionSuperiorOtros ?? [],
          opcionesMotivosSeleccionados: survey?.motivoEleccionOrtIds ?? [],
          opcionesPublicidadSeleccionadas: survey?.publicidadOrtIds ?? [],
        };
      })
    );
  }
  public saveInitialSurvey(payload: InscripcionInitialSurveyPayload): Observable<boolean> {
    const body = {
      carreraId: payload.carreraId,
      procesoId: payload.comienzoId,
      orientacionBachilleratoId: payload.orientacionBachilleratoId,
      anioBachillerato: payload.anioBachillerato,
      cursaSecundariaActualmente: payload.cursaSecundariaActualmente,
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
      universidadConsideradaOtros: payload.universidadConsideradaOtros,
      universidadEducacionSuperiorIds: payload.universidadEducacionSuperiorIds,
      universidadEducacionSuperiorOtros: payload.universidadEducacionSuperiorOtros,
      publicidadOrtIds: payload.publicidadOrtIds,
      motivoEleccionOrtIds: payload.motivoEleccionOrtIds,
    };

    return this.api
      .request(postInscripcionesEncuestaInicialEndpoint, {
        body,
        showLoader: true,
      })
      .pipe(
        map(() => true),
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
          idInscripcion: response.idInscripcion ?? null,
          fechaVencimientoPago: response.fechaVencimientoPago ?? null,
          seniaInscripcion: response.senia ?? null,
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

  public pay(payload: InscripcionPaymentPayload): Observable<InscripcionPaymentResponse> {
    const body = buildPaymentPayload(payload);
    return this.api
      .request(postInscripcionesPagarEndpoint, {
        body,
        showLoader: true,
      })
      .pipe(
        map(response => ({
          success: true,
          resultado: response.resultado ?? null,
          urlPago: response.urlPago ?? null,
          mensajes:
            response.mensajes?.map(message => ({
              clave: message.clave ?? null,
              valor: message.valor ?? null,
            })) ?? [],
          message: null,
          errorCode: null,
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
      .pipe(map(() => true));
  }

  private toInitialSurvey(survey: DtoEncuestaInicialLectura): InscripcionInitialSurvey {
    return {
      carreraId: survey.carreraId ?? null,
      comienzoId: survey.procesoId ?? null,
      turnoId: null,
      nivelProductoId: null,
      completa: survey.estado === 'completa',
      seccionActiva: toSurveySection(survey.estado),
      cursaSecundaria: survey.cursaSecundariaActualmente ?? null,
      orientacionBachilleratoId: survey.orientacionBachilleratoId ?? null,
      anioBachilleratoId: survey.anioBachillerato ?? null,
      recursaAnioBachillerato: survey.recursaAnioBachillerato ?? null,
      vecesRecursaAnioBachillerato: survey.vecesRecursaAnioBachillerato ?? null,
      institucionSecundariaId: survey.institucionSecundariaId ?? null,
      ubicacionSecundariaId: survey.ubicacionUltimoAnioSecundariaId ?? null,
      nombreInstitucionSecundaria: survey.nombreInstitucionSecundaria ?? null,
      estadoEducacionSuperiorPreviaId: survey.estadoEducacionSuperiorPreviaId ?? null,
      nivelFormacionMadreId: survey.nivelFormacionMadreTutorId ?? null,
      nivelFormacionPadreId: survey.nivelFormacionPadreTutorId ?? null,
      madreEgresadaOrt: survey.madreTutorEgresadoOrt ?? null,
      padreEgresadoOrt: survey.padreTutorEgresadoOrt ?? null,
      anioDecisionCarreraId: survey.anioDecisionCarreraId ?? null,
      anioDecisionOrtId: survey.anioDecisionOrtId ?? null,
      seInformoEnOtrasUniversidades: survey.seInformoEnOtrasUniversidades ?? null,
      apoyoDecisionId: survey.apoyoDecisionId ?? null,
      nivelDecisionId: survey.nivelDecisionId ?? null,
      tuvoAsesoramientoOrt: survey.tuvoAsesoramientoOrt ?? null,
      valoracionAsesoramientoOrt: survey.valoracionAsesoramientoOrtId ?? null,
      visitoSitioWebOrt: survey.visitoSitioWebOrt ?? null,
      valoracionSitioWebOrt: survey.valoracionSitioWebOrtId ?? null,
      visitoInstalacionesOrt: survey.visitoInstalacionesOrt ?? null,
      valoracionInstalacionesOrt: survey.valoracionInstalacionesOrtId ?? null,
      recuerdaPublicidadOrt: survey.recuerdaPublicidadOrt ?? null,
    };
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
