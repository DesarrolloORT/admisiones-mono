import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  convertToParamMap,
  provideRouter,
  RouterStateSnapshot,
} from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { CatalogsApi } from '../../catalogs/api/catalogs.api';
import { EnrollmentsApi } from '../api/enrollments.api';
import type { EnrollmentDetail } from '../models/enrollment-detail';
import type { EnrollmentEntryResolved } from '../models/enrollment-entry';
import { EnrollmentResumeContextStore } from '../services/enrollment-resume-context';
import { enrollmentDetailResolver, resolveEntryIntent } from './enrollment-detail.resolver';

const REACTIVATION_RESPONSE = {
  confirmed: false,
  isWaiting: false,
  enrollmentId: 7010,
  paymentDueDate: '2027-03-04',
  enrollmentDeposit: 15500,
  accountBalance: 1200,
  summary: { degreeProgram: 'Sistemas', intake: 'Marzo 2027', shift: 'Noche' },
  seminars: [
    {
      enrollmentId: 7010,
      offeringId: 310,
      name: 'Seminario',
      intake: 'Marzo 2027',
      shift: 'Noche',
    },
  ],
};

describe('enrollmentDetailResolver', () => {
  let getDetail: ReturnType<typeof vi.fn>;
  let getDegreePrograms: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    sessionStorage.clear();
    getDetail = vi.fn();
    getDegreePrograms = vi.fn().mockReturnValue(of([]));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: EnrollmentsApi, useValue: { getDetail } },
        { provide: CatalogsApi, useValue: { getDegreePrograms } },
      ],
    });
  });

  it('resolves the "retomar" intent with the loaded detail', async () => {
    const detail = createDetail('Pago pendiente');
    getDetail.mockReturnValue(of(detail));

    await expect(resolve({ idProducto: '20', idProceso: '200' })).resolves.toEqual({
      intent: 'resume',
      detail,
      productId: 20,
      admissionProcessId: 200,
      offeringIds: [],
      productLevelId: null,
    });
    expect(getDetail).toHaveBeenCalledWith(20, 200, null);
  });

  it('propagates the estado query param to the detail request', async () => {
    const detail = createDetail('Pago pendiente');
    getDetail.mockReturnValue(of(detail));

    await resolve({ idProducto: '20', idProceso: '200', estado: 'Pago pendiente' });

    expect(getDetail).toHaveBeenCalledWith(20, 200, 'Pago pendiente');
  });

  it('resolves the product level from the degreePrograms catalog', async () => {
    const detail = createDetail('En proceso', 21);
    getDetail.mockReturnValue(of(detail));
    getDegreePrograms.mockReturnValue(
      of([
        {
          productId: 21,
          productLevelId: 3,
          productName: 'Programa de Asesoramiento Financiero',
          productLevelName: 'Actualización profesional',
        },
      ])
    );

    await expect(resolve({ idProducto: '21', idProceso: '200' })).resolves.toEqual({
      intent: 'resume',
      detail,
      productId: 21,
      admissionProcessId: 200,
      offeringIds: [],
      productLevelId: 3,
    });
  });

  it('takes the product level from the nivel param without querying the catalog', async () => {
    const detail = createDetail('En proceso', 21);
    getDetail.mockReturnValue(of(detail));

    await expect(
      resolve({ idProducto: '21', idProceso: '200', nivel: '3' })
    ).resolves.toMatchObject({ productLevelId: 3 });
    expect(getDegreePrograms).not.toHaveBeenCalled();
  });

  it.each(['0', 'abc', '99'])(
    'falls back to the catalog when the nivel param is %s',
    async nivel => {
      const detail = createDetail('En proceso', 21);
      getDetail.mockReturnValue(of(detail));
      getDegreePrograms.mockReturnValue(
        of([{ productId: 21, productLevelId: 2, productName: 'Tecnicatura' }])
      );

      await expect(resolve({ idProducto: '21', idProceso: '200', nivel })).resolves.toMatchObject({
        productLevelId: 2,
      });
      expect(getDegreePrograms).toHaveBeenCalled();
    }
  );

  it('keeps a null level when the degreePrograms catalog fails', async () => {
    const detail = createDetail('En proceso', 21);
    getDetail.mockReturnValue(of(detail));
    getDegreePrograms.mockReturnValue(throwError(() => new Error('failed')));

    await expect(resolve({ idProducto: '21', idProceso: '200' })).resolves.toEqual({
      intent: 'resume',
      detail,
      productId: 21,
      admissionProcessId: 200,
      offeringIds: [],
      productLevelId: null,
    });
  });

  it('resolves the "reactivar" intent when modo=reactivar is present', async () => {
    const detail = createDetail('Cancelada');
    getDetail.mockReturnValue(of(detail));

    await expect(
      resolve({ idProducto: '20', idProceso: '200', modo: 'reactivar' })
    ).resolves.toEqual({
      intent: 'reactivate',
      detail,
      preEnrollment: null,
      productId: 20,
      admissionProcessId: 200,
      offeringIds: [],
      productLevelId: null,
    });
    expect(getDetail).toHaveBeenCalledWith(20, 200, null);
  });

  it('uses the transient reactivation response without loading detail', async () => {
    TestBed.inject(EnrollmentResumeContextStore).saveReactivation(
      {
        productId: 20,
        admissionProcessId: 200,
        offeringIds: [310],
        enrollmentIds: [7010],
      },
      REACTIVATION_RESPONSE
    );

    await expect(
      resolve({ idProducto: '20', idProceso: '200', modo: 'reactivar' })
    ).resolves.toEqual({
      intent: 'reactivate',
      detail: null,
      preEnrollment: REACTIVATION_RESPONSE,
      productId: 20,
      admissionProcessId: 200,
      offeringIds: [310],
      productLevelId: null,
    });
    expect(getDetail).not.toHaveBeenCalled();
    expect(TestBed.inject(EnrollmentResumeContextStore).takeReactivation(20, 200)).toBeNull();
  });

  it('resolves the "nueva" intent without valid parameters so the flow starts fresh', async () => {
    await expect(resolve({ idProducto: '0', idProceso: '200' })).resolves.toEqual({
      intent: 'new',
    });
    await expect(resolve({})).resolves.toEqual({ intent: 'new' });
    expect(getDetail).not.toHaveBeenCalled();
  });

  it('degrades to detail:null when the request fails but keeps the intent', async () => {
    getDetail.mockReturnValue(throwError(() => new Error('failed')));

    await expect(resolve({ idProducto: '20', idProceso: '200' })).resolves.toEqual({
      intent: 'resume',
      detail: null,
      productId: 20,
      admissionProcessId: 200,
      offeringIds: [],
      productLevelId: null,
    });
  });

  // Con el Detalle caído los params siguen alcanzando para el nivel del producto, así
  // que el paso 2 arranca con el tipo de propuesta correcto.
  it('resolves the product level from the URL param when the detail fails', async () => {
    getDetail.mockReturnValue(throwError(() => new Error('failed')));
    getDegreePrograms.mockReturnValue(
      of([
        {
          productId: 21,
          productLevelId: 3,
          productName: 'Programa de Asesoramiento Financiero',
          productLevelName: 'Actualización profesional',
        },
      ])
    );

    await expect(resolve({ idProducto: '21', idProceso: '200' })).resolves.toEqual({
      intent: 'resume',
      detail: null,
      productId: 21,
      admissionProcessId: 200,
      offeringIds: [],
      productLevelId: 3,
    });
  });

  it('keeps unique positive offer ids from the resume URL', async () => {
    const detail = createDetail('En proceso', 40);
    getDetail.mockReturnValue(of(detail));

    await expect(
      resolve({
        idProducto: '40',
        idProceso: '210',
        idOferta: ['310', '311', '310', '0', 'invalid'],
      })
    ).resolves.toEqual(
      expect.objectContaining({
        intent: 'resume',
        offeringIds: [310, 311],
      })
    );
  });

  it('prefers the offers saved when continuing from the dashboard', async () => {
    getDetail.mockReturnValue(of(createDetail('En proceso', 40)));
    TestBed.inject(EnrollmentResumeContextStore).save({
      productId: 40,
      admissionProcessId: 210,
      offeringIds: [310, 311],
      enrollmentIds: [7010, 7011],
    });

    await expect(resolve({ idProducto: '40', idProceso: '210' })).resolves.toEqual(
      expect.objectContaining({ offeringIds: [310, 311] })
    );
  });

  it('keeps an explicit detail URL authoritative over a previous session', async () => {
    getDetail.mockReturnValue(of(createDetail('En proceso', 40)));
    TestBed.inject(EnrollmentResumeContextStore).save({
      productId: 40,
      admissionProcessId: 210,
      offeringIds: [310, 311],
      enrollmentIds: [7010, 7011],
    });

    await expect(
      resolve({ idProducto: '40', idProceso: '210', idOferta: ['320'] })
    ).resolves.toEqual(expect.objectContaining({ offeringIds: [320] }));
  });

  it('resolves "nueva" when a parameter is not numeric', async () => {
    await expect(resolve({ idProducto: 'abc', idProceso: '200' })).resolves.toEqual({
      intent: 'new',
    });
    await expect(resolve({ idProducto: '20', idProceso: 'abc' })).resolves.toEqual({
      intent: 'new',
    });
    expect(getDetail).not.toHaveBeenCalled();
  });

  it('resolves "nueva" when only one parameter is present', async () => {
    await expect(resolve({ idProducto: '20' })).resolves.toEqual({ intent: 'new' });
    await expect(resolve({ idProceso: '200' })).resolves.toEqual({ intent: 'new' });
    expect(getDetail).not.toHaveBeenCalled();
  });

  it('rejects negative and unsafe integer values, falling back to "nueva"', async () => {
    await expect(resolve({ idProducto: '-5', idProceso: '200' })).resolves.toEqual({
      intent: 'new',
    });
    await expect(resolve({ idProducto: '9007199254740993', idProceso: '200' })).resolves.toEqual({
      intent: 'new',
    });
    expect(getDetail).not.toHaveBeenCalled();
  });

  describe('resolveEntryIntent', () => {
    it('maps valid params + modo to the right intent', () => {
      expect(resolveEntryIntent(convertToParamMap({}))).toBe('new');
      expect(resolveEntryIntent(convertToParamMap({ idProducto: '20', idProceso: '200' }))).toBe(
        'resume'
      );
      expect(
        resolveEntryIntent(
          convertToParamMap({ idProducto: '20', idProceso: '200', modo: 'reactivar' })
        )
      ).toBe('reactivate');
      // modo=reactivar sin oferta válida sigue siendo nueva.
      expect(resolveEntryIntent(convertToParamMap({ modo: 'reactivar' }))).toBe('new');
    });
  });

  function resolve(
    queryParams: Record<string, string | string[]>
  ): Promise<EnrollmentEntryResolved> {
    const route = new ActivatedRouteSnapshot();
    Object.defineProperty(route, 'queryParamMap', { value: convertToParamMap(queryParams) });
    const result = TestBed.runInInjectionContext(() =>
      enrollmentDetailResolver(route, {} as RouterStateSnapshot)
    );
    return firstValueFrom(result as Observable<EnrollmentEntryResolved>);
  }

  function createDetail(status: string, productId: number | null = null): EnrollmentDetail {
    return {
      status,
      summary:
        productId === null
          ? null
          : {
              offeringId: 300,
              productId,
              degreeProgram: 'Programa de Asesoramiento Financiero',
              intake: 'Abril 2026',
              shift: 'Noche',
            },
      interests: [],
      pendingPayment: null,
      minimumDeposit: null,
      confirmed: null,
    };
  }
});
