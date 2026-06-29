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
          idNivelProducto: 1,
          nombreNivelProducto: 'Carreras',
          escuelas: [{ productos: [{ idProducto: 20, nombreProducto: 'Diseño' }] }],
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
        educacion: {
          estadosEducacionSuperiorPrevia: [{ value: 5, label: 'Sin estudios previos' }],
          nivelesFormacionTutores: [{ value: 6, label: 'Universitaria completa' }],
          aniosBachillerato: [
            {
              value: 11,
              label: '6º año',
              orientaciones: [{ value: 12, label: 'Científico', orientacion: 'Matemática' }],
            },
          ],
        },
        decisionAcademica: {
          apoyosDecision: [{ value: 2, label: 'Familia' }],
          aniosEducacionMediaSuperior: [{ value: 3, label: 'Salida laboral' }],
          nivelesDecision: [{ value: 7, label: 'Alto' }],
          motivosEleccionOrt: [{ value: 8, label: 'Reputación' }],
          universidades: [{ value: 10, label: 'Universidad de la República' }],
        },
        experienciaOrt: {
          publicidadesOrt: [{ value: 9, label: 'Redes sociales' }],
        },
      })
    );

    endpoint.getInitialSurveyCatalogs().subscribe(result => {
      expect(result).toEqual({
        aniosAprobadosEducacionSuperior: [],
        compartidoCon: [{ id: 2, label: 'Familia' }],
        decisionCarrera: [{ id: 3, label: 'Salida laboral' }],
        decisionUniversidad: [{ id: 3, label: 'Salida laboral' }],
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
      of([{ idBanco: 1, nombreBanco: 'BROU', codigoBanco: 11, idBancoSistarbanc: 'brou' }])
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
