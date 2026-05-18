import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client.service';
import {
  getCatalogosPaisesEstadosCiudadesEndpoint,
  getCatalogosTiposDocumentosEndpoint,
} from 'src/app/shared/api/endpoints/generated/catalogos.endpoints';
import { vi } from 'vitest';

import { Catalogs } from './catalogs';

describe('Catalogs', () => {
  let service: Catalogs;
  let apiMock: {
    list: ReturnType<typeof vi.fn>;
    clearCache: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    apiMock = {
      list: vi.fn(),
      clearCache: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [Catalogs, { provide: ApiHttpClient, useValue: apiMock }],
    });

    service = TestBed.inject(Catalogs);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should fetch and map document types with the generated endpoint', () => {
    apiMock.list.mockReturnValue(of([{ id: 1, label: 'Cédula', code: 'CI' }]));

    service.getDocumentTypes().subscribe(data => {
      expect(data).toEqual([{ id: 1, label: 'Cédula', code: 'CI' }]);
    });

    expect(apiMock.list).toHaveBeenCalledWith(
      getCatalogosTiposDocumentosEndpoint,
      expect.any(Function)
    );
    expect(
      apiMock.list.mock.calls[0][1]({
        codTipoDocumento: 1,
        descripcion: 'Cédula',
        descrTd: 'CI',
      })
    ).toEqual({ id: 1, label: 'Cédula', code: 'CI' });
  });

  it('should fetch and map countries with the generated endpoint', () => {
    apiMock.list.mockReturnValue(of([{ id: 1, label: 'Uruguay' }]));

    service.getCountries().subscribe(data => {
      expect(data).toEqual([{ id: 1, label: 'Uruguay' }]);
    });

    expect(apiMock.list).toHaveBeenCalledWith(
      getCatalogosPaisesEstadosCiudadesEndpoint,
      expect.any(Function)
    );
    expect(apiMock.list.mock.calls[0][1]({ codigoPais: 1, nombre: 'Uruguay' })).toEqual({
      id: 1,
      label: 'Uruguay',
    });
  });

  it('should clear the shared api cache', () => {
    service.clearCache();

    expect(apiMock.clearCache).toHaveBeenCalledOnce();
  });
});
