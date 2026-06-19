import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getPersonaBecasEndpoint,
  getPersonaInscripcionesEndpoint,
} from 'src/app/shared/api/generated/endpoints/persona.endpoints';

import { HomeEndpoint } from './home.endpoint';

describe('HomeEndpoint', () => {
  let endpoint: HomeEndpoint;
  let api: { request: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = { request: vi.fn() };

    TestBed.configureTestingModule({
      providers: [HomeEndpoint, { provide: ApiHttpClient, useValue: api }],
    });

    endpoint = TestBed.inject(HomeEndpoint);
  });

  it('should map Persona/Inscripciones into dashboard cards', async () => {
    api.request.mockReturnValueOnce(
      of([
        {
          idProducto: 10,
          idComienzo: 20,
          idTurno: 30,
          nombreExtensoProducto: 'Analista Programador',
          nombreComienzo: 'Marzo 2027',
          nombreTurno: 'Noche',
          estadoInscripcion: 'Confirmada',
        },
      ])
    );

    await expect(firstValueFrom(endpoint.getMisInscripciones())).resolves.toEqual([
      {
        idProducto: 10,
        idComienzo: 20,
        idTurno: 30,
        nombreProducto: 'Analista Programador',
        nombreComienzo: 'Marzo 2027',
        nombreTurno: 'Noche',
        estado: 'Confirmada',
      },
    ]);
    expect(api.request).toHaveBeenCalledWith(getPersonaInscripcionesEndpoint);
  });

  it('should map Persona/Becas into dashboard cards', async () => {
    api.request.mockReturnValueOnce(
      of([
        {
          idBeca: 40,
          idPostulacion: 50,
          nombre: 'Fondo de Excelencia Académica',
          carrera: 'Licenciatura en Diseño Gráfico',
          estado: 'En proceso',
          fechaCierrePostulacion: '2026-07-15T00:00:00Z',
          fechaPrueba: '2026-07-22T00:00:00Z',
          fechaResultados: null,
        },
      ])
    );

    await expect(firstValueFrom(endpoint.getMisBecas())).resolves.toEqual([
      {
        id: 50,
        nombreBeca: 'Fondo de Excelencia Académica',
        nombreCarrera: 'Licenciatura en Diseño Gráfico',
        estado: 'En proceso',
        cierrePostulacion: 'Miércoles 15/07/2026',
        fechaPrueba: 'Miércoles 22/07/2026',
        resultadoPrueba: '',
        beneficio: '',
        fechaResultados: '',
      },
    ]);
    expect(api.request).toHaveBeenCalledWith(getPersonaBecasEndpoint);
  });
});
