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

  it('should map careers from API data', () => {
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

    endpoint.getCareers(3).subscribe(result => {
      expect(result).toEqual([
        {
          idProducto: 20,
          idProceso: 10,
          idNivelProducto: 1,
          nombreProducto: 'Diseño',
          nombreNivelProducto: 'Carreras',
          nombreEscuela: 'Facultad de Diseño',
          tieneSeminario: false,
        },
        {
          idProducto: 30,
          idProceso: 11,
          idNivelProducto: 3,
          nombreProducto: 'Ciberseguridad',
          nombreNivelProducto: 'Actualización profesional',
          nombreEscuela: 'Escuela de Tecnología',
          tieneSeminario: true,
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
        educacion: {
          ubicacionesUltimoAnioSecundaria: [{ id: 1, label: 'Uruguay' }],
          estadosEducacionSuperiorPrevia: [{ id: 5, label: 'Sin estudios previos' }],
          universidades: [{ id: 11, label: 'ORT' }],
          nivelesFormacionTutores: [{ id: 6, label: 'Universitaria completa' }],
          aniosBachillerato: [
            {
              id: 11,
              label: '6º año',
              baccalaureates: [{ id: 12, label: 'Científico', orientation: 'Matemática' }],
            },
          ],
        },
        decisionAcademica: {
          apoyosDecision: [{ id: 2, label: 'Familia' }],
          aniosEducacionMediaSuperior: [{ id: 3, label: 'Salida laboral' }],
          nivelesDecision: [{ id: 7, label: 'Alto' }],
          motivosEleccionOrt: [{ id: 8, label: 'Reputación' }],
          universidades: [{ id: 10, label: 'Universidad de la República' }],
        },
        experienciaOrt: {
          valoraciones: [{ id: 4, label: 'Muy bueno' }],
          publicidadesOrt: [{ id: 9, label: 'Redes sociales' }],
        },
      });
    });
  });

  it('should map bancos from API data', () => {
    apiMock.request.mockReturnValue(
      of([{ id: 1, name: 'BROU', code: 11, sistarbancBankId: 'brou' }])
    );

    endpoint.getBancos().subscribe(result => {
      expect(result).toEqual([{ id: 1, label: 'BROU', code: 'brou' }]);
    });
  });

  it('should map instituciones from API data', () => {
    apiMock.request.mockReturnValue(of([{ id: 5, name: 'Liceo 1' }]));

    endpoint.getInstituciones(1, 10).subscribe(result => {
      expect(result).toEqual([{ id: 5, label: 'Liceo 1', codigoPais: 1, codigoEstado: 10 }]);
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

    endpoint.getTurnos(20, 10).subscribe(result => {
      expect(result).toEqual([
        {
          idOferta: 30,
          idTurno: 2,
          nombreTurno: 'Nocturno',
          horarioReferencia: '19:00 a 23:00',
          descripcionOferta: 'Seminario de marco legal',
          fechaReferencia: '19/05/2026',
        },
      ]);
    });
  });
});
