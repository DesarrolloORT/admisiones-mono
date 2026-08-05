import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import { postEnrollmentsReactivateEndpoint } from 'src/app/shared/api/generated/endpoints/enrollments.endpoints';
import {
  getPersonEnrollmentsEndpoint,
  getPersonScholarshipsEndpoint,
} from 'src/app/shared/api/generated/endpoints/person.endpoints';

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
          productId: 10,
          productLevelId: 1,
          admissionProcessId: 25,
          productFullName: 'Analista Programador',
          enrollmentStatus: 'Confirmada',
          enrollments: [
            {
              enrollmentId: 100,
              offeringId: 300,
              intakeId: 20,
              shiftId: 30,
              intakeName: 'Marzo 2027',
              shiftName: 'Noche',
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
        seminarios: [],
      },
    ]);
    expect(api.request).toHaveBeenCalledWith(getPersonEnrollmentsEndpoint);
  });

  it('should group levels 3 and 4 into a single card with seminarios', async () => {
    api.request.mockReturnValueOnce(
      of([
        {
          productId: 15,
          productLevelId: 3,
          admissionProcessId: 26,
          productFullName: 'Programa de Asesoramiento Financiero',
          enrollmentStatus: 'Confirmada',
          enrollments: [
            {
              enrollmentId: 200,
              offeringId: 1,
              offeringDescription: 'Marco legal y tributario',
              intakeId: 21,
              shiftId: 31,
              intakeName: 'Abril 2027',
              shiftName: 'Tarde',
            },
            {
              enrollmentId: 201,
              offeringId: 2,
              offeringDescription: 'Renta fija y renta variable',
              intakeId: 22,
              shiftId: 32,
              intakeName: 'Mayo 2027',
              shiftName: 'Noche',
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
          productId: 10,
          productLevelId: 2,
          admissionProcessId: 25,
          productFullName: 'Analista Programador',
          enrollmentStatus: 'Confirmada',
          enrollments: [
            {
              enrollmentId: 100,
              offeringId: 300,
              intakeId: 20,
              shiftId: 30,
              intakeName: 'Marzo 2027',
            },
            {
              enrollmentId: 101,
              offeringId: 301,
              intakeId: 21,
              shiftId: 31,
              intakeName: 'Abril 2027',
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
          productId: 15,
          productLevelId: 4,
          admissionProcessId: 26,
          enrollments: [
            { offeringId: 310 },
            { offeringId: 310 },
            { offeringId: 0 },
            { offeringId: null },
          ],
        },
      ])
    );

    const result = await firstValueFrom(endpoint.getMisInscripciones());

    expect(result[0]?.idOfertas).toEqual([310]);
  });

  it('should not explode when inscripciones is null or empty', async () => {
    api.request.mockReturnValueOnce(
      of([
        {
          productId: 10,
          productLevelId: 1,
          admissionProcessId: 25,
          productFullName: 'Analista Programador',
          enrollmentStatus: 'Confirmada',
          enrollments: null,
        },
        {
          productId: 11,
          productLevelId: 3,
          admissionProcessId: 27,
          productFullName: 'Programa vacío',
          enrollmentStatus: 'Pendiente',
          enrollments: [],
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
          scholarshipId: 40,
          applicationId: 50,
          name: 'Fondo de Excelencia Académica',
          degreeProgram: 'Licenciatura en Diseño Gráfico',
          status: 'En proceso',
          applicationCloseDate: '2026-07-15T00:00:00Z',
          testDate: '2026-07-22T00:00:00Z',
          resultsDate: null,
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
    expect(api.request).toHaveBeenCalledWith(getPersonScholarshipsEndpoint);
  });

  it('should reactivate an inscripcion and clear the cache', async () => {
    api.request.mockReturnValueOnce(of({}));

    await expect(firstValueFrom(endpoint.reactivarInscripcion(100))).resolves.toBe(true);

    expect(api.request).toHaveBeenCalledWith(postEnrollmentsReactivateEndpoint, {
      body: { enrollmentId: 100 },
      showLoader: true,
    });
    expect(api.clearCache).toHaveBeenCalled();
  });
});
