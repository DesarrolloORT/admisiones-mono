import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ApiHttpClient } from '../../../shared/api/core/api-http-client';
import {
  getInscripcionesEncuestaInicialEndpoint,
  postInscripcionesConfirmarPreInscripcionEndpoint,
  postInscripcionesEncuestaInicialEndpoint,
} from '../../../shared/api/generated/endpoints/inscripciones.endpoints';
import {
  getPersonaDocumentoEndpoint,
  getPersonaFotoEndpoint,
} from '../../../shared/api/generated/endpoints/persona.endpoints';
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

  it('loads identity files without using the GET cache', () => {
    endpoint.getIdentityDocument().subscribe();
    endpoint.getIdentityPhoto().subscribe();

    expect(apiMock.request).toHaveBeenNthCalledWith(1, getPersonaDocumentoEndpoint, {
      cache: false,
    });
    expect(apiMock.request).toHaveBeenNthCalledWith(2, getPersonaFotoEndpoint, {
      cache: false,
      responseType: 'blob',
    });
  });

  it('loads the initial survey without using the GET cache', () => {
    apiMock.request.mockReturnValue(of({ tieneDerechoEncuesta: true }));

    endpoint.getInitialSurvey().subscribe();

    expect(apiMock.request).toHaveBeenCalledWith(getInscripcionesEncuestaInicialEndpoint, {
      cache: false,
    });
  });

  it('saves the survey and invalidates cached API responses', () => {
    const payload = { idProducto: 20, idProceso: 200 };

    endpoint.saveInitialSurvey(payload).subscribe();

    expect(apiMock.request).toHaveBeenCalledWith(postInscripcionesEncuestaInicialEndpoint, {
      body: payload,
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });

  it('confirms pre-enrollment and invalidates cached API responses', () => {
    const payload = { aceptoReglamento: true, idOfertaSeleccionada: 300 };

    endpoint.confirmPreEnrollment(payload).subscribe();

    expect(apiMock.request).toHaveBeenCalledWith(postInscripcionesConfirmarPreInscripcionEndpoint, {
      body: payload,
      showLoader: true,
    });
    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });
});
