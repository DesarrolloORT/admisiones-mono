import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';

import { HomeEndpoint } from '../endpoints/home.endpoint';
import { HomeService } from './home';

describe('HomeService', () => {
  let service: HomeService;
  let endpointMock: {
    getMisInscripciones: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      getMisInscripciones: vi.fn().mockReturnValue(of([])),
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
        idOfertas: [300],
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
});
