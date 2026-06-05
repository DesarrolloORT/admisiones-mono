import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ApiHttpClient } from '../../../shared/api/core/api-http-client';
import { CatalogsEndpoint } from './catalogs.endpoint';

describe('CatalogsEndpoint', () => {
  let endpoint: CatalogsEndpoint;
  let apiMock: {
    request: ReturnType<typeof vi.fn>;
    clearCache: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    apiMock = {
      request: vi.fn().mockReturnValue(of([])),
      clearCache: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [CatalogsEndpoint, { provide: ApiHttpClient, useValue: apiMock }],
    });

    endpoint = TestBed.inject(CatalogsEndpoint);
  });

  it('should map careers from API data', () => {
    apiMock.request.mockReturnValue(
      of([
        {
          idProducto: 20,
          idNivelProducto: 1,
          nombreProducto: 'Diseño',
          nombreNivelProducto: 'Carreras',
        },
      ])
    );

    endpoint.getCareers().subscribe(result => {
      expect(result).toEqual([
        {
          idProducto: 20,
          idNivelProducto: 1,
          nombreProducto: 'Diseño',
          nombreNivelProducto: 'Carreras',
        },
      ]);
    });
  });

  it('should delegate cache clearing', () => {
    endpoint.clearCache();

    expect(apiMock.clearCache).toHaveBeenCalled();
  });
});
