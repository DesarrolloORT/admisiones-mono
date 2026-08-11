import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, type Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { ApiHttpClient } from '../../../shared/api/core/api-http-client';
import {
  getEnrollmentsDetailsEndpoint,
  getEnrollmentsInitialSurveyEndpoint,
  getEnrollmentsStudentRegulationsEndpoint,
  postEnrollmentsConfirmPreEnrollmentEndpoint,
  postEnrollmentsInitialSurveyEndpoint,
  postEnrollmentsProductInterestEndpoint,
  postEnrollmentsReactivateEndpoint,
  postEnrollmentsStartPaymentEndpoint,
} from '../../../shared/api/generated/endpoints/enrollments.endpoints';
import {
  getPersonIdentityDocumentEndpoint,
  getPersonPhotoEndpoint,
  postPersonIdentityDocumentEndpoint,
  postPersonPhotoEndpoint,
} from '../../../shared/api/generated/endpoints/person.endpoints';
import type { InscripcionInitialSurveyPayload } from '../models/inscription-flow';
import { InscripcionesEndpoint } from './inscriptions.endpoint';

describe('InscripcionesEndpoint', () => {
  let endpoint: InscripcionesEndpoint;
  let apiMock: {
    request: ReturnType<typeof vi.fn>;
    clearCache: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    apiMock = {
      request: vi.fn().mockReturnValue(of(true)),
      clearCache: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [InscripcionesEndpoint, { provide: ApiHttpClient, useValue: apiMock }],
    });
    endpoint = TestBed.inject(InscripcionesEndpoint);
  });

  it('maps inscription detail without exposing generated contracts', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'Confirmada',
        confirmed: {
          personId: 397654,
          productId: 20,
          degreeProgram: 'Sistemas',
          academicCoordinator: { name: 'Ana Coordinadora', email: 'ana@example.com' },
          enrollments: [{ firstSemesterSubjects: [{ subjectId: 1, name: 'Programación' }, {}] }],
        },
      })
    );

    await expect(firstValueFrom(endpoint.getDetail(20, 200, 'Confirmada'))).resolves.toEqual({
      estado: 'Confirmada',
      detalle: null,
      intereses: [],
      pagoPendiente: null,
      seniaMinima: null,
      confirmada: {
        numeroEstudiante: 397654,
        resumen: {
          idOferta: null,
          idProducto: 20,
          carrera: 'Sistemas',
          comienzo: null,
          turno: null,
        },
        coordinadorAcademico: { nombre: 'Ana Coordinadora', email: 'ana@example.com' },
        coordinadorCursos: null,
        inscripciones: [
          {
            idInscripcion: null,
            idOferta: null,
            comienzo: null,
            turno: null,
            materiasPrimerSemestre: [
              { idMateria: 1, nombre: 'Programación' },
              { idMateria: null, nombre: null },
            ],
          },
        ],
      },
    });
    expect(apiMock.request).toHaveBeenCalledWith(getEnrollmentsDetailsEndpoint, {
      queryParams: { productId: 20, admissionProcessId: 200, status: 'Confirmada' },
      cache: false,
      showLoader: true,
    });
  });

  it('omits the status query param when no estado is known', async () => {
    apiMock.request.mockReturnValueOnce(of({}));

    await firstValueFrom(endpoint.getDetail(20, 200));

    expect(apiMock.request).toHaveBeenCalledWith(getEnrollmentsDetailsEndpoint, {
      queryParams: { productId: 20, admissionProcessId: 200 },
      cache: false,
      showLoader: true,
    });
  });

  it('maps every interest offering of an in-progress detail', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'En proceso',
        inProgress: {
          summary: { productId: 40, degreeProgram: 'Asesoramiento financiero' },
          interests: [
            {
              offeringId: 310,
              offeringDescription: 'Marco legal',
              intake: 'Abril',
              shift: 'Noche',
            },
            { offeringId: 311, offeringDescription: 'Renta fija' },
          ],
        },
      })
    );

    const detail = await firstValueFrom(endpoint.getDetail(40, 210));

    // El resumen toma la primera oferta; `intereses` conserva todas (una por seminario).
    expect(detail.detalle).toEqual({
      idOferta: 310,
      idProducto: 40,
      carrera: 'Asesoramiento financiero',
      comienzo: 'Abril',
      turno: 'Noche',
    });
    expect(detail.intereses).toEqual([
      {
        idInscripcion: null,
        idOferta: 310,
        nombre: 'Marco legal',
        comienzo: 'Abril',
        turno: 'Noche',
      },
      { idInscripcion: null, idOferta: 311, nombre: 'Renta fija', comienzo: null, turno: null },
    ]);
  });

  it('maps the course coordinator alongside the academic coordinator', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'Confirmada',
        confirmed: {
          personId: 397654,
          academicCoordinator: { name: 'Ana Coordinadora', email: 'ana@example.com' },
          courseCoordinator: { name: 'Beto Cursos', email: 'beto@example.com' },
        },
      })
    );

    await expect(firstValueFrom(endpoint.getDetail(20, 200))).resolves.toEqual(
      expect.objectContaining({
        confirmada: expect.objectContaining({
          coordinadorAcademico: { nombre: 'Ana Coordinadora', email: 'ana@example.com' },
          coordinadorCursos: { nombre: 'Beto Cursos', email: 'beto@example.com' },
        }),
      })
    );
  });

  it('maps the seniaMinima block when the payment method was already chosen', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'Pago pendiente',
        minimumDeposit: {
          paymentType: 'ABITAB',
          documentNumber: '12345678',
          personId: 555,
          depositAmount: 3339,
        },
      })
    );

    await expect(firstValueFrom(endpoint.getDetail(719, 1398))).resolves.toEqual(
      expect.objectContaining({
        estado: 'Pago pendiente',
        pagoPendiente: null,
        seniaMinima: {
          metodoPago: 'ABITAB',
          cedula: '12345678',
          codigoPersona: 555,
          senia: 3339,
        },
      })
    );
  });

  it('maps pending payment account balance from inscription detail', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'Pago pendiente',
        pendingPayment: {
          enrollments: [{ enrollmentId: 1072704, paymentDueDate: '2026-06-26T16:29:20' }],
          depositAmount: 3339,
          currentAccount: { currentBalance: 70000 },
          summary: { degreeProgram: 'Arquitectura' },
        },
      })
    );

    await expect(firstValueFrom(endpoint.getDetail(719, 1398))).resolves.toEqual(
      expect.objectContaining({
        pagoPendiente: expect.objectContaining({
          senia: 3339,
          saldoCuenta: 70000,
        }),
      })
    );
  });

  it('maps the Actualización profesional seminarios array from a pending payment detail', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        status: 'Pago pendiente',
        pendingPayment: {
          enrollments: [
            {
              enrollmentId: 1072704,
              offeringId: 58563,
              intake: 'Marzo',
              shift: 'Matutino',
              offeringDescription: 'Seminario de Liderazgo',
            },
            {
              enrollmentId: 1072705,
              offeringId: 58564,
              intake: 'Abril',
              shift: 'Nocturno',
              offeringDescription: 'Seminario de Finanzas',
            },
          ],
          depositAmount: 3339,
          currentAccount: { currentBalance: 70000 },
          summary: { degreeProgram: 'Actualización profesional' },
        },
      })
    );

    await expect(firstValueFrom(endpoint.getDetail(719, 1398))).resolves.toEqual(
      expect.objectContaining({
        pagoPendiente: expect.objectContaining({
          seminarios: [
            {
              idInscripcion: 1072704,
              idOferta: 58563,
              nombre: 'Seminario de Liderazgo',
              comienzo: 'Marzo',
              turno: 'Matutino',
            },
            {
              idInscripcion: 1072705,
              idOferta: 58564,
              nombre: 'Seminario de Finanzas',
              comienzo: 'Abril',
              turno: 'Nocturno',
            },
          ],
        }),
      })
    );
  });
  it('maps identity document fields to the feature contract', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        front: { content: 'front', fileName: 'front.png' },
        back: {},
        expirationDate: '2030-02-04',
      })
    );

    await expect(firstValueFrom(endpoint.getIdentityDocument())).resolves.toEqual({
      frente: { archivo: 'front', nombreArchivo: 'front.png' },
      dorso: { archivo: null, nombreArchivo: null },
      fechaVencimiento: '2030-02-04',
    });
    expect(apiMock.request).toHaveBeenCalledWith(getPersonIdentityDocumentEndpoint, {
      cache: false,
    });
  });

  it('loads the identity photo as a blob without using the GET cache', () => {
    endpoint.getIdentityPhoto().subscribe();

    expect(apiMock.request).toHaveBeenCalledWith(getPersonPhotoEndpoint, {
      cache: false,
      responseType: 'blob',
    });
  });

  it('uploads identity document files and clears API cache', async () => {
    const payload = {
      fecha: '2030-02-04',
      frente: { nombreArchivo: 'frente.png', archivo: 'front' },
      dorso: { nombreArchivo: 'dorso.png', archivo: 'back' },
    };

    await expect(firstValueFrom(endpoint.uploadIdentityDocument(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postPersonIdentityDocumentEndpoint, {
      body: {
        expirationDate: '2030-02-04',
        front: { fileName: 'frente.png', content: 'front' },
        back: { fileName: 'dorso.png', content: 'back' },
      },
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('uploads identity photo and clears API cache', async () => {
    const payload = { archivoAdjunto: { nombreArchivo: 'selfie.png', archivo: 'photo' } };

    await expect(firstValueFrom(endpoint.uploadIdentityPhoto(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postPersonPhotoEndpoint, {
      body: { file: { fileName: 'selfie.png', content: 'photo' } },
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });
  it('maps the initial survey to the feature contract', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        canAnswerSurvey: true,
        survey: {
          idEncuestaIni: 1,
          degreeProgramId: 20,
          admissionProcessId: 200,
          status: 'completa',
          currentlyInSecondary: true,
          repeatsHighSchoolYear: true,
          highSchoolYearRepeatCount: 2,
          previousHigherEducationId: 1,
          decisionLevelId: 1,
          consideredUniversityIds: [10],
          consideredUniversityOthers: ['Otra consultada'],
          higherEducationUniversityIds: [20],
          higherEducationUniversityOthers: ['Otra superior'],
          ortChoiceReasonIds: [5],
          ortAdvertisingIds: [7],
        },
      })
    );

    const result = await firstValueFrom(endpoint.getInitialSurvey());

    expect(result).toEqual(
      expect.objectContaining({
        tieneDerechoEncuesta: true,
        encuesta: expect.objectContaining({
          carreraId: 20,
          comienzoId: 200,
          completa: true,
          cursaSecundaria: true,
          recursaAnioBachillerato: true,
          vecesRecursaAnioBachillerato: 2,
          estadoEducacionSuperiorPreviaId: 1,
          nivelDecisionId: 1,
        }),
        universidadesConsideradas: [10],
        universidadesConsideradasOtros: ['Otra consultada'],
        universidadesEducacionSuperior: [20],
        universidadesEducacionSuperiorOtros: ['Otra superior'],
        opcionesMotivosSeleccionados: [5],
        opcionesPublicidadSeleccionadas: [7],
      })
    );
    expect(apiMock.request).toHaveBeenCalledWith(getEnrollmentsInitialSurveyEndpoint, {
      cache: false,
    });
  });
  it('preserves the regulation acceptance date and normalizes missing values', async () => {
    apiMock.request.mockReturnValueOnce(
      of({ acceptedStudentRegulations: true, acceptanceDate: '2026-06-01' })
    );

    await expect(firstValueFrom(endpoint.getStudentRegulationAcceptance())).resolves.toEqual({
      aceptoReglamentoEstudiantil: true,
      fechaAceptacion: '2026-06-01',
    });

    apiMock.request.mockReturnValueOnce(of({}));
    await expect(firstValueFrom(endpoint.getStudentRegulationAcceptance())).resolves.toEqual({
      aceptoReglamentoEstudiantil: false,
      fechaAceptacion: null,
    });
    expect(apiMock.request).toHaveBeenCalledWith(getEnrollmentsStudentRegulationsEndpoint, {
      cache: false,
    });
  });

  it('maps the survey payload and invalidates cached API responses', async () => {
    const payload: InscripcionInitialSurveyPayload = {
      carreraId: 20,
      comienzoId: 200,
      orientacionBachilleratoId: null,
      anioBachillerato: null,
      cursaSecundariaActualmente: null,
      vecesRecursaAnioBachillerato: null,
      recursaAnioBachillerato: null,
      nivelFormacionPadreTutorId: null,
      nivelFormacionMadreTutorId: null,
      anioDecisionCarreraId: null,
      anioDecisionOrtId: null,
      seInformoEnOtrasUniversidades: null,
      informacionOtrasUniversidadesLinea1: null,
      informacionOtrasUniversidadesLinea2: null,
      apoyoDecisionId: null,
      institucionSecundariaId: null,
      nombreInstitucionSecundaria: null,
      ubicacionUltimoAnioSecundariaId: null,
      estadoEducacionSuperiorPreviaId: null,
      nivelDecisionId: null,
      tuvoAsesoramientoOrt: null,
      valoracionAsesoramientoOrtId: null,
      visitoSitioWebOrt: null,
      valoracionSitioWebOrtId: null,
      visitoInstalacionesOrt: null,
      valoracionInstalacionesOrtId: null,
      recuerdaPublicidadOrt: null,
      madreTutorEgresadoOrt: null,
      padreTutorEgresadoOrt: null,
      universidadConsideradaIds: null,
      universidadConsideradaOtros: null,
      universidadEducacionSuperiorIds: null,
      universidadEducacionSuperiorOtros: null,
      publicidadOrtIds: null,
      motivoEleccionOrtIds: null,
    };

    await expect(firstValueFrom(endpoint.saveInitialSurvey(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsInitialSurveyEndpoint, {
      body: expect.objectContaining({
        degreeProgramId: 20,
        admissionProcessId: 200,
        currentlyInSecondary: null,
        consideredUniversityOthers: null,
        higherEducationUniversityOthers: null,
      }),
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('maps pre-enrollment response and invalidates cached API responses', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        confirmed: true,
        waiting: true,
        enrollmentId: null,
        paymentDueDate: null,
        depositAmount: 0,
        currentAccount: { currentBalance: 70000 },
        summary: { degreeProgram: 'Sistemas' },
        enrollments: [{ intake: 'Marzo', shift: 'Matutino' }],
      })
    );
    const payload = {
      aceptoReglamento: true,
      esInscripcionCorporativa: true,
      idOfertasSeleccionadas: [300],
    };

    await expect(firstValueFrom(endpoint.confirmPreEnrollment(payload))).resolves.toEqual({
      confirmada: true,
      enEspera: true,
      idInscripcion: null,
      fechaVencimientoPago: null,
      seniaInscripcion: 0,
      saldoCuenta: 70000,
      resumen: { carrera: 'Sistemas', comienzo: 'Marzo', turno: 'Matutino' },
      seminarios: [
        { idInscripcion: null, idOferta: null, nombre: null, comienzo: 'Marzo', turno: 'Matutino' },
      ],
    });
    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsConfirmPreEnrollmentEndpoint, {
      body: {
        acceptedRegulations: true,
        isCorporateEnrollment: true,
        selectedOfferingIds: [300],
      },
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('maps the Actualización profesional seminarios array from confirmarPreInscripcion', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        confirmed: false,
        summary: { degreeProgram: 'Actualización profesional' },
        enrollments: [
          {
            enrollmentId: 1072704,
            offeringId: 58563,
            intake: 'Marzo',
            shift: 'Matutino',
            offeringDescription: 'Seminario de Liderazgo',
          },
          {
            enrollmentId: 1072705,
            offeringId: 58564,
            intake: 'Abril',
            shift: 'Nocturno',
            offeringDescription: 'Seminario de Finanzas',
          },
        ],
      })
    );

    const response = await firstValueFrom(
      endpoint.confirmPreEnrollment({
        aceptoReglamento: true,
        esInscripcionCorporativa: false,
        idOfertasSeleccionadas: [58563, 58564],
      })
    );

    expect(response.seminarios).toEqual([
      {
        idInscripcion: 1072704,
        idOferta: 58563,
        nombre: 'Seminario de Liderazgo',
        comienzo: 'Marzo',
        turno: 'Matutino',
      },
      {
        idInscripcion: 1072705,
        idOferta: 58564,
        nombre: 'Seminario de Finanzas',
        comienzo: 'Abril',
        turno: 'Nocturno',
      },
    ]);
  });

  it('maps reactivation with the pre-enrollment contract and invalidates cache', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        confirmed: false,
        waiting: false,
        depositAmount: 15500,
        currentAccount: { currentBalance: 1200 },
        summary: { degreeProgram: 'Actualización profesional', paymentDueDate: '2027-03-04' },
        enrollments: [
          {
            enrollmentId: 1072704,
            offeringId: 58563,
            intake: 'Marzo',
            shift: 'Matutino',
            offeringDescription: 'Seminario de Liderazgo',
          },
        ],
      })
    );

    await expect(firstValueFrom(endpoint.reactivate([100, 101]))).resolves.toEqual({
      confirmada: false,
      enEspera: false,
      idInscripcion: 1072704,
      fechaVencimientoPago: '2027-03-04',
      seniaInscripcion: 15500,
      saldoCuenta: 1200,
      resumen: {
        carrera: 'Actualización profesional',
        comienzo: 'Marzo',
        turno: 'Matutino',
      },
      seminarios: [
        {
          idInscripcion: 1072704,
          idOferta: 58563,
          nombre: 'Seminario de Liderazgo',
          comienzo: 'Marzo',
          turno: 'Matutino',
        },
      ],
    });
    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsReactivateEndpoint, {
      body: { enrollmentIds: [100, 101] },
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('maps bank account payment to Sistarbanc payload', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        result: 'pendiente',
        paymentUrl: 'https://pagos.example/sistarbanc',
        encryptedParameters: 'token-encriptado',
        messages: [{ key: 'factura', value: 'Creada' }],
      })
    );

    await expect(
      firstValueFrom(
        endpoint.pay({
          idsInscripcion: [1072704, 1072705],
          metodoPago: 'cuenta-bancaria',
          idBancoSistarbanc: 'brou',
        })
      )
    ).resolves.toEqual({
      success: true,
      resultado: 'pendiente',
      urlPago: 'https://pagos.example/sistarbanc',
      parametrosEncriptados: 'token-encriptado',
      mensajes: [{ clave: 'factura', valor: 'Creada' }],
      confirmada: null,
      message: null,
      errorCode: null,
    });
    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsStartPaymentEndpoint, {
      body: {
        enrollmentIds: [1072704, 1072705],
        paymentType: 'SISTARBANC',
        sistarbancBankId: 'brou',
      },
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('maps the confirmada block when the backend confirms the payment inline', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        result: 'confirmada',
        paymentUrl: null,
        encryptedParameters: null,
        messages: [],
        // La confirmada trae producto/carrera en la cabecera y comienzo/turno/materias
        // en cada inscripción confirmada.
        confirmed: {
          personId: 34692671,
          productId: 20,
          degreeProgram: 'Sistemas',
          academicCoordinator: { name: 'Ana', email: 'ana@ort.edu.uy' },
          courseCoordinator: null,
          enrollments: [
            {
              enrollmentId: 1072704,
              offeringId: 300,
              intake: 'Marzo',
              shift: 'Matutino',
              firstSemesterSubjects: [{ subjectId: 1, name: 'Cálculo' }],
            },
          ],
        },
      })
    );

    const response = await firstValueFrom(
      endpoint.pay({ idsInscripcion: [1], metodoPago: 'cuenta-personal', idBancoSistarbanc: null })
    );

    expect(response.confirmada).toEqual({
      numeroEstudiante: 34692671,
      resumen: {
        idOferta: 300,
        idProducto: 20,
        carrera: 'Sistemas',
        comienzo: 'Marzo',
        turno: 'Matutino',
      },
      coordinadorAcademico: { nombre: 'Ana', email: 'ana@ort.edu.uy' },
      coordinadorCursos: null,
      inscripciones: [
        {
          idInscripcion: 1072704,
          idOferta: 300,
          comienzo: 'Marzo',
          turno: 'Matutino',
          materiasPrimerSemestre: [{ idMateria: 1, nombre: 'Cálculo' }],
        },
      ],
    });
  });

  it('maps each payment method without leaking generated contracts', async () => {
    apiMock.request.mockReturnValue(of({}));

    await firstValueFrom(
      endpoint.pay({ idsInscripcion: [1], metodoPago: 'cuenta-personal', idBancoSistarbanc: null })
    );
    await firstValueFrom(
      endpoint.pay({ idsInscripcion: [1], metodoPago: 'abitab', idBancoSistarbanc: null })
    );
    await firstValueFrom(
      endpoint.pay({ idsInscripcion: [1], metodoPago: 'paganza', idBancoSistarbanc: null })
    );
    await firstValueFrom(
      endpoint.pay({ idsInscripcion: [1], metodoPago: 'banred', idBancoSistarbanc: null })
    );
    await firstValueFrom(
      endpoint.pay({ idsInscripcion: [1], metodoPago: 'geopay', idBancoSistarbanc: null })
    );

    expect(apiMock.request).toHaveBeenNthCalledWith(
      1,
      postEnrollmentsStartPaymentEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ paymentType: 'CUENTA_PERSONAL' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      2,
      postEnrollmentsStartPaymentEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ paymentType: 'ABITAB' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      3,
      postEnrollmentsStartPaymentEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ paymentType: 'PAGANZA' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      4,
      postEnrollmentsStartPaymentEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ paymentType: 'BANRED' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      5,
      postEnrollmentsStartPaymentEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ paymentType: 'GEOPAY' }) })
    );
  });
  it('maps product interest payload and boolean response', async () => {
    const payload = { idOfertas: [300], idProcesoSeleccionado: 200, idProducto: 20 };

    await expect(firstValueFrom(endpoint.registerProductInterest(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsProductInterestEndpoint, {
      body: { offeringIds: [300], admissionProcessId: 200, productId: 20 },
      showLoader: true,
    });
  });

  it('propagates HTTP errors without catching them silently', async () => {
    const failure = new HttpErrorResponse({ status: 500 });
    const operations: readonly (() => Observable<unknown>)[] = [
      () => endpoint.getDetail(20, 200),
      () => endpoint.saveInitialSurvey(createSurveyPayload()),
      () =>
        endpoint.confirmPreEnrollment({
          aceptoReglamento: true,
          esInscripcionCorporativa: false,
          idOfertasSeleccionadas: [300],
        }),
      () => endpoint.reactivate([100]),
      () => endpoint.pay({ idsInscripcion: [1], metodoPago: 'abitab', idBancoSistarbanc: null }),
    ];

    for (const operation of operations) {
      apiMock.request.mockReturnValueOnce(throwError(() => failure));
      await expect(firstValueFrom(operation())).rejects.toBe(failure);
    }
    expect(apiMock.clearCache).not.toHaveBeenCalled();
  });

  it('maps the complete survey body renaming comienzoId to procesoId', async () => {
    await expect(firstValueFrom(endpoint.saveInitialSurvey(createSurveyPayload()))).resolves.toBe(
      true
    );

    expect(apiMock.request).toHaveBeenCalledWith(postEnrollmentsInitialSurveyEndpoint, {
      body: {
        degreeProgramId: 20,
        admissionProcessId: 200,
        highSchoolTrackId: 3,
        highSchoolYear: 2025,
        currentlyInSecondary: false,
        highSchoolYearRepeatCount: 1,
        repeatsHighSchoolYear: true,
        fatherEducationLevelId: 4,
        motherEducationLevelId: 5,
        careerDecisionYearId: 6,
        ortDecisionYearId: 7,
        researchedOtherUniversities: true,
        otherUniversitiesInfoLine1: 'UCU',
        otherUniversitiesInfoLine2: 'UM',
        decisionSupportId: 8,
        secondaryInstitutionId: 9,
        secondaryInstitutionName: 'Liceo 1',
        lastSecondaryYearLocationId: 10,
        previousHigherEducationId: 11,
        decisionLevelId: 12,
        hadOrtAdvisory: true,
        ortAdvisoryRatingId: 13,
        visitedOrtWebsite: true,
        ortWebsiteRatingId: 14,
        visitedOrtFacilities: false,
        ortFacilitiesRatingId: 15,
        recallsOrtAdvertising: true,
        motherIsOrtGraduate: false,
        fatherIsOrtGraduate: true,
        consideredUniversityIds: [10, 11],
        consideredUniversityOthers: ['Otra consultada'],
        higherEducationUniversityIds: [20],
        higherEducationUniversityOthers: ['Otra superior'],
        ortAdvertisingIds: [7],
        ortChoiceReasonIds: [5],
      },
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('normalizes a detail response without estado to nulls', async () => {
    apiMock.request.mockReturnValueOnce(of({}));

    await expect(firstValueFrom(endpoint.getDetail(20, 200))).resolves.toEqual({
      estado: null,
      detalle: null,
      intereses: [],
      pagoPendiente: null,
      seniaMinima: null,
      confirmada: null,
    });
  });

  it('drops an unknown survey estado to a null active section', async () => {
    apiMock.request.mockReturnValueOnce(
      of({ canAnswerSurvey: true, survey: { status: 'en-revision' } })
    );
    const unknown = await firstValueFrom(endpoint.getInitialSurvey());

    expect(unknown.encuesta?.seccionActiva).toBeNull();
    expect(unknown.encuesta?.completa).toBe(false);

    apiMock.request.mockReturnValueOnce(
      of({ canAnswerSurvey: true, survey: { status: 'identidad' } })
    );
    const known = await firstValueFrom(endpoint.getInitialSurvey());

    expect(known.encuesta?.seccionActiva).toBe('identidad');
  });
});

function createSurveyPayload(): InscripcionInitialSurveyPayload {
  return {
    carreraId: 20,
    comienzoId: 200,
    orientacionBachilleratoId: 3,
    anioBachillerato: 2025,
    cursaSecundariaActualmente: false,
    vecesRecursaAnioBachillerato: 1,
    recursaAnioBachillerato: true,
    nivelFormacionPadreTutorId: 4,
    nivelFormacionMadreTutorId: 5,
    anioDecisionCarreraId: 6,
    anioDecisionOrtId: 7,
    seInformoEnOtrasUniversidades: true,
    informacionOtrasUniversidadesLinea1: 'UCU',
    informacionOtrasUniversidadesLinea2: 'UM',
    apoyoDecisionId: 8,
    institucionSecundariaId: 9,
    nombreInstitucionSecundaria: 'Liceo 1',
    ubicacionUltimoAnioSecundariaId: 10,
    estadoEducacionSuperiorPreviaId: 11,
    nivelDecisionId: 12,
    tuvoAsesoramientoOrt: true,
    valoracionAsesoramientoOrtId: 13,
    visitoSitioWebOrt: true,
    valoracionSitioWebOrtId: 14,
    visitoInstalacionesOrt: false,
    valoracionInstalacionesOrtId: 15,
    recuerdaPublicidadOrt: true,
    madreTutorEgresadoOrt: false,
    padreTutorEgresadoOrt: true,
    universidadConsideradaIds: [10, 11],
    universidadConsideradaOtros: ['Otra consultada'],
    universidadEducacionSuperiorIds: [20],
    universidadEducacionSuperiorOtros: ['Otra superior'],
    publicidadOrtIds: [7],
    motivoEleccionOrtIds: [5],
  };
}
