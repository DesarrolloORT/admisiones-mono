import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import { getPersonEnrollmentsEndpoint } from 'src/app/shared/api/generated/endpoints/person.endpoints';

import { HomeApi } from './home.api';

describe('HomeApi', () => {
  let endpoint: HomeApi;
  let api: { request: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    // Se stubea solo `request`: `list()` corre su implementacion real, asi que
    // este spec sigue cubriendo la normalizacion de la respuesta.
    api = Object.assign(Object.create(ApiHttpClient.prototype), {
      request: vi.fn(),
    }) as { request: ReturnType<typeof vi.fn> };

    TestBed.configureTestingModule({
      providers: [HomeApi, { provide: ApiHttpClient, useValue: api }],
    });

    endpoint = TestBed.inject(HomeApi);
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

    await expect(firstValueFrom(endpoint.getMyEnrollments())).resolves.toEqual([
      {
        productLevelId: 1,
        enrollmentId: 100,
        offeringIds: [300],
        productId: 10,
        admissionProcessId: 25,
        intakeId: 20,
        shiftId: 30,
        degreeProgramName: 'Analista Programador',
        intakeName: 'Marzo 2027',
        shiftName: 'Noche',
        status: 'Confirmada',
        paymentDueDate: null,
        seminars: [],
      },
    ]);
    expect(api.request.mock.calls[0][0]).toBe(getPersonEnrollmentsEndpoint);
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

    await expect(firstValueFrom(endpoint.getMyEnrollments())).resolves.toEqual([
      {
        enrollmentId: 200,
        offeringIds: [1, 2],
        productId: 15,
        admissionProcessId: 26,
        productLevelId: 3,
        intakeId: 21,
        shiftId: 31,
        degreeProgramName: 'Programa de Asesoramiento Financiero',
        intakeName: 'Abril 2027',
        shiftName: 'Tarde',
        status: 'Confirmada',
        paymentDueDate: null,
        seminars: [
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

    const result = await firstValueFrom(endpoint.getMyEnrollments());

    expect(result).toHaveLength(2);
    expect(result.every(enrollment => enrollment.seminars.length === 0)).toBe(true);
    expect(result.map(enrollment => enrollment.offeringIds)).toEqual([[300], [301]]);
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

    const result = await firstValueFrom(endpoint.getMyEnrollments());

    expect(result[0]?.offeringIds).toEqual([310]);
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

    const result = await firstValueFrom(endpoint.getMyEnrollments());

    expect(result[0]?.paymentDueDate).toBe('2026-07-18');
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

    const result = await firstValueFrom(endpoint.getMyEnrollments());

    expect(result).toEqual([]);
  });

  it('should treat a 404 response as no enrollments', async () => {
    api.request.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 404 })));

    await expect(firstValueFrom(endpoint.getMyEnrollments())).resolves.toEqual([]);
  });

  // ponytail: `GET /person/scholarships` fue removido del backend y `getMyScholarships` es un
  // stub que devuelve la lista vacia. Los tests del mapeo se borraron con el mapeo; vuelven
  // cuando vuelva el endpoint.
  it('should return no scholarships while the endpoint is gone', async () => {
    await expect(firstValueFrom(endpoint.getMyScholarships())).resolves.toEqual([]);
    expect(api.request).not.toHaveBeenCalled();
  });
});
