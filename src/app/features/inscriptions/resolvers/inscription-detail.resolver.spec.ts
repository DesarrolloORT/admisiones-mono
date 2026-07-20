import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  convertToParamMap,
  provideRouter,
  RouterStateSnapshot,
} from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../catalogs/services/catalogs';
import type { InscripcionDetail } from '../models/inscription-detail';
import type { InscripcionEntryResolved } from '../models/inscription-entry';
import { Inscripciones } from '../services/inscriptions';
import { inscriptionDetailResolver, resolveEntryIntent } from './inscription-detail.resolver';

describe('inscriptionDetailResolver', () => {
  let getDetail: ReturnType<typeof vi.fn>;
  let getCareers: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    getDetail = vi.fn();
    getCareers = vi.fn().mockReturnValue(of([]));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: Inscripciones, useValue: { getDetail } },
        { provide: Catalogs, useValue: { getCareers } },
      ],
    });
  });

  it('resolves the "retomar" intent with the loaded detail', async () => {
    const detail = createDetail('Pago pendiente');
    getDetail.mockReturnValue(of(detail));

    await expect(resolve({ idProducto: '20', idProceso: '200' })).resolves.toEqual({
      intent: 'retomar',
      detail,
      idNivelProducto: null,
    });
    expect(getDetail).toHaveBeenCalledWith(20, 200);
  });

  it('resolves the product level from the careers catalog', async () => {
    const detail = createDetail('En proceso', 21);
    getDetail.mockReturnValue(of(detail));
    getCareers.mockReturnValue(
      of([
        {
          idProducto: 21,
          idNivelProducto: 3,
          nombreProducto: 'Programa de Asesoramiento Financiero',
          nombreNivelProducto: 'Actualización profesional',
        },
      ])
    );

    await expect(resolve({ idProducto: '21', idProceso: '200' })).resolves.toEqual({
      intent: 'retomar',
      detail,
      idNivelProducto: 3,
    });
  });

  it('keeps a null level when the careers catalog fails', async () => {
    const detail = createDetail('En proceso', 21);
    getDetail.mockReturnValue(of(detail));
    getCareers.mockReturnValue(throwError(() => new Error('failed')));

    await expect(resolve({ idProducto: '21', idProceso: '200' })).resolves.toEqual({
      intent: 'retomar',
      detail,
      idNivelProducto: null,
    });
  });

  it('resolves the "reactivar" intent when modo=reactivar is present', async () => {
    const detail = createDetail('Cancelada');
    getDetail.mockReturnValue(of(detail));

    await expect(
      resolve({ idProducto: '20', idProceso: '200', modo: 'reactivar' })
    ).resolves.toEqual({ intent: 'reactivar', detail, idNivelProducto: null });
    expect(getDetail).toHaveBeenCalledWith(20, 200);
  });

  it('resolves the "nueva" intent without valid parameters so the flow starts fresh', async () => {
    await expect(resolve({ idProducto: '0', idProceso: '200' })).resolves.toEqual({
      intent: 'nueva',
    });
    await expect(resolve({})).resolves.toEqual({ intent: 'nueva' });
    expect(getDetail).not.toHaveBeenCalled();
  });

  it('degrades to detail:null when the request fails but keeps the intent', async () => {
    getDetail.mockReturnValue(throwError(() => new Error('failed')));

    await expect(resolve({ idProducto: '20', idProceso: '200' })).resolves.toEqual({
      intent: 'retomar',
      detail: null,
      idNivelProducto: null,
    });
  });

  it('resolves "nueva" when a parameter is not numeric', async () => {
    await expect(resolve({ idProducto: 'abc', idProceso: '200' })).resolves.toEqual({
      intent: 'nueva',
    });
    await expect(resolve({ idProducto: '20', idProceso: 'abc' })).resolves.toEqual({
      intent: 'nueva',
    });
    expect(getDetail).not.toHaveBeenCalled();
  });

  it('resolves "nueva" when only one parameter is present', async () => {
    await expect(resolve({ idProducto: '20' })).resolves.toEqual({ intent: 'nueva' });
    await expect(resolve({ idProceso: '200' })).resolves.toEqual({ intent: 'nueva' });
    expect(getDetail).not.toHaveBeenCalled();
  });

  it('rejects negative and unsafe integer values, falling back to "nueva"', async () => {
    await expect(resolve({ idProducto: '-5', idProceso: '200' })).resolves.toEqual({
      intent: 'nueva',
    });
    await expect(resolve({ idProducto: '9007199254740993', idProceso: '200' })).resolves.toEqual({
      intent: 'nueva',
    });
    expect(getDetail).not.toHaveBeenCalled();
  });

  describe('resolveEntryIntent', () => {
    it('maps valid params + modo to the right intent', () => {
      expect(resolveEntryIntent(convertToParamMap({}))).toBe('nueva');
      expect(resolveEntryIntent(convertToParamMap({ idProducto: '20', idProceso: '200' }))).toBe(
        'retomar'
      );
      expect(
        resolveEntryIntent(
          convertToParamMap({ idProducto: '20', idProceso: '200', modo: 'reactivar' })
        )
      ).toBe('reactivar');
      // modo=reactivar sin oferta válida sigue siendo nueva.
      expect(resolveEntryIntent(convertToParamMap({ modo: 'reactivar' }))).toBe('nueva');
    });
  });

  function resolve(queryParams: Record<string, string>): Promise<InscripcionEntryResolved> {
    const route = new ActivatedRouteSnapshot();
    Object.defineProperty(route, 'queryParamMap', { value: convertToParamMap(queryParams) });
    const result = TestBed.runInInjectionContext(() =>
      inscriptionDetailResolver(route, {} as RouterStateSnapshot)
    );
    return firstValueFrom(result as Observable<InscripcionEntryResolved>);
  }

  function createDetail(estado: string, idProducto: number | null = null): InscripcionDetail {
    return {
      estado,
      detalle:
        idProducto === null
          ? null
          : {
              idOferta: 300,
              idProducto,
              carrera: 'Programa de Asesoramiento Financiero',
              idComienzo: 200,
              comienzo: 'Abril 2026',
              idTurno: 10,
              turno: 'Noche',
            },
      pagoPendiente: null,
      seniaMinima: null,
      confirmada: null,
    };
  }
});
