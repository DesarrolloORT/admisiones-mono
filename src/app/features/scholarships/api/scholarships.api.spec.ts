import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getScholarshipsAvailableEndpoint,
  getScholarshipsEnrollmentsEndpoint,
  postScholarshipsApplicationsEndpoint,
} from 'src/app/shared/api/generated/endpoints/scholarships.endpoints';
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

  describe('getAvailableScholarships', () => {
    it('maps the response into feature types', async () => {
      requestMock.mockReturnValue(
        of({
          requiresPriorEnrollment: false,
          scholarships: [
            {
              scholarshipTypeIds: [33, 57],
              name: 'Fondo de Excelencia Académica',
              description: 'Dirigida a estudiantes con destacado desempeño en secundaria.',
              requiresTest: true,
            },
          ],
        })
      );

      await expect(firstValueFrom(api.getAvailableScholarships())).resolves.toEqual({
        requiresPriorEnrollment: false,
        scholarships: [
          {
            scholarshipTypeIds: [33, 57],
            name: 'Fondo de Excelencia Académica',
            description: 'Dirigida a estudiantes con destacado desempeño en secundaria.',
            requiresTest: true,
          },
        ],
      });
      expect(requestMock.mock.calls[0][0]).toBe(getScholarshipsAvailableEndpoint);
    });

    it('collapses nullable fields to safe defaults', async () => {
      requestMock.mockReturnValue(of({ scholarships: [{}] }));

      await expect(firstValueFrom(api.getAvailableScholarships())).resolves.toEqual({
        // Sin el flag se asume el caso restrictivo: no habilitar la postulación.
        requiresPriorEnrollment: true,
        scholarships: [{ scholarshipTypeIds: [], name: '', description: '', requiresTest: false }],
      });
    });

    it('returns an empty catalogue when there is no data', async () => {
      await expect(firstValueFrom(api.getAvailableScholarships())).resolves.toEqual({
        requiresPriorEnrollment: true,
        scholarships: [],
      });
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

  describe('createApplication', () => {
    it('sends the payload as the request body and maps the created application', async () => {
      requestMock.mockReturnValue(
        of({ applicationId: 9001, affidavitId: 55, affidavitStatus: 'PENDIENTE' })
      );

      await expect(
        firstValueFrom(api.createApplication({ enrollmentId: 1072704, testId: 42 }))
      ).resolves.toEqual({
        applicationId: 9001,
        affidavitId: 55,
        affidavitStatus: 'PENDIENTE',
      });
      expect(requestMock).toHaveBeenCalledWith(postScholarshipsApplicationsEndpoint, {
        body: { enrollmentId: 1072704, testId: 42 },
        showLoader: true,
      });
    });

    it('collapses nullable fields to safe defaults', async () => {
      requestMock.mockReturnValue(of({}));

      await expect(
        firstValueFrom(api.createApplication({ enrollmentId: 1, testId: 2 }))
      ).resolves.toEqual({ applicationId: 0, affidavitId: null, affidavitStatus: '' });
    });
  });

  it('propagates HTTP errors to the caller', async () => {
    requestMock.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));

    await expect(firstValueFrom(api.getAvailableScholarships())).rejects.toBeInstanceOf(
      HttpErrorResponse
    );
    await expect(
      firstValueFrom(api.createApplication({ enrollmentId: 1, testId: 2 }))
    ).rejects.toBeInstanceOf(HttpErrorResponse);
  });
});
