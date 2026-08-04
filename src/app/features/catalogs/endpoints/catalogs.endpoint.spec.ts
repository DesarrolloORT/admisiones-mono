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
          escuelas: [
            {
              nombreEscuela: 'Facultad de Diseño',
              productos: [{ idProducto: 20, idProceso: 10, nombreProducto: 'Diseño' }],
            },
          ],
        },
        {
          idNivelProducto: 3,
          nombreNivelProducto: 'Actualización profesional',
          escuelas: [
            {
              nombreEscuela: 'Escuela de Tecnología',
              seminarios: [
                {
                  tieneSeminario: true,
                  productos: [{ idProducto: 30, idProceso: 11, nombreProducto: 'Ciberseguridad' }],
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
      queryParams: { propuestaAcademica: 3 },
    });
  });

  it('should map initial survey catalogs from API data', () => {
    apiMock.request.mockReturnValue(
      of({
        educacion: {
          ubicacionesUltimoAnioSecundaria: [{ value: 1, label: 'Uruguay' }],
          estadosEducacionSuperiorPrevia: [{ value: 5, label: 'Sin estudios previos' }],
          universidades: [{ value: 11, label: 'ORT' }],
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
          valoraciones: [{ value: 4, label: 'Muy bueno' }],
          publicidadesOrt: [{ value: 9, label: 'Redes sociales' }],
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
          descripcionOferta: 'Seminario de marco legal',
          fechaReferencia: '19/05/2026',
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

  it('should delegate cache clearing', () => {
    endpoint.clearCache();

    expect(apiMock.clearCache).toHaveBeenCalled();
  });
});
