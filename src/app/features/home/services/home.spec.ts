import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';

import { HomeEndpoint } from '../endpoints/home.endpoint';
import { HomeService } from './home';

describe('HomeService', () => {
  let service: HomeService;
  let endpointMock: {
    getMisEnrollments: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    endpointMock = {
      getMisEnrollments: vi.fn().mockReturnValue(of([])),
    };

    TestBed.configureTestingModule({
      providers: [HomeService, { provide: HomeEndpoint, useValue: endpointMock }],
    });

    service = TestBed.inject(HomeService);
  });

  it('delegates getMisEnrollments to the endpoint', async () => {
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
    endpointMock.getMisEnrollments.mockReturnValue(of(inscripciones));

    await expect(firstValueFrom(service.getMisEnrollments())).resolves.toEqual(inscripciones);
    expect(endpointMock.getMisEnrollments).toHaveBeenCalled();
  });
});
