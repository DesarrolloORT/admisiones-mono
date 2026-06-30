import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { InscripcionesEndpoint } from '../endpoints/inscripciones.endpoint';
import { Inscripciones } from './inscripciones';

describe('Inscripciones', () => {
  let service: Inscripciones;
  let endpointMock: {
    confirmPreEnrollment: ReturnType<typeof vi.fn>;
    getDetail: ReturnType<typeof vi.fn>;
    getIdentityDocument: ReturnType<typeof vi.fn>;
    getIdentityPhoto: ReturnType<typeof vi.fn>;
    getInitialSurvey: ReturnType<typeof vi.fn>;
    getStudentRegulationAcceptance: ReturnType<typeof vi.fn>;
    saveInitialSurvey: ReturnType<typeof vi.fn>;
    registerProductInterest: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      confirmPreEnrollment: vi.fn().mockReturnValue(
        of({
          confirmada: true,
          fechaVencimientoPago: null,
          seniaInscripcion: null,
          saldoCuenta: null,
          resumen: null,
        })
      ),
      getDetail: vi
        .fn()
        .mockReturnValue(
          of({ estado: 'A la espera', detalle: null, pagoPendiente: null, confirmada: null })
        ),
      getIdentityDocument: vi.fn().mockReturnValue(of({})),
      getIdentityPhoto: vi.fn().mockReturnValue(of(new Blob())),
      getInitialSurvey: vi.fn().mockReturnValue(
        of({
          tieneDerechoEncuesta: true,
          encuesta: null,
          universidadesConsideradas: [],
          universidadesEducacionSuperior: [],
          opcionesMotivosSeleccionados: [],
          opcionesPublicidadSeleccionadas: [],
        })
      ),
      getStudentRegulationAcceptance: vi
        .fn()
        .mockReturnValue(of({ aceptoReglamentoEstudiantil: false, fechaAceptacion: null })),
      saveInitialSurvey: vi.fn().mockReturnValue(of(true)),
      registerProductInterest: vi.fn().mockReturnValue(of(true)),
    };

    TestBed.configureTestingModule({
      providers: [Inscripciones, { provide: InscripcionesEndpoint, useValue: endpointMock }],
    });
    service = TestBed.inject(Inscripciones);
  });

  it('maps identity document and photo responses to preload files', async () => {
    endpointMock.getIdentityDocument.mockReturnValueOnce(
      of({
        frente: { nombreArchivo: 'frente.png', archivo: 'aGVsbG8=' },
        dorso: {
          nombreArchivo: 'carpeta\\dorso.jpg',
          archivo: 'data:image/jpeg;base64,d29ybGQ=',
        },
        fechaVencimiento: '2030-02-04',
      })
    );
    endpointMock.getIdentityPhoto.mockReturnValueOnce(
      of(new Blob(['photo'], { type: 'image/png' }))
    );

    const preload = await firstValueFrom(service.getIdentityPreload());

    expect(preload.frente).toEqual(
      expect.objectContaining({ name: 'frente.png', size: 5, type: 'image/png' })
    );
    expect(preload.dorso).toEqual(
      expect.objectContaining({ name: 'dorso.jpg', size: 5, type: 'image/jpeg' })
    );
    expect(preload.selfie).toEqual(
      expect.objectContaining({ name: 'foto-persona.jpg', size: 5, type: 'image/png' })
    );
    expect(preload.fechaVencimiento).toBe('2030-02-04');
  });

  it('returns an empty identity preload when persona files are unavailable', async () => {
    endpointMock.getIdentityDocument.mockReturnValueOnce(
      throwError(() => new Error('document unavailable'))
    );
    endpointMock.getIdentityPhoto.mockReturnValueOnce(
      throwError(() => new Error('photo unavailable'))
    );

    await expect(firstValueFrom(service.getIdentityPreload())).resolves.toEqual({
      frente: null,
      dorso: null,
      selfie: null,
      fechaVencimiento: null,
    });
  });

  it('delegates initial survey loading and saving', () => {
    const payload = {
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

    service.getInitialSurvey().subscribe();
    service.saveInitialSurvey(payload).subscribe();

    expect(endpointMock.getInitialSurvey).toHaveBeenCalledOnce();
    expect(endpointMock.saveInitialSurvey).toHaveBeenCalledWith(payload);
  });

  it('delegates inscription detail loading', () => {
    service.getDetail(20, 200).subscribe();

    expect(endpointMock.getDetail).toHaveBeenCalledWith(20, 200);
  });

  it('delegates student regulation acceptance loading', () => {
    service.getStudentRegulationAcceptance().subscribe();

    expect(endpointMock.getStudentRegulationAcceptance).toHaveBeenCalledOnce();
  });

  it('delegates pre-enrollment confirmation', () => {
    const payload = { aceptoReglamento: true, idOfertaSeleccionada: 300 };

    service.confirmPreEnrollment(payload).subscribe();

    expect(endpointMock.confirmPreEnrollment).toHaveBeenCalledWith(payload);
  });
});
