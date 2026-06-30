import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { vi } from 'vitest';

import { ApiHttpClient } from '../../../shared/api/core/api-http-client';
import {
  getInscripcionesDetalleEndpoint,
  getInscripcionesEncuestaInicialEndpoint,
  getInscripcionesReglamentoEstudiantilEndpoint,
  postInscripcionesConfirmarPreInscripcionEndpoint,
  postInscripcionesEncuestaInicialEndpoint,
  postInscripcionesInteresProductoEndpoint,
} from '../../../shared/api/generated/endpoints/inscripciones.endpoints';
import {
  getPersonaDocumentoEndpoint,
  getPersonaFotoEndpoint,
} from '../../../shared/api/generated/endpoints/persona.endpoints';
import type { InscripcionInitialSurveyPayload } from '../models/inscripcion-flow';
import { InscripcionesEndpoint } from './inscripciones.endpoint';

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
          numeroEstudiante: 397654,
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

  it('maps pending payment account balance from inscription detail', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        estado: 'Pago pendiente',
        pagoPendiente: {
          idInscripcion: 1072704,
          fechaVencimientoPago: '2026-06-26T16:29:20',
          carritos: [{ senia: 3339 }],
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

  it('maps the initial survey to the feature contract', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        tieneDerechoEncuesta: true,
        encuesta: {
          idEncuestaIni: 1,
          idProducto: 20,
          estadoEncuestaIniAdmision: 'completa',
          producto: { idNivelProducto: 4 },
        },
        universidadesConsideradas: [
          { codigoEmpresa: 10, empresa: { codigoEmpresa: 10, nombre: 'Udelar' } },
        ],
        universidadesEducacionSuperior: [{ codigoEmpresa: 20, nombreOtraEmpresa: 'Otra U' }],
        opcionesMotivosSeleccionados: [
          {
            idMotivo: 8,
            motivoOpcionesAdmision: { idMotivo: 5, nombreMotivo: 'Plan de estudios' },
          },
        ],
        opcionesPublicidadSeleccionadas: [
          {
            idPublicidad: 9,
            publicidadOpcionesAdmision: { idPublicidad: 7, nombrePublicidad: 'Redes' },
          },
        ],
      })
    );

    const result = await firstValueFrom(endpoint.getInitialSurvey());

    expect(result).toEqual(
      expect.objectContaining({
        tieneDerechoEncuesta: true,
        encuesta: expect.objectContaining({
          carreraId: 20,
          nivelProductoId: 4,
          completa: true,
        }),
        universidadesConsideradas: [10],
        universidadesEducacionSuperior: [20],
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
      autorizaInformarEncuesta: null,
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
      trabajaActualmente: null,
      tipoJornadaId: null,
      universidadConsideradaIds: null,
      universidadEducacionSuperiorIds: null,
      publicidadOrtIds: null,
      motivoEleccionOrtIds: null,
    };

    await expect(firstValueFrom(endpoint.saveInitialSurvey(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postInscripcionesEncuestaInicialEndpoint, {
      body: payload,
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('maps pre-enrollment response and invalidates cached API responses', async () => {
    apiMock.request.mockReturnValueOnce(
      of({
        confirmada: true,
        fechaVencimientoPago: '2027-04-15',
        carritos: [{ senia: 21000 }],
        estadoCuenta: { saldoActual: 70000 },
        resumen: { carrera: 'Sistemas', comienzo: 'Marzo', turno: 'Matutino' },
      })
    );
    const payload = { aceptoReglamento: true, idOfertaSeleccionada: 300 };

    await expect(firstValueFrom(endpoint.confirmPreEnrollment(payload))).resolves.toEqual({
      confirmada: true,
      fechaVencimientoPago: '2027-04-15',
      seniaInscripcion: 21000,
      saldoCuenta: 70000,
      resumen: { carrera: 'Sistemas', comienzo: 'Marzo', turno: 'Matutino' },
    });
    expect(apiMock.request).toHaveBeenCalledWith(postInscripcionesConfirmarPreInscripcionEndpoint, {
      body: payload,
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('maps product interest payload and boolean response', async () => {
    const payload = { idOferta: 300, idProcesoSeleccionado: 200, idProducto: 20 };

    await expect(firstValueFrom(endpoint.registerProductInterest(payload))).resolves.toBe(true);

    expect(apiMock.request).toHaveBeenCalledWith(postInscripcionesInteresProductoEndpoint, {
      body: payload,
      showLoader: true,
    });
  });
});
