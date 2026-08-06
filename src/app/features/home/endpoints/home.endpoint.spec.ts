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
  let api: { request: ReturnType<typeof vi.fn>; clearCache: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = { request: vi.fn(), clearCache: vi.fn() };

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
          idNivelProducto: 1,
          idProceso: 25,
          nombreExtensoProducto: 'Analista Programador',
          estadoInscripcion: 'Confirmada',
          inscripciones: [
            {
              idInscripto: 100,
              idOferta: 300,
              idComienzo: 20,
              idTurno: 30,
              nombreComienzo: 'Marzo 2027',
              nombreTurno: 'Noche',
            },
          ],
        },
      ])
    );

    await expect(firstValueFrom(endpoint.getMisInscripciones())).resolves.toEqual([
      {
        idInscripto: 100,
        idOfertas: [300],
        idProducto: 10,
        idProceso: 25,
        idComienzo: 20,
        idTurno: 30,
        nombreProducto: 'Analista Programador',
        nombreComienzo: 'Marzo 2027',
        nombreTurno: 'Noche',
        estado: 'Confirmada',
        fechaVencimientoPago: null,
        seminarios: [],
      },
    ]);
    expect(api.request).toHaveBeenCalledWith(getPersonaInscripcionesEndpoint);
  });

  it('should group levels 3 and 4 into a single card with seminarios', async () => {
    api.request.mockReturnValueOnce(
      of([
        {
          idProducto: 15,
          idNivelProducto: 3,
          idProceso: 26,
          nombreExtensoProducto: 'Programa de Asesoramiento Financiero',
          estadoInscripcion: 'Confirmada',
          inscripciones: [
            {
              idInscripto: 200,
              idOferta: 1,
              descripcionOferta: 'Marco legal y tributario',
              idComienzo: 21,
              idTurno: 31,
              nombreComienzo: 'Abril 2027',
              nombreTurno: 'Tarde',
            },
            {
              idInscripto: 201,
              idOferta: 2,
              descripcionOferta: 'Renta fija y renta variable',
              idComienzo: 22,
              idTurno: 32,
              nombreComienzo: 'Mayo 2027',
              nombreTurno: 'Noche',
            },
          ],
        },
      ])
    );

    await expect(firstValueFrom(endpoint.getMisInscripciones())).resolves.toEqual([
      {
        idInscripto: 200,
        idOfertas: [1, 2],
        idProducto: 15,
        idProceso: 26,
        idComienzo: 21,
        idTurno: 31,
        nombreProducto: 'Programa de Asesoramiento Financiero',
        nombreComienzo: 'Abril 2027',
        nombreTurno: 'Tarde',
        estado: 'Confirmada',
        fechaVencimientoPago: null,
        seminarios: [
          {
            idInscripto: 200,
            idOferta: 1,
            descripcionOferta: 'Marco legal y tributario',
            idComienzo: 21,
            idTurno: 31,
            nombreComienzo: 'Abril 2027',
            nombreTurno: 'Tarde',
          },
          {
            idInscripto: 201,
            idOferta: 2,
            descripcionOferta: 'Renta fija y renta variable',
            idComienzo: 22,
            idTurno: 32,
            nombreComienzo: 'Mayo 2027',
            nombreTurno: 'Noche',
          },
        ],
      },
    ]);
  });

  it('should keep one card per item for levels 1 and 2 with multiple inscripciones', async () => {
    api.request.mockReturnValueOnce(
      of([
        {
          idProducto: 10,
          idNivelProducto: 2,
          idProceso: 25,
          nombreExtensoProducto: 'Analista Programador',
          estadoInscripcion: 'Confirmada',
          inscripciones: [
            {
              idInscripto: 100,
              idOferta: 300,
              idComienzo: 20,
              idTurno: 30,
              nombreComienzo: 'Marzo 2027',
            },
            {
              idInscripto: 101,
              idOferta: 301,
              idComienzo: 21,
              idTurno: 31,
              nombreComienzo: 'Abril 2027',
            },
          ],
        },
      ])
    );

    const result = await firstValueFrom(endpoint.getMisInscripciones());

    expect(result).toHaveLength(2);
    expect(result.every(inscripcion => inscripcion.seminarios.length === 0)).toBe(true);
    expect(result.map(inscripcion => inscripcion.idOfertas)).toEqual([[300], [301]]);
  });

  it('keeps only unique positive offer ids in an AP package', async () => {
    api.request.mockReturnValueOnce(
      of([
        {
          idProducto: 15,
          idNivelProducto: 4,
          idProceso: 26,
          inscripciones: [
            { idOferta: 310 },
            { idOferta: 310 },
            { idOferta: 0 },
            { idOferta: null },
          ],
        },
      ])
    );

    const result = await firstValueFrom(endpoint.getMisInscripciones());

    expect(result[0]?.idOfertas).toEqual([310]);
  });

  it('reads the payment deadline from the group or, failing that, from the item', async () => {
    api.request.mockReturnValueOnce(
      of([
        {
          idProducto: 10,
          idNivelProducto: 1,
          idProceso: 25,
          estadoInscripcion: 'Pago pendiente',
          fechaVencimientoPago: '2026-07-15',
          inscripciones: [{ idInscripto: 100, idOferta: 300 }],
        },
        {
          idProducto: 11,
          idNivelProducto: 1,
          idProceso: 26,
          estadoInscripcion: 'Pago pendiente',
          inscripciones: [{ idInscripto: 101, idOferta: 301, fechaVencimientoPago: '2026-07-20' }],
        },
        {
          idProducto: 12,
          idNivelProducto: 1,
          idProceso: 27,
          estadoInscripcion: 'Pago pendiente',
          inscripciones: [{ idInscripto: 102, idOferta: 302, fechaVencimientoPago: '   ' }],
        },
      ])
    );

    const result = await firstValueFrom(endpoint.getMisInscripciones());

    expect(result.map(inscripcion => inscripcion.fechaVencimientoPago)).toEqual([
      '2026-07-15',
      '2026-07-20',
      null,
    ]);
  });

  it('should not explode when inscripciones is null or empty', async () => {
    api.request.mockReturnValueOnce(
      of([
        {
          idProducto: 10,
          idNivelProducto: 1,
          idProceso: 25,
          nombreExtensoProducto: 'Analista Programador',
          estadoInscripcion: 'Confirmada',
          inscripciones: null,
        },
        {
          idProducto: 11,
          idNivelProducto: 3,
          idProceso: 27,
          nombreExtensoProducto: 'Programa vacío',
          estadoInscripcion: 'Pendiente',
          inscripciones: [],
        },
      ])
    );

    const result = await firstValueFrom(endpoint.getMisInscripciones());

    expect(result.every(inscripcion => inscripcion.seminarios.length === 0)).toBe(true);
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
