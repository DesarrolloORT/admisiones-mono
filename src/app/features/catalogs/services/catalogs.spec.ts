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
    getDegreePrograms: ReturnType<typeof vi.fn>;
    getIntakes: ReturnType<typeof vi.fn>;
    getInitialSurveyCatalogs: ReturnType<typeof vi.fn>;
    getBanks: ReturnType<typeof vi.fn>;
    getInstitutions: ReturnType<typeof vi.fn>;
    getShifts: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      getCountries: vi.fn(),
      getCountryLocations: vi.fn(),
      getDegreePrograms: vi.fn(),
      getIntakes: vi.fn(),
      getInitialSurveyCatalogs: vi.fn(),
      getBanks: vi.fn(),
      getInstitutions: vi.fn(),
      getShifts: vi.fn(),
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

  it('should delegate getDegreePrograms to the endpoint', () => {
    const result = [
      {
        productId: 20,
        productLevelId: 1,
        productName: 'Diseño',
        productLevelName: 'Carreras',
      },
    ];
    endpointMock.getDegreePrograms.mockReturnValue(of(result));

    service.getDegreePrograms(2).subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getDegreePrograms).toHaveBeenCalledWith(2);
  });

  it('should delegate getIntakes to the endpoint', () => {
    const result = [{ admissionProcessId: 10, admissionProcessName: 'Marzo 2026' }];
    endpointMock.getIntakes.mockReturnValue(of(result));

    service.getIntakes(20).subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getIntakes).toHaveBeenCalledWith(20);
  });

  it('should delegate getInitialSurveyCatalogs to the endpoint', () => {
    const result = {
      education: {
        lastSecondaryYearLocations: [],
        highSchoolYears: [],
        previousHigherEducationOptions: [],
        universities: [],
        guardianEducationLevels: [],
      },
      academicDecision: {
        upperSecondaryYears: [],
        decisionSupports: [],
        decisionLevels: [],
        universities: [],
        ortChoiceReasons: [],
      },
      ortExperience: { ratings: [], ortAdvertisements: [] },
    };
    endpointMock.getInitialSurveyCatalogs.mockReturnValue(of(result));

    service.getInitialSurveyCatalogs().subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getInitialSurveyCatalogs).toHaveBeenCalledOnce();
  });

  it('should delegate getBanks to the endpoint', () => {
    const result = [{ id: 1, label: 'BROU', code: 'brou' }];
    endpointMock.getBanks.mockReturnValue(of(result));

    service.getBanks().subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getBanks).toHaveBeenCalledOnce();
  });

  it('should delegate getInstitutions to the endpoint', () => {
    const result = [{ id: 5, label: 'Liceo 1', countryCode: 1, stateCode: 10 }];
    endpointMock.getInstitutions.mockReturnValue(of(result));

    service.getInstitutions(1, 10).subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getInstitutions).toHaveBeenCalledWith(1, 10);
  });

  it('should delegate getShifts to the endpoint', () => {
    const result = [
      {
        offeringId: 30,
        shiftId: 2,
        shiftName: 'Nocturno',
        referenceSchedule: '19:00',
      },
    ];
    endpointMock.getShifts.mockReturnValue(of(result));

    service.getShifts(20, 10).subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getShifts).toHaveBeenCalledWith(20, 10);
  });

  it('should map seminars using the offer description and reference date', () => {
    endpointMock.getShifts.mockReturnValue(
      of([
        {
          offeringId: 30,
          shiftId: 2,
          shiftName: 'Nocturno',
          referenceSchedule: '19:00',
          offeringDescription: 'Seminario de marco legal',
          referenceDate: '19/05/2026',
        },
      ])
    );

    service.getSeminars(20, 10).subscribe(data => {
      expect(data).toEqual([
        {
          offeringId: 30,
          admissionProcessId: 10,
          name: 'Seminario de marco legal',
          startDate: '19/05/2026',
        },
      ]);
    });
  });
});
