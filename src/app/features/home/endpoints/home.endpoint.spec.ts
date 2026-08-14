import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import { getPersonEnrollmentsEndpoint } from 'src/app/shared/api/generated/endpoints/person.endpoints';

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

  it('should map Persona/Enrollments into dashboard cards', async () => {
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

    await expect(firstValueFrom(endpoint.getMisEnrollments())).resolves.toEqual([
      {
        idNivelProducto: 1,
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

    await expect(firstValueFrom(endpoint.getMisEnrollments())).resolves.toEqual([
      {
        idInscripto: 200,
        idOfertas: [1, 2],
        idProducto: 15,
        idProceso: 26,
        idNivelProducto: 3,
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

    const result = await firstValueFrom(endpoint.getMisEnrollments());

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

    const result = await firstValueFrom(endpoint.getMisEnrollments());

    expect(result[0]?.idOfertas).toEqual([310]);
  });

  it('reads the payment deadline from the group', async () => {
    api.request.mockReturnValueOnce(
      of([
        {
          productId: 13,
          productLevelId: 1,
          admissionProcessId: 28,
          enrollmentStatus: 'Pago pendiente',
          paymentDueDate: '2026-07-18',
          enrollments: [{ enrollmentId: 103, offeringId: 303 }],
        },
      ])
    );

    const result = await firstValueFrom(endpoint.getMisEnrollments());

    expect(result[0]?.fechaVencimientoPago).toBe('2026-07-18');
  });

  it('should ignore groups without enrollments', async () => {
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

    const result = await firstValueFrom(endpoint.getMisEnrollments());

    expect(result).toEqual([]);
  });

  it('should treat a 404 response as no enrollments', async () => {
    api.request.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 404 })));

    await expect(firstValueFrom(endpoint.getMisEnrollments())).resolves.toEqual([]);
  });
});
