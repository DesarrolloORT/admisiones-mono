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
    getCareers: ReturnType<typeof vi.fn>;
    getComienzos: ReturnType<typeof vi.fn>;
    getInitialSurveyCatalogs: ReturnType<typeof vi.fn>;
    getBancos: ReturnType<typeof vi.fn>;
    getInstituciones: ReturnType<typeof vi.fn>;
    getTurnos: ReturnType<typeof vi.fn>;
    clearCache: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      getCountries: vi.fn(),
      getCountryLocations: vi.fn(),
      getCareers: vi.fn(),
      getComienzos: vi.fn(),
      getInitialSurveyCatalogs: vi.fn(),
      getBancos: vi.fn(),
      getInstituciones: vi.fn(),
      getTurnos: vi.fn(),
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

  it('should delegate getCareers to the endpoint', () => {
    const result = [
      {
        idProducto: 20,
        idNivelProducto: 1,
        nombreProducto: 'Diseño',
        nombreNivelProducto: 'Carreras',
      },
    ];
    endpointMock.getCareers.mockReturnValue(of(result));

    service.getCareers().subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getCareers).toHaveBeenCalledOnce();
  });

  it('should delegate getComienzos to the endpoint', () => {
    const result = [{ idProceso: 10, nombreProceso: 'Marzo 2026' }];
    endpointMock.getComienzos.mockReturnValue(of(result));

    service.getComienzos(20).subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getComienzos).toHaveBeenCalledWith(20);
  });

  it('should delegate getInitialSurveyCatalogs to the endpoint', () => {
    const result = {
      educacion: {
        ubicacionesUltimoAnioSecundaria: [],
        aniosBachillerato: [],
        estadosEducacionSuperiorPrevia: [],
        universidades: [],
        nivelesFormacionTutores: [],
      },
      decisionAcademica: {
        aniosEducacionMediaSuperior: [],
        apoyosDecision: [],
        nivelesDecision: [],
        universidades: [],
        motivosEleccionOrt: [],
      },
      experienciaOrt: { valoraciones: [], publicidadesOrt: [] },
      situacionLaboral: { tiposJornada: [] },
    };
    endpointMock.getInitialSurveyCatalogs.mockReturnValue(of(result));

    service.getInitialSurveyCatalogs().subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getInitialSurveyCatalogs).toHaveBeenCalledOnce();
  });

  it('should delegate getBancos to the endpoint', () => {
    const result = [{ id: 1, label: 'BROU', code: 'brou' }];
    endpointMock.getBancos.mockReturnValue(of(result));

    service.getBancos().subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getBancos).toHaveBeenCalledOnce();
  });

  it('should delegate getInstituciones to the endpoint', () => {
    const result = [{ id: 5, label: 'Liceo 1', codigoPais: 1, codigoEstado: 10 }];
    endpointMock.getInstituciones.mockReturnValue(of(result));

    service.getInstituciones(1, 10).subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getInstituciones).toHaveBeenCalledWith(1, 10);
  });

  it('should delegate getTurnos to the endpoint', () => {
    const result = [
      {
        idOferta: 30,
        idTurno: 2,
        nombreTurno: 'Nocturno',
        horarioReferencia: '19:00',
      },
    ];
    endpointMock.getTurnos.mockReturnValue(of(result));

    service.getTurnos(20, 10).subscribe(data => {
      expect(data).toEqual(result);
    });

    expect(endpointMock.getTurnos).toHaveBeenCalledWith(20, 10);
  });

  it('should delegate clearCache to the endpoint', () => {
    service.clearCache();

    expect(endpointMock.clearCache).toHaveBeenCalledOnce();
  });
});
