import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ApiHttpClient } from '../../../shared/api/core/api-http-client';
import { CatalogsEndpoint } from './catalogs.endpoint';

describe('CatalogsEndpoint', () => {
  let endpoint: CatalogsEndpoint;
  let apiMock: {
    request: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    apiMock = {
      request: vi.fn().mockReturnValue(of([])),
    };

    TestBed.configureTestingModule({
      providers: [CatalogsEndpoint, { provide: ApiHttpClient, useValue: apiMock }],
    });

    endpoint = TestBed.inject(CatalogsEndpoint);
  });

  it('should map degreePrograms from API data', () => {
    apiMock.request.mockReturnValue(
      of([
        {
          productLevelId: 1,
          productLevelName: 'Carreras',
          schools: [
            {
              schoolName: 'Facultad de Diseño',
              products: [{ productId: 20, admissionProcessId: 10, productName: 'Diseño' }],
            },
          ],
        },
        {
          productLevelId: 3,
          productLevelName: 'Actualización profesional',
          schools: [
            {
              schoolName: 'Escuela de Tecnología',
              seminars: [
                {
                  hasSeminar: true,
                  products: [
                    { productId: 30, admissionProcessId: 11, productName: 'Ciberseguridad' },
                  ],
                },
              ],
            },
          ],
        },
      ])
    );

    endpoint.getDegreePrograms(3).subscribe(result => {
      expect(result).toEqual([
        {
          productId: 20,
          admissionProcessId: 10,
          productLevelId: 1,
          productName: 'Diseño',
          productLevelName: 'Carreras',
          schoolName: 'Facultad de Diseño',
          hasSeminar: false,
        },
        {
          productId: 30,
          admissionProcessId: 11,
          productLevelId: 3,
          productName: 'Ciberseguridad',
          productLevelName: 'Actualización profesional',
          schoolName: 'Escuela de Tecnología',
          hasSeminar: true,
        },
      ]);
    });

    expect(apiMock.request).toHaveBeenCalledWith(expect.anything(), {
      queryParams: { academicOffer: 3 },
    });
  });

  it('should map initial survey catalogs from API data', () => {
    apiMock.request.mockReturnValue(
      of({
        education: {
          lastSecondaryYearLocations: [{ value: 1, label: 'Uruguay' }],
          previousHigherEducationOptions: [{ value: 5, label: 'Sin estudios previos' }],
          universities: [{ value: 11, label: 'ORT' }],
          educationLevels: [{ value: 6, label: 'Universitaria completa' }],
          highSchoolYears: [
            {
              value: 11,
              label: '6º año',
              tracks: [{ value: 12, label: 'Científico', track: 'Matemática' }],
            },
          ],
        },
        academicDecision: {
          decisionSupports: [{ value: 2, label: 'Familia' }],
          upperSecondaryYears: [{ value: 3, label: 'Salida laboral' }],
          decisionLevels: [{ value: 7, label: 'Alto' }],
          ortChoiceReasons: [{ value: 8, label: 'Reputación' }],
          universities: [{ value: 10, label: 'Universidad de la República' }],
        },
        ortExperience: {
          ratings: [{ value: 4, label: 'Muy bueno' }],
          ortAdvertisements: [{ value: 9, label: 'Redes sociales' }],
        },
      })
    );

    endpoint.getInitialSurveyCatalogs().subscribe(result => {
      expect(result).toEqual({
        education: {
          lastSecondaryYearLocations: [{ id: 1, label: 'Uruguay' }],
          previousHigherEducationOptions: [{ id: 5, label: 'Sin estudios previos' }],
          universities: [{ id: 11, label: 'ORT' }],
          guardianEducationLevels: [{ id: 6, label: 'Universitaria completa' }],
          highSchoolYears: [
            {
              id: 11,
              label: '6º año',
              baccalaureates: [{ id: 12, label: 'Científico', orientation: 'Matemática' }],
            },
          ],
        },
        academicDecision: {
          decisionSupports: [{ id: 2, label: 'Familia' }],
          upperSecondaryYears: [{ id: 3, label: 'Salida laboral' }],
          decisionLevels: [{ id: 7, label: 'Alto' }],
          ortChoiceReasons: [{ id: 8, label: 'Reputación' }],
          universities: [{ id: 10, label: 'Universidad de la República' }],
        },
        ortExperience: {
          ratings: [{ id: 4, label: 'Muy bueno' }],
          ortAdvertisements: [{ id: 9, label: 'Redes sociales' }],
        },
      });
    });
  });

  it('should map bancos from API data', () => {
    apiMock.request.mockReturnValue(
      of([{ id: 1, name: 'BROU', code: 11, sistarbancBankId: 'brou' }])
    );

    endpoint.getBanks().subscribe(result => {
      expect(result).toEqual([{ id: 1, label: 'BROU', code: 'brou' }]);
    });
  });

  it('should map instituciones from API data', () => {
    apiMock.request.mockReturnValue(of([{ id: 5, name: 'Liceo 1' }]));

    endpoint.getInstitutions(1, 10).subscribe(result => {
      expect(result).toEqual([{ id: 5, label: 'Liceo 1', countryCode: 1, stateCode: 10 }]);
    });

    expect(apiMock.request).toHaveBeenCalledWith(expect.anything(), {
      queryParams: { countryId: 1, stateId: 10 },
    });
  });

  it('should map turnos from API data', () => {
    apiMock.request.mockReturnValue(
      of([
        {
          offeringId: 30,
          shift: {
            shiftId: 2,
            shiftName: 'Nocturno',
          },
          referenceSchedule: '19:00 a 23:00',
          offeringDescription: 'Seminario de marco legal',
          referenceDate: '19/05/2026',
        },
      ])
    );

    endpoint.getShifts(20, 10).subscribe(result => {
      expect(result).toEqual([
        {
          offeringId: 30,
          shiftId: 2,
          shiftName: 'Nocturno',
          referenceSchedule: '19:00 a 23:00',
          offeringDescription: 'Seminario de marco legal',
          referenceDate: '19/05/2026',
        },
      ]);
    });
  });
});
