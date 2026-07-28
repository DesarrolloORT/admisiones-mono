import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, type Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { ApiHttpClient } from '../../../shared/api/core/api-http-client';
import {
  getInscripcionesDetalleEndpoint,
  getInscripcionesEncuestaInicialEndpoint,
  getInscripcionesReglamentoEstudiantilEndpoint,
  postInscripcionesConfirmarPreInscripcionEndpoint,
  postInscripcionesEncuestaInicialEndpoint,
  postInscripcionesInteresProductoEndpoint,
  postInscripcionesPagarEndpoint,
} from '../../../shared/api/generated/endpoints/inscripciones.endpoints';
import {
  getPersonaDocumentoEndpoint,
  getPersonaFotoEndpoint,
  postPersonaSubirDocumentoEndpoint,
  postPersonaSubirFotoEndpoint,
} from '../../../shared/api/generated/endpoints/persona.endpoints';
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
        estado: 'Confirmada',
        confirmada: {
          codigoPersona: 397654,
          resumen: { idProducto: 20, carrera: 'Sistemas' },
          coordinadorAcademico: { nombre: 'Ana Coordinadora', email: 'ana@example.com' },
          materiasPrimerSemestre: [{ idMateria: 1, nombre: 'Programación' }, {}],
        },
      })
    );

    await expect(firstValueFrom(endpoint.getDetail(20, 200))).resolves.toEqual({
      estado: 'Confirmada',
      detalle: null,
      pagoPendiente: null,
      seniaMinima: null,
      confirmada: {
        numeroEstudiante: 397654,
        resumen: {
          idOferta: null,
          idProducto: 20,
          carrera: 'Sistemas',
          idComienzo: null,
          comienzo: null,
          idTurno: null,
          turno: null,
        },
        coordinadorAcademico: { nombre: 'Ana Coordinadora', email: 'ana@example.com' },
        coordinadorCursos: null,
        materiasPrimerSemestre: [
          { idMateria: 1, nombre: 'Programación' },
          { idMateria: null, nombre: null },
        ],
      },
    });
    expect(apiMock.request).toHaveBeenCalledWith(getInscripcionesDetalleEndpoint, {
      queryParams: { idProducto: 20, idProceso: 200 },
      cache: false,
      showLoader: true,
    });
  });

  it('maps the course coordinator alongside the academic coordinator', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        estado: 'Confirmada',
        confirmada: {
          codigoPersona: 397654,
          coordinadorAcademico: { nombre: 'Ana Coordinadora', email: 'ana@example.com' },
          coordinadorCursos: { nombre: 'Beto Cursos', email: 'beto@example.com' },
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
        estado: 'Pago pendiente',
        reservaMinima: {
          tipoPago: 'ABITAB',
          cedula: '12345678',
          codigoPersona: 555,
          pagoReserva: 3339,
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
        estado: 'Pago pendiente',
        pagoPendiente: {
          ofertas: [{ idInscripcion: 1072704, fechaVencimientoPago: '2026-06-26T16:29:20' }],
          pagoReserva: 3339,
          estadoCuenta: { saldoActual: 70000 },
          resumen: { carrera: 'Arquitectura' },
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
  it('maps identity document fields to the feature contract', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        frente: { archivo: 'front', nombreArchivo: 'front.png' },
        dorso: {},
        fechaVencimiento: '2030-02-04',
      })
    );

    await expect(firstValueFrom(endpoint.getIdentityDocument())).resolves.toEqual({
      frente: { archivo: 'front', nombreArchivo: 'front.png' },
      dorso: { archivo: null, nombreArchivo: null },
      fechaVencimiento: '2030-02-04',
    });
    expect(apiMock.request).toHaveBeenCalledWith(getPersonaDocumentoEndpoint, { cache: false });
  });

  it('loads the identity photo as a blob without using the GET cache', () => {
    endpoint.getIdentityPhoto().subscribe();

    expect(apiMock.request).toHaveBeenCalledWith(getPersonaFotoEndpoint, {
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

    expect(apiMock.request).toHaveBeenCalledWith(postPersonaSubirDocumentoEndpoint, {
      body: payload,
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('uploads identity photo and clears API cache', async () => {
    const payload = { archivoAdjunto: { nombreArchivo: 'selfie.png', archivo: 'photo' } };

    await expect(firstValueFrom(endpoint.uploadIdentityPhoto(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postPersonaSubirFotoEndpoint, {
      body: payload,
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });
  it('maps the initial survey to the feature contract', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        tieneDerechoEncuesta: true,
        encuesta: {
          idEncuestaIni: 1,
          carreraId: 20,
          procesoId: 200,
          estado: 'completa',
          cursaSecundariaActualmente: true,
          recursaAnioBachillerato: true,
          vecesRecursaAnioBachillerato: 2,
          estadoEducacionSuperiorPreviaId: 1,
          nivelDecisionId: 1,
          universidadConsideradaIds: [10],
          universidadConsideradaOtros: ['Otra consultada'],
          universidadEducacionSuperiorIds: [20],
          universidadEducacionSuperiorOtros: ['Otra superior'],
          motivoEleccionOrtIds: [5],
          publicidadOrtIds: [7],
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
    expect(apiMock.request).toHaveBeenCalledWith(getInscripcionesEncuestaInicialEndpoint, {
      cache: false,
    });
  });
  it('preserves the regulation acceptance date and normalizes missing values', async () => {
    apiMock.request.mockReturnValueOnce(
      of({ aceptoReglamentoEstudiantil: true, fechaAceptacion: '2026-06-01' })
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
    expect(apiMock.request).toHaveBeenCalledWith(getInscripcionesReglamentoEstudiantilEndpoint, {
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

    expect(apiMock.request).toHaveBeenCalledWith(postInscripcionesEncuestaInicialEndpoint, {
      body: expect.objectContaining({
        carreraId: 20,
        procesoId: 200,
        cursaSecundariaActualmente: null,
        universidadConsideradaOtros: null,
        universidadEducacionSuperiorOtros: null,
      }),
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('maps pre-enrollment response and invalidates cached API responses', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        confirmada: true,
        enEspera: true,
        idInscripcion: null,
        fechaVencimientoPago: null,
        pagoReserva: 0,
        estadoCuenta: { saldoActual: 70000 },
        resumen: { carrera: 'Sistemas' },
        inscripciones: [{ comienzo: 'Marzo', turno: 'Matutino' }],
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
    });
    expect(apiMock.request).toHaveBeenCalledWith(postInscripcionesConfirmarPreInscripcionEndpoint, {
      body: {
        aceptoReglamento: true,
        esInscripcionCorporativa: true,
        idsOfertasSeleccionadas: [300],
      },
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('maps bank account payment to Sistarbanc payload', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        resultado: 'pendiente',
        urlPago: 'https://pagos.example/sistarbanc',
        parametrosEncriptados: 'token-encriptado',
        mensajes: [{ clave: 'factura', valor: 'Creada' }],
      })
    );

    await expect(
      firstValueFrom(
        endpoint.pay({
          idInscripcion: 1072704,
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
    expect(apiMock.request).toHaveBeenCalledWith(postInscripcionesPagarEndpoint, {
      body: {
        idInscripto: 1072704,
        tipoPago: 'SISTARBANC',
        idBancoSistarbanc: 'brou',
      },
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('maps the confirmada block when the backend confirms the payment inline', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        resultado: 'confirmada',
        urlPago: null,
        parametrosEncriptados: null,
        mensajes: [],
        confirmada: {
          codigoPersona: 34692671,
          resumen: { carrera: 'Sistemas', comienzo: 'Marzo', turno: 'Matutino' },
          coordinadorAcademico: { nombre: 'Ana', email: 'ana@ort.edu.uy' },
          coordinadorCursos: null,
          materiasPrimerSemestre: [{ idMateria: 1, nombre: 'Cálculo' }],
        },
      })
    );

    const response = await firstValueFrom(
      endpoint.pay({ idInscripcion: 1, metodoPago: 'cuenta-personal', idBancoSistarbanc: null })
    );

    expect(response.confirmada).toEqual({
      numeroEstudiante: 34692671,
      resumen: {
        idOferta: null,
        idProducto: null,
        carrera: 'Sistemas',
        idComienzo: null,
        comienzo: 'Marzo',
        idTurno: null,
        turno: 'Matutino',
      },
      coordinadorAcademico: { nombre: 'Ana', email: 'ana@ort.edu.uy' },
      coordinadorCursos: null,
      materiasPrimerSemestre: [{ idMateria: 1, nombre: 'Cálculo' }],
    });
  });

  it('maps each payment method without leaking generated contracts', async () => {
    apiMock.request.mockReturnValue(of({}));

    await firstValueFrom(
      endpoint.pay({ idInscripcion: 1, metodoPago: 'cuenta-personal', idBancoSistarbanc: null })
    );
    await firstValueFrom(
      endpoint.pay({ idInscripcion: 1, metodoPago: 'abitab', idBancoSistarbanc: null })
    );
    await firstValueFrom(
      endpoint.pay({ idInscripcion: 1, metodoPago: 'paganza', idBancoSistarbanc: null })
    );
    await firstValueFrom(
      endpoint.pay({ idInscripcion: 1, metodoPago: 'banred', idBancoSistarbanc: null })
    );
    await firstValueFrom(
      endpoint.pay({ idInscripcion: 1, metodoPago: 'geopay', idBancoSistarbanc: null })
    );

    expect(apiMock.request).toHaveBeenNthCalledWith(
      1,
      postInscripcionesPagarEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ tipoPago: 'CUENTA_PERSONAL' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      2,
      postInscripcionesPagarEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ tipoPago: 'ABITAB' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      3,
      postInscripcionesPagarEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ tipoPago: 'PAGANZA' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      4,
      postInscripcionesPagarEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ tipoPago: 'BANRED' }) })
    );
    expect(apiMock.request).toHaveBeenNthCalledWith(
      5,
      postInscripcionesPagarEndpoint,
      expect.objectContaining({ body: expect.objectContaining({ tipoPago: 'GEOPAY' }) })
    );
  });
  it('maps product interest payload and boolean response', async () => {
    const payload = { idOfertas: [300], idProcesoSeleccionado: 200, idProducto: 20 };

    await expect(firstValueFrom(endpoint.registerProductInterest(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postInscripcionesInteresProductoEndpoint, {
      body: { idsOferta: [300], idProcesoSeleccionado: 200, idProducto: 20 },
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
      () => endpoint.pay({ idInscripcion: 1, metodoPago: 'abitab', idBancoSistarbanc: null }),
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

    expect(apiMock.request).toHaveBeenCalledWith(postInscripcionesEncuestaInicialEndpoint, {
      body: {
        carreraId: 20,
        procesoId: 200,
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
      pagoPendiente: null,
      seniaMinima: null,
      confirmada: null,
    });
  });

  it('drops an unknown survey estado to a null active section', async () => {
    apiMock.request.mockReturnValueOnce(
      of({ tieneDerechoEncuesta: true, encuesta: { estado: 'en-revision' } })
    );
    const unknown = await firstValueFrom(endpoint.getInitialSurvey());

    expect(unknown.encuesta?.seccionActiva).toBeNull();
    expect(unknown.encuesta?.completa).toBe(false);

    apiMock.request.mockReturnValueOnce(
      of({ tieneDerechoEncuesta: true, encuesta: { estado: 'identidad' } })
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
