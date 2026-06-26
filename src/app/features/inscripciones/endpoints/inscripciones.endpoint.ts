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
  InscripcionBackendSurvey,
  InscripcionConfirmPreEnrollmentPayload,
  InscripcionIdentityDocument,
  InscripcionInitialSurveyPayload,
  InscripcionInitialSurveyResponse,
  InscripcionPreEnrollmentResponse,
  InscripcionProductInterestPayload,
  InscripcionStudentRegulationAcceptance,
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
                senia: response.pagoPendiente.senia ?? null,
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
        opcionesMotivosSeleccionados:
          response.opcionesMotivosSeleccionados?.map(item => ({
            idMotivo: item.motivoOpcionesAdmision?.idMotivo ?? item.idMotivo,
            nombreMotivo: item.motivoOpcionesAdmision?.nombreMotivo ?? null,
          })) ?? null,
      }))
    );
  }

  public saveInitialSurvey(payload: InscripcionInitialSurveyPayload): Observable<boolean> {
    return this.api
      .request(postInscripcionesEncuestaInicialEndpoint, {
        body: {
          idProducto: payload.idProducto,
          idProceso: payload.idProceso,
          ultimoAnioSecundaria: payload.ultimoAnioSecundaria,
          codigoTitulo: payload.codigoTitulo,
          ultimoAnioSexto: payload.ultimoAnioSexto,
          codigoInstitucionBac: payload.codigoInstitucionBac,
          informarEncuesta: payload.informarEncuesta,
          instruccionPadre: payload.instruccionPadre,
          instruccionMadre: payload.instruccionMadre,
          decisionCarrera: payload.decisionCarrera,
          decisionUniversidad: payload.decisionUniversidad,
          infoOtrasUniversidadesAntes: payload.infoOtrasUniversidadesAntes,
          compartidoCon: payload.compartidoCon,
          tieneEducacionSuperior: payload.tieneEducacionSuperior,
          nivelDecision: payload.nivelDecision,
          asesoramientoOrt: payload.asesoramientoOrt,
          vistaSitioWebOrt: payload.vistaSitioWebOrt,
          vistaInstalacionesOrt: payload.vistaInstalacionesOrt,
          publicidadOrt: payload.publicidadOrt,
          trabajaActualmente: payload.trabajaActualmente,
          opcionesMotivosSeleccionados: payload.opcionesMotivosSeleccionados,
        },
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
          seniaInscripcion: response.seniaInscripcion ?? null,
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

  private toInitialSurvey(survey: DtoEncuestaIniAdmisionDevart): InscripcionBackendSurvey {
    return {
      idProducto: survey.idProducto ?? null,
      idProceso: survey.idProceso ?? null,
      idTurno: survey.idTurno ?? null,
      estadoEncuestaIniAdmision: survey.estadoEncuestaIniAdmision ?? null,
      fechaProcesadoEncuestaIni: survey.fechaProcesadoEncuestaIni ?? null,
      producto: survey.producto
        ? { idNivelProducto: survey.producto.idNivelProducto ?? null }
        : null,
      ultimoanioSecundariaEncuestaIni: survey.ultimoanioSecundariaEncuestaIni ?? null,
      codigoTitulo: survey.codigoTitulo ?? null,
      ultimoAnioSextoEncuestaIni: survey.ultimoAnioSextoEncuestaIni ?? null,
      codigoInstitucionBac: survey.codigoInstitucionBac ?? null,
      informarEncuestaIni: survey.informarEncuestaIni ?? null,
      nombreInstSecEncuestaIni: survey.nombreInstSecEncuestaIni ?? null,
      tieneEducacionSuperiorEncuestaIni: survey.tieneEducacionSuperiorEncuestaIni ?? null,
      instruccionMadreEncuestaIni: survey.instruccionMadreEncuestaIni ?? null,
      instruccionPadreEncuestaIni: survey.instruccionPadreEncuestaIni ?? null,
      decisionCarreraEncuestaIni: survey.decisionCarreraEncuestaIni ?? null,
      decisionUniverEncuestaIni: survey.decisionUniverEncuestaIni ?? null,
      inforOtrasAntesEncuestaIni: survey.inforOtrasAntesEncuestaIni ?? null,
      nivelDecisionEncuestaIni: survey.nivelDecisionEncuestaIni ?? null,
      asesoramientoOrtEncuestaIni: survey.asesoramientoOrtEncuestaIni ?? null,
      vistaSitioWebOrtEncuestaIni: survey.vistaSitioWebOrtEncuestaIni ?? null,
      vistaInstalacionesOrtEncuestaIni: survey.vistaInstalacionesOrtEncuestaIni ?? null,
      publicidadOrtEncuestaIni: survey.publicidadOrtEncuestaIni ?? null,
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
