import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { CatalogsEndpoint } from '../endpoints/catalogs.endpoint';
import { Catalogs } from './catalogs';

describe('Catalogs', () => {
  let service: Catalogs;
  let endpointMock: {
    getCountries: ReturnType<typeof vi.fn>;
    getCountryLocations: ReturnType<typeof vi.fn>;
    clearCache: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      getCountries: vi.fn(),
      getCountryLocations: vi.fn(),
      clearCache: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [Catalogs, { provide: CatalogsEndpoint, useValue: endpointMock }],
    });

    service = TestBed.inject(Catalogs);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should return static document types', () => {
    service.getDocumentTypes().subscribe(data => {
      expect(data).toEqual([
        { id: 1, label: 'Cédula', code: 'CI' },
        { id: 2, label: 'Pasaporte', code: 'PS' },
        { id: 3, label: 'Documento extranjero', code: 'DE' },
      ]);
    });
  });

  it('should delegate getCountries to the endpoint', () => {
    const result = [{ id: 1, label: 'Uruguay' }];
    endpointMock.getCountries.mockReturnValue(of(result));

    service.getCountries().subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getCountries).toHaveBeenCalledOnce();
  });

  it('should delegate clearCache to the endpoint', () => {
    service.clearCache();

    expect(endpointMock.clearCache).toHaveBeenCalledOnce();
  });
});
