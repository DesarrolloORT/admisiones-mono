import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { CatalogsEndpoint } from '../endpoints/catalogs.endpoint';
import { Catalogs } from './catalogs';

describe('Catalogs', () => {
  let service: Catalogs;
  let endpointMock: Record<string, ReturnType<typeof vi.fn>>;

  beforeEach(() => {
    endpointMock = {
      getCountries: vi.fn().mockReturnValue(of([{ id: 1, label: 'Uruguay' }])),
      getReasonsForChoice: vi.fn().mockReturnValue(of([{ id: 1, label: 'Recomendación' }])),
      getAdvertisingChoices: vi.fn().mockReturnValue(of([{ id: 1, label: 'Redes sociales' }])),
      getBaccalaureates: vi.fn().mockReturnValue(of([{ id: 1, label: 'Científico' }])),
      getBaccalaureateYears: vi.fn().mockReturnValue(of([{ id: 1, label: '2024', year: 2024 }])),
      getInstitutions: vi.fn().mockReturnValue(of([{ id: 1, label: 'Liceo 1' }])),
      getUniversities: vi.fn().mockReturnValue(of([{ id: 1, label: 'ORT Uruguay' }])),
      getScholarshipProducts: vi.fn().mockReturnValue(of([{ id: 1, label: 'Beca Excelencia' }])),
      getScholarshipFunds: vi.fn().mockReturnValue(of([{ id: 1, label: 'Fondo ORT' }])),
    };

    TestBed.configureTestingModule({
      providers: [Catalogs, { provide: CatalogsEndpoint, useValue: endpointMock }],
    });

    service = TestBed.inject(Catalogs);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should delegate getCountries to endpoint', () => {
    service.getCountries().subscribe(data => {
      expect(data).toEqual([{ id: 1, label: 'Uruguay' }]);
    });
    expect(endpointMock['getCountries']).toHaveBeenCalled();
  });

  it('should cache getCountries result', () => {
    service.getCountries().subscribe();
    service.getCountries().subscribe();
    service.getCountries().subscribe();

    expect(endpointMock['getCountries']).toHaveBeenCalledTimes(1);
  });

  it('should delegate getReasonsForChoice to endpoint', () => {
    service.getReasonsForChoice().subscribe(data => {
      expect(data).toEqual([{ id: 1, label: 'Recomendación' }]);
    });
    expect(endpointMock['getReasonsForChoice']).toHaveBeenCalled();
  });

  it('should delegate getAdvertisingChoices to endpoint', () => {
    service.getAdvertisingChoices().subscribe(data => {
      expect(data).toEqual([{ id: 1, label: 'Redes sociales' }]);
    });
    expect(endpointMock['getAdvertisingChoices']).toHaveBeenCalled();
  });

  it('should delegate getBaccalaureates to endpoint', () => {
    service.getBaccalaureates().subscribe(data => {
      expect(data).toEqual([{ id: 1, label: 'Científico' }]);
    });
    expect(endpointMock['getBaccalaureates']).toHaveBeenCalled();
  });

  it('should delegate getBaccalaureateYears to endpoint', () => {
    service.getBaccalaureateYears().subscribe(data => {
      expect(data).toEqual([{ id: 1, label: '2024', year: 2024 }]);
    });
    expect(endpointMock['getBaccalaureateYears']).toHaveBeenCalled();
  });

  it('should delegate getInstitutions to endpoint', () => {
    service.getInstitutions().subscribe(data => {
      expect(data).toEqual([{ id: 1, label: 'Liceo 1' }]);
    });
    expect(endpointMock['getInstitutions']).toHaveBeenCalled();
  });

  it('should delegate getUniversities to endpoint', () => {
    service.getUniversities().subscribe(data => {
      expect(data).toEqual([{ id: 1, label: 'ORT Uruguay' }]);
    });
    expect(endpointMock['getUniversities']).toHaveBeenCalled();
  });

  it('should delegate getScholarshipProducts to endpoint', () => {
    service.getScholarshipProducts().subscribe(data => {
      expect(data).toEqual([{ id: 1, label: 'Beca Excelencia' }]);
    });
    expect(endpointMock['getScholarshipProducts']).toHaveBeenCalled();
  });

  it('should delegate getScholarshipFunds to endpoint', () => {
    service.getScholarshipFunds().subscribe(data => {
      expect(data).toEqual([{ id: 1, label: 'Fondo ORT' }]);
    });
    expect(endpointMock['getScholarshipFunds']).toHaveBeenCalled();
  });

  it('should clear all caches', () => {
    service.getCountries().subscribe();
    service.clearCache();
    service.getCountries().subscribe();

    expect(endpointMock['getCountries']).toHaveBeenCalledTimes(2);
  });
});

