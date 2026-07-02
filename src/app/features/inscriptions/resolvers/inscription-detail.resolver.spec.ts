import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  convertToParamMap,
  provideRouter,
  RouterStateSnapshot,
} from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import type { InscripcionDetail } from '../models/inscription-detail';
import { Inscripciones } from '../services/inscriptions';
import { inscriptionDetailResolver } from './inscription-detail.resolver';

describe('inscriptionDetailResolver', () => {
  let getDetail: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    getDetail = vi.fn();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: Inscripciones, useValue: { getDetail } }],
    });
  });

  it('loads detail with validated query parameters', async () => {
    const detail = createDetail('Pago pendiente');
    getDetail.mockReturnValue(of(detail));

    await expect(resolve({ idProducto: '20', idProceso: '200' })).resolves.toBe(detail);
    expect(getDetail).toHaveBeenCalledWith(20, 200);
  });

  it('resolves to null without valid parameters so the flow starts fresh', async () => {
    await expect(resolve({ idProducto: '0', idProceso: '200' })).resolves.toBeNull();
    await expect(resolve({})).resolves.toBeNull();
    expect(getDetail).not.toHaveBeenCalled();
  });

  it('resolves to null when the request fails', async () => {
    getDetail.mockReturnValue(throwError(() => new Error('failed')));

    await expect(resolve({ idProducto: '20', idProceso: '200' })).resolves.toBeNull();
  });

  function resolve(queryParams: Record<string, string>): Promise<InscripcionDetail | null> {
    const route = new ActivatedRouteSnapshot();
    Object.defineProperty(route, 'queryParamMap', { value: convertToParamMap(queryParams) });
    const result = TestBed.runInInjectionContext(() =>
      inscriptionDetailResolver(route, {} as RouterStateSnapshot)
    );
    return firstValueFrom(result as Observable<InscripcionDetail | null>);
  }

  function createDetail(estado: string): InscripcionDetail {
    return { estado, detalle: null, pagoPendiente: null, confirmada: null };
  }
});
