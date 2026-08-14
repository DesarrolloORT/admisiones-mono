import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getEnrollmentsDetailsEndpoint,
  getEnrollmentsInitialSurveyEndpoint,
  getEnrollmentsStudentRegulationsEndpoint,
  postEnrollmentsConfirmPreEnrollmentEndpoint,
  postEnrollmentsInitialSurveyEndpoint,
  postEnrollmentsProductInterestEndpoint,
  postEnrollmentsReactivateEndpoint,
  postEnrollmentsStartPaymentEndpoint,
} from 'src/app/shared/api/generated/endpoints/enrollments.endpoints';
import {
  getPersonIdentityDocumentEndpoint,
  getPersonPhotoEndpoint,
  postPersonIdentityDocumentEndpoint,
  postPersonPhotoEndpoint,
} from 'src/app/shared/api/generated/endpoints/person.endpoints';
import type { ConfirmedEnrollmentDetailsResponse } from 'src/app/shared/api/generated/models/confirmedEnrollmentDetailsResponse';
import type { ConfirmPreEnrollmentResponse } from 'src/app/shared/api/generated/models/confirmPreEnrollmentResponse';
import type { EnrollmentOffering } from 'src/app/shared/api/generated/models/enrollmentOffering';
import type { InitialSurveyDetails } from 'src/app/shared/api/generated/models/initialSurveyDetails';

import type {
  InscripcionConfirmedDetail,
  InscripcionCoordinador,
  InscripcionDetail,
  InscripcionSummary,
} from '../models/inscription-detail';
import type {
  InscripcionConfirmPreEnrollmentPayload,
  InscripcionIdentityDocument,
  InscripcionIdentityDocumentUploadPayload,
  InscripcionIdentityPhotoUploadPayload,
  InscripcionInitialSurvey,
  InscripcionInitialSurveyPayload,
  InscripcionInitialSurveyResponse,
  InscripcionOfertaResumen,
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

  public getDetail(
    idProducto: number,
    idProceso: number,
    estado?: string | null
  ): Observable<InscripcionDetail> {
    return this.api
      .request(getEnrollmentsDetailsEndpoint, {
        queryParams: {
          productId: idProducto,
          admissionProcessId: idProceso,
          ...(estado ? { status: estado } : {}),
        },
        showLoader: true,
      })
      .pipe(
        map(response => ({
          estado: response.status ?? null,
          detalle: this.toSummary(
            response.inProgress?.summary,
            response.inProgress?.interests?.[0]
          ),
          intereses: this.toSeminarios(response.inProgress?.interests),
          pagoPendiente: response.pendingPayment
            ? {
                idInscripcion: response.pendingPayment.enrollments?.[0]?.enrollmentId ?? null,
                senia: response.pendingPayment.depositAmount ?? null,
                saldoCuenta: response.pendingPayment.currentAccount?.currentBalance ?? null,
                fechaVencimientoPago: response.pendingPayment.summary?.paymentDueDate ?? null,
                resumen: this.toSummary(
                  response.pendingPayment.summary,
                  response.pendingPayment.enrollments?.[0]
                ),
                seminarios: this.toSeminarios(response.pendingPayment.enrollments),
              }
            : null,
          seniaMinima: response.minimumDeposit
            ? {
                metodoPago: response.minimumDeposit.paymentType ?? null,
                cedula: response.minimumDeposit.documentNumber ?? null,
                codigoPersona: response.minimumDeposit.personId ?? null,
                senia: response.minimumDeposit.depositAmount ?? null,
              }
            : null,
          confirmada: this.toConfirmedDetail(response.confirmed),
        }))
      );
  }

  public getIdentityDocument(): Observable<InscripcionIdentityDocument> {
    return this.api.request(getPersonIdentityDocumentEndpoint).pipe(
      map(document => ({
        frente: document.front
          ? {
              archivo: document.front.content ?? null,
              nombreArchivo: document.front.fileName ?? null,
            }
          : null,
        dorso: document.back
          ? {
              archivo: document.back.content ?? null,
              nombreArchivo: document.back.fileName ?? null,
            }
          : null,
        fechaVencimiento: document.expirationDate ?? null,
      }))
    );
  }

  public getIdentityPhoto(): Observable<Blob> {
    return this.api.request(getPersonPhotoEndpoint, { responseType: 'blob' });
  }

  public uploadIdentityDocument(
    payload: InscripcionIdentityDocumentUploadPayload
  ): Observable<boolean> {
    return this.api.request(postPersonIdentityDocumentEndpoint, {
      body: {
        expirationDate: payload.fecha,
        front: { fileName: payload.frente.nombreArchivo, content: payload.frente.archivo },
        back: { fileName: payload.dorso.nombreArchivo, content: payload.dorso.archivo },
      },
      showLoader: true,
    });
  }

  public uploadIdentityPhoto(payload: InscripcionIdentityPhotoUploadPayload): Observable<boolean> {
    return this.api.request(postPersonPhotoEndpoint, {
      body: {
        file: {
          fileName: payload.archivoAdjunto.nombreArchivo,
          content: payload.archivoAdjunto.archivo,
        },
      },
      showLoader: true,
    });
  }

  public getInitialSurvey(): Observable<InscripcionInitialSurveyResponse> {
    return this.api.request(getEnrollmentsInitialSurveyEndpoint).pipe(
      map(response => {
        const survey = response.survey;

        return {
          tieneDerechoEncuesta: response.canAnswerSurvey === true,
          encuesta: survey ? this.toInitialSurvey(survey) : null,
          universidadesConsideradas: survey?.consideredUniversityIds ?? [],
          universidadesConsideradasOtros: survey?.consideredUniversityOthers ?? [],
          universidadesEducacionSuperior: survey?.higherEducationUniversityIds ?? [],
          universidadesEducacionSuperiorOtros: survey?.higherEducationUniversityOthers ?? [],
          opcionesMotivosSeleccionados: survey?.ortChoiceReasonIds ?? [],
          opcionesPublicidadSeleccionadas: survey?.ortAdvertisingIds ?? [],
        };
      })
    );
  }

  public saveInitialSurvey(payload: InscripcionInitialSurveyPayload): Observable<boolean> {
    const body = {
      degreeProgramId: payload.carreraId,
      admissionProcessId: payload.comienzoId,
      highSchoolTrackId: payload.orientacionBachilleratoId,
      highSchoolYear: payload.anioBachillerato,
      currentlyInSecondary: payload.cursaSecundariaActualmente,
      highSchoolYearRepeatCount: payload.vecesRecursaAnioBachillerato,
      repeatsHighSchoolYear: payload.recursaAnioBachillerato,
      fatherEducationLevelId: payload.nivelFormacionPadreTutorId,
      motherEducationLevelId: payload.nivelFormacionMadreTutorId,
      careerDecisionYearId: payload.anioDecisionCarreraId,
      ortDecisionYearId: payload.anioDecisionOrtId,
      researchedOtherUniversities: payload.seInformoEnOtrasUniversidades,
      otherUniversitiesInfoLine1: payload.informacionOtrasUniversidadesLinea1,
      otherUniversitiesInfoLine2: payload.informacionOtrasUniversidadesLinea2,
      decisionSupportId: payload.apoyoDecisionId,
      secondaryInstitutionId: payload.institucionSecundariaId,
      secondaryInstitutionName: payload.nombreInstitucionSecundaria,
      lastSecondaryYearLocationId: payload.ubicacionUltimoAnioSecundariaId,
      previousHigherEducationId: payload.estadoEducacionSuperiorPreviaId,
      decisionLevelId: payload.nivelDecisionId,
      hadOrtAdvisory: payload.tuvoAsesoramientoOrt,
      ortAdvisoryRatingId: payload.valoracionAsesoramientoOrtId,
      visitedOrtWebsite: payload.visitoSitioWebOrt,
      ortWebsiteRatingId: payload.valoracionSitioWebOrtId,
      visitedOrtFacilities: payload.visitoInstalacionesOrt,
      ortFacilitiesRatingId: payload.valoracionInstalacionesOrtId,
      recallsOrtAdvertising: payload.recuerdaPublicidadOrt,
      motherIsOrtGraduate: payload.madreTutorEgresadoOrt,
      fatherIsOrtGraduate: payload.padreTutorEgresadoOrt,
      consideredUniversityIds: payload.universidadConsideradaIds,
      consideredUniversityOthers: payload.universidadConsideradaOtros,
      higherEducationUniversityIds: payload.universidadEducacionSuperiorIds,
      higherEducationUniversityOthers: payload.universidadEducacionSuperiorOtros,
      ortAdvertisingIds: payload.publicidadOrtIds,
      ortChoiceReasonIds: payload.motivoEleccionOrtIds,
    };

    return this.api
      .request(postEnrollmentsInitialSurveyEndpoint, {
        body,
        showLoader: true,
      })
      .pipe(map(() => true));
  }

  public getStudentRegulationAcceptance(): Observable<InscripcionStudentRegulationAcceptance> {
    return this.api.request(getEnrollmentsStudentRegulationsEndpoint).pipe(
      map(acceptance => ({
        aceptoReglamentoEstudiantil: acceptance.acceptedStudentRegulations === true,
        fechaAceptacion: acceptance.acceptanceDate ?? null,
      }))
    );
  }

  public confirmPreEnrollment(
    payload: InscripcionConfirmPreEnrollmentPayload
  ): Observable<InscripcionPreEnrollmentResponse> {
    return this.api
      .request(postEnrollmentsConfirmPreEnrollmentEndpoint, {
        body: {
          acceptedRegulations: payload.aceptoReglamento,
          isCorporateEnrollment: payload.esInscripcionCorporativa,
          selectedOfferingIds: payload.idOfertasSeleccionadas,
        },
        showLoader: true,
      })
      .pipe(map(response => this.toPreEnrollmentResponse(response)));
  }

  // Actualización profesional (nivel 3/4) da de baja el paquete entero: se reactivan
  // todas sus anotaciones en una sola llamada, igual que `Pagar` las cobra en bloque.
  public reactivate(idsInscripcion: number[]): Observable<InscripcionPreEnrollmentResponse> {
    return this.api
      .request(postEnrollmentsReactivateEndpoint, {
        body: { enrollmentIds: idsInscripcion },
        showLoader: true,
      })
      .pipe(map(response => this.toPreEnrollmentResponse(response)));
  }

  public pay(payload: InscripcionPaymentPayload): Observable<InscripcionPaymentResponse> {
    const body = buildPaymentPayload(payload);
    return this.api
      .request(postEnrollmentsStartPaymentEndpoint, {
        body,
      })
      .pipe(
        map(response => ({
          success: true,
          resultado: response.result ?? null,
          urlPago: response.paymentUrl ?? null,
          parametrosEncriptados: response.encryptedParameters ?? null,
          mensajes:
            response.messages?.map(message => ({
              clave: message.key ?? null,
              valor: message.value ?? null,
            })) ?? [],
          confirmada: this.toConfirmedDetail(response.confirmed),
          message: null,
          errorCode: null,
        }))
      );
  }

  public registerProductInterest(payload: InscripcionProductInterestPayload): Observable<boolean> {
    return this.api
      .request(postEnrollmentsProductInterestEndpoint, {
        body: {
          offeringIds: payload.idOfertas,
          admissionProcessId: payload.idProcesoSeleccionado,
          productId: payload.idProducto,
        },
        showLoader: true,
      })
      .pipe(map(() => true));
  }

  private toInitialSurvey(survey: InitialSurveyDetails): InscripcionInitialSurvey {
    return {
      carreraId: survey.degreeProgramId ?? null,
      comienzoId: survey.admissionProcessId ?? null,
      turnoId: null,
      nivelProductoId: null,
      completa: survey.status === 'completa',
      seccionActiva: toSurveySection(survey.status),
      cursaSecundaria: survey.currentlyInSecondary ?? null,
      orientacionBachilleratoId: survey.highSchoolTrackId ?? null,
      anioBachilleratoId: survey.highSchoolYear ?? null,
      recursaAnioBachillerato: survey.repeatsHighSchoolYear ?? null,
      vecesRecursaAnioBachillerato: survey.highSchoolYearRepeatCount ?? null,
      institucionSecundariaId: survey.secondaryInstitutionId ?? null,
      ubicacionSecundariaId: survey.lastSecondaryYearLocationId ?? null,
      nombreInstitucionSecundaria: survey.secondaryInstitutionName ?? null,
      estadoEducacionSuperiorPreviaId: survey.previousHigherEducationId ?? null,
      nivelFormacionMadreId: survey.motherEducationLevelId ?? null,
      nivelFormacionPadreId: survey.fatherEducationLevelId ?? null,
      madreEgresadaOrt: survey.motherIsOrtGraduate ?? null,
      padreEgresadoOrt: survey.fatherIsOrtGraduate ?? null,
      anioDecisionCarreraId: survey.careerDecisionYearId ?? null,
      anioDecisionOrtId: survey.ortDecisionYearId ?? null,
      seInformoEnOtrasUniversidades: survey.researchedOtherUniversities ?? null,
      apoyoDecisionId: survey.decisionSupportId ?? null,
      nivelDecisionId: survey.decisionLevelId ?? null,
      tuvoAsesoramientoOrt: survey.hadOrtAdvisory ?? null,
      valoracionAsesoramientoOrt: survey.ortAdvisoryRatingId ?? null,
      visitoSitioWebOrt: survey.visitedOrtWebsite ?? null,
      valoracionSitioWebOrt: survey.ortWebsiteRatingId ?? null,
      visitoInstalacionesOrt: survey.visitedOrtFacilities ?? null,
      valoracionInstalacionesOrt: survey.ortFacilitiesRatingId ?? null,
      recuerdaPublicidadOrt: survey.recallsOrtAdvertising ?? null,
    };
  }
  private toSummary(
    summary:
      | {
          offeringId?: number;
          productId?: number;
          degreeProgram?: string | null;
          intake?: string | null;
          shift?: string | null;
        }
      | null
      | undefined,
    oferta?: { offeringId?: number; intake?: string | null; shift?: string | null } | null
  ): InscripcionSummary | null {
    return summary
      ? {
          idOferta: oferta?.offeringId ?? summary.offeringId ?? null,
          idProducto: summary.productId ?? null,
          carrera: summary.degreeProgram ?? null,
          comienzo: oferta?.intake ?? summary.intake ?? null,
          turno: oferta?.shift ?? summary.shift ?? null,
        }
      : null;
  }

  // Una fila por oferta elegida (en Actualización profesional, un seminario por fila).
  // Sirve para los dos bloques: en `pagoPendiente.inscripciones` el idInscripcion es lo
  // que después se cobra en bloque vía Pagar.idsInscripcion, y en `detalle.intereses`
  // los idOferta son los que reconfirma la preinscripción al retomar.
  private toSeminarios(
    ofertas: Array<EnrollmentOffering> | null | undefined
  ): InscripcionOfertaResumen[] {
    return (ofertas ?? []).map(oferta => ({
      idInscripcion: oferta.enrollmentId ?? null,
      idOferta: oferta.offeringId ?? null,
      nombre: oferta.offeringDescription ?? null,
      comienzo: oferta.intake ?? null,
      turno: oferta.shift ?? null,
    }));
  }

  private toPreEnrollmentResponse(
    response: ConfirmPreEnrollmentResponse
  ): InscripcionPreEnrollmentResponse {
    return {
      confirmada: response.confirmed === true,
      enEspera: response.waiting === true,
      idInscripcion: response.enrollments?.[0]?.enrollmentId ?? null,
      fechaVencimientoPago: response.summary?.paymentDueDate ?? null,
      seniaInscripcion: response.depositAmount ?? null,
      saldoCuenta: response.currentAccount?.currentBalance ?? null,
      resumen: response.summary
        ? {
            carrera: response.summary.degreeProgram ?? null,
            comienzo: response.enrollments?.[0]?.intake ?? null,
            turno: response.enrollments?.[0]?.shift ?? null,
          }
        : null,
      seminarios: this.toSeminarios(response.enrollments),
    };
  }

  private toCoordinador(
    coordinador: { name?: string | null; email?: string | null } | null | undefined
  ): InscripcionCoordinador | null {
    return coordinador
      ? { nombre: coordinador.name ?? null, email: coordinador.email ?? null }
      : null;
  }

  // La confirmada ya no trae un bloque `resumen`: producto y carrera están en la
  // cabecera y comienzo/turno/materias en cada inscripción confirmada.
  private toConfirmedDetail(
    confirmada: ConfirmedEnrollmentDetailsResponse | null | undefined
  ): InscripcionConfirmedDetail | null {
    if (!confirmada) return null;

    const inscripciones = confirmada.enrollments ?? [];
    return {
      numeroEstudiante: confirmada.personId ?? null,
      resumen: this.toSummary(confirmada, inscripciones[0]),
      coordinadorAcademico: this.toCoordinador(confirmada.academicCoordinator),
      coordinadorCursos: this.toCoordinador(confirmada.courseCoordinator),
      inscripciones: inscripciones.map(inscripcion => ({
        idInscripcion: inscripcion.enrollmentId ?? null,
        idOferta: inscripcion.offeringId ?? null,
        comienzo: inscripcion.intake ?? null,
        turno: inscripcion.shift ?? null,
        materiasPrimerSemestre: (inscripcion.firstSemesterSubjects ?? []).map(materia => ({
          idMateria: materia.subjectId ?? null,
          nombre: materia.name ?? null,
        })),
      })),
    };
  }
}

function toSurveySection(value: string | null | undefined): SeccionEncuestaId | null {
  switch (value) {
    case 'educacion':
    case 'decision-academica':
    case 'experiencia-ort':
    case 'identidad':
    case 'reglamento':
      return value;
    default:
      return null;
  }
}
