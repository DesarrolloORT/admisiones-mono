import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import { getScholarshipsEnrollmentsEndpoint } from 'src/app/shared/api/generated/endpoints/scholarships.endpoints';
import { vi } from 'vitest';

import { ScholarshipsApi } from './scholarships.api';

describe('ScholarshipsApi', () => {
  let api: ScholarshipsApi;
  let requestMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    requestMock = vi.fn().mockReturnValue(of(null));
    // Se stubea solo `request`: `list()` y `data()` corren su implementacion
    // real, asi que este spec sigue cubriendo la normalizacion de la respuesta.
    const apiMock = Object.assign(Object.create(ApiHttpClient.prototype), {
      request: requestMock,
    }) as ApiHttpClient;

    TestBed.configureTestingModule({
      providers: [ScholarshipsApi, { provide: ApiHttpClient, useValue: apiMock }],
    });

    api = TestBed.inject(ScholarshipsApi);
  });

  // ponytail: `GET /scholarships/available` fue removido del backend y el adapter es un stub.
  // Los tests del mapeo se fueron con el mapeo; vuelven cuando vuelva el endpoint.
  describe('getAvailableScholarships', () => {
    it('returns an empty catalogue while the endpoint is gone', async () => {
      await expect(firstValueFrom(api.getAvailableScholarships())).resolves.toEqual({
        requiresPriorEnrollment: true,
        scholarships: [],
      });
      expect(requestMock).not.toHaveBeenCalled();
    });
  });

  describe('getConfirmedEnrollments', () => {
    it('maps confirmed enrollments into feature types', async () => {
      requestMock.mockReturnValue(
        of([
          {
            enrollmentId: 1072704,
            enrollmentDate: '2027-02-04',
            enrollmentStatus: 'Confirmada',
            productId: 10,
            productFullName: 'Analista Programador',
            productLevelId: 1,
            admissionProcessId: 25,
            intakeId: 20,
            intakeName: 'Marzo 2027',
            intakeStartDate: '2027-03-01',
            shiftId: 30,
            shiftName: 'Noche',
            offeringId: 300,
            origin: 'Fresco',
          },
        ])
      );

      await expect(firstValueFrom(api.getConfirmedEnrollments())).resolves.toEqual([
        {
          enrollmentId: 1072704,
          enrollmentDate: '2027-02-04',
          status: 'Confirmada',
          productId: 10,
          degreeProgramName: 'Analista Programador',
          productLevelId: 1,
          admissionProcessId: 25,
          intakeId: 20,
          intakeName: 'Marzo 2027',
          intakeStartDate: '2027-03-01',
          shiftId: 30,
          shiftName: 'Noche',
          offeringId: 300,
          origin: 'Fresco',
        },
      ]);
      expect(requestMock.mock.calls[0][0]).toBe(getScholarshipsEnrollmentsEndpoint);
    });

    it('collapses nullable fields to safe defaults', async () => {
      requestMock.mockReturnValue(of([{}]));

      await expect(firstValueFrom(api.getConfirmedEnrollments())).resolves.toEqual([
        {
          enrollmentId: 0,
          enrollmentDate: null,
          status: '',
          productId: 0,
          degreeProgramName: '',
          productLevelId: null,
          admissionProcessId: 0,
          intakeId: 0,
          intakeName: '',
          intakeStartDate: null,
          shiftId: 0,
          shiftName: '',
          offeringId: 0,
          origin: '',
        },
      ]);
    });

    it('returns an empty list when there is no data', async () => {
      await expect(firstValueFrom(api.getConfirmedEnrollments())).resolves.toEqual([]);
    });
  });

  // ponytail: `POST /scholarships/applications` fue removido; el stub falla en vez de simular
  // un alta que nunca ocurrio.
  describe('createApplication', () => {
    it('fails while the endpoint is gone', async () => {
      await expect(
        firstValueFrom(api.createApplication({ enrollmentId: 1, testId: 2 }))
      ).rejects.toThrow('POST /scholarships/applications no está disponible.');
    });
  });

  it('propagates HTTP errors to the caller', async () => {
    requestMock.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));

    await expect(firstValueFrom(api.getConfirmedEnrollments())).rejects.toBeInstanceOf(
      HttpErrorResponse
    );
  });
});
