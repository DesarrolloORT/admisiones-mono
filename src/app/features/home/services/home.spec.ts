import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';

import { HomeEndpoint } from '../endpoints/home.endpoint';
import { HomeService } from './home';

describe('HomeService', () => {
  let service: HomeService;
  let endpointMock: {
    getMisInscripciones: ReturnType<typeof vi.fn>;
    getMisBecas: ReturnType<typeof vi.fn>;
    reactivarInscripcion: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      getMisInscripciones: vi.fn().mockReturnValue(of([])),
      getMisBecas: vi.fn().mockReturnValue(of([])),
      reactivarInscripcion: vi.fn().mockReturnValue(of(true)),
    };

    TestBed.configureTestingModule({
      providers: [HomeService, { provide: HomeEndpoint, useValue: endpointMock }],
    });

    service = TestBed.inject(HomeService);
  });

  it('delegates getMisInscripciones to the endpoint', async () => {
    const inscripciones = [
      {
        idInscripto: 100,
        idProducto: 1,
        idProceso: 4,
        idComienzo: 2,
        idTurno: 3,
        nombreProducto: 'Analista Programador',
        nombreComienzo: 'Marzo 2027',
        nombreTurno: 'Noche',
        estado: 'Confirmada',
      },
    ];
    endpointMock.getMisInscripciones.mockReturnValue(of(inscripciones));

    await expect(firstValueFrom(service.getMisInscripciones())).resolves.toEqual(inscripciones);
    expect(endpointMock.getMisInscripciones).toHaveBeenCalled();
  });

  it('delegates getMisBecas to the endpoint', async () => {
    const becas = [
      {
        id: 4,
        nombreBeca: 'Fondo de Excelencia Académica',
        nombreCarrera: 'Analista Programador',
        estado: 'En proceso',
        cierrePostulacion: '',
        fechaPrueba: '',
        resultadoPrueba: '',
        beneficio: '',
        fechaResultados: '',
      },
    ];
    endpointMock.getMisBecas.mockReturnValue(of(becas));

    await expect(firstValueFrom(service.getMisBecas())).resolves.toEqual(becas);
    expect(endpointMock.getMisBecas).toHaveBeenCalled();
  });

  it('delegates reactivarInscripcion to the endpoint', async () => {
    await expect(firstValueFrom(service.reactivarInscripcion(100))).resolves.toBe(true);

    expect(endpointMock.reactivarInscripcion).toHaveBeenCalledWith(100);
  });
});
