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

  it('should map initial survey catalogs from API data', () => {
    apiMock.request.mockReturnValue(
      of({
        aniosAprobadosEducacionSuperior: [{ value: 1, label: 'Un año' }],
        compartidoCon: [{ value: 2, label: 'Familia' }],
        decisionCarrera: [{ value: 3, label: 'Salida laboral' }],
        decisionUniversidad: [{ value: 4, label: 'Prestigio' }],
        estadoEducacionSuperior: [{ value: 5, label: 'Sin estudios previos' }],
        formacionTutores: [{ value: 6, label: 'Universitaria completa' }],
        nivelConocimiento: [{ value: 7, label: 'Alto' }],
        motivosEleccion: [{ idMotivo: 8, nombreMotivo: 'Reputación' }],
        publicidadesEleccion: [{ idPublicidad: 9, nombrePublicidad: 'Redes sociales' }],
        universidades: [{ codigoEmpresa: 10, nombre: 'Universidad de la República' }],
        aniosBachiller: [
          {
            idAnioBachiller: 11,
            nombreAnioBachiller: '6º año',
            bachilleratos: [
              { codigoTitulo: 12, nombre: 'Científico', orientacionTitulo: 'Matemática' },
            ],
          },
        ],
      })
    );

    endpoint.getInitialSurveyCatalogs().subscribe(result => {
      expect(result).toEqual({
        aniosAprobadosEducacionSuperior: [{ id: 1, label: 'Un año' }],
        compartidoCon: [{ id: 2, label: 'Familia' }],
        decisionCarrera: [{ id: 3, label: 'Salida laboral' }],
        decisionUniversidad: [{ id: 4, label: 'Prestigio' }],
        estadoEducacionSuperior: [{ id: 5, label: 'Sin estudios previos' }],
        formacionTutores: [{ id: 6, label: 'Universitaria completa' }],
        nivelConocimiento: [{ id: 7, label: 'Alto' }],
        motivosEleccion: [{ id: 8, label: 'Reputación' }],
        publicidadesEleccion: [{ id: 9, label: 'Redes sociales' }],
        universidades: [{ id: 10, label: 'Universidad de la República' }],
        aniosBachiller: [
          {
            id: 11,
            label: '6º año',
            baccalaureates: [{ id: 12, label: 'Científico', orientation: 'Matemática' }],
          },
        ],
      });
    });
  });

  it('should map bancos from API data', () => {
    apiMock.request.mockReturnValue(
      of({
        bancos: [{ idBanco: 1, nombreBanco: 'BROU', codigo: 'brou' }],
        totalCount: 1,
      })
    );

    endpoint.getBancos().subscribe(result => {
      expect(result).toEqual([{ id: 1, label: 'BROU', code: 'brou' }]);
    });
  });

  it('should map instituciones from API data', () => {
    apiMock.request.mockReturnValue(
      of([{ codigoEmpresa: 5, nombre: 'Liceo 1', codigoPais: 1, codigoEstado: 10 }])
    );

    endpoint.getInstituciones(1, 10).subscribe(result => {
      expect(result).toEqual([{ id: 5, label: 'Liceo 1', codigoPais: 1, codigoEstado: 10 }]);
    });

    expect(apiMock.request).toHaveBeenCalledWith(expect.anything(), {
      queryParams: { codigoPais: 1, codigoEstado: 10 },
    });
  });

  it('should map turnos from API data', () => {
    apiMock.request.mockReturnValue(
      of([
        {
          idOferta: 30,
          turno: {
            idTurno: 2,
            nombreTurno: 'Nocturno',
          },
          horarioReferencia: '19:00 a 23:00',
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
        },
      ]);
    });
  });

  it('should delegate cache clearing', () => {
    endpoint.clearCache();

    expect(apiMock.clearCache).toHaveBeenCalled();
  });
});
