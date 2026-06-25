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
      getInitialSurvey: vi
        .fn()
        .mockReturnValue(
          of({ tieneDerechoEncuesta: true, encuesta: null, opcionesMotivosSeleccionados: null })
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
      idProducto: 20,
      idProceso: 200,
      ultimoAnioSecundaria: null,
      instruccionPadre: null,
      instruccionMadre: null,
      decisionCarrera: null,
      decisionUniversidad: null,
      infoOtrasUniversidadesAntes: null,
      compartidoCon: null,
      tieneEducacionSuperior: null,
      nivelDecision: null,
      asesoramientoOrt: null,
      vistaSitioWebOrt: null,
      vistaInstalacionesOrt: null,
      publicidadOrt: null,
      trabajaActualmente: null,
      opcionesMotivosSeleccionados: null,
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
