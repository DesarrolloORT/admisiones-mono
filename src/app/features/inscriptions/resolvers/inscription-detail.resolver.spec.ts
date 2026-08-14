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
import { InscriptionResumeContextStore } from '../services/inscription-resume-context';
import { Inscripciones } from '../services/inscriptions';
import { inscriptionDetailResolver, resolveEntryIntent } from './inscription-detail.resolver';

const REACTIVATION_RESPONSE = {
  confirmada: false,
  enEspera: false,
  idInscripcion: 7010,
  fechaVencimientoPago: '2027-03-04',
  seniaInscripcion: 15500,
  saldoCuenta: 1200,
  resumen: { carrera: 'Sistemas', comienzo: 'Marzo 2027', turno: 'Noche' },
  seminarios: [
    {
      idInscripcion: 7010,
      idOferta: 310,
      nombre: 'Seminario',
      comienzo: 'Marzo 2027',
      turno: 'Noche',
    },
  ],
};

describe('inscriptionDetailResolver', () => {
  let getDetail: ReturnType<typeof vi.fn>;
  let getCareers: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    sessionStorage.clear();
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
      idProducto: 20,
      idProceso: 200,
      idOfertas: [],
      idNivelProducto: null,
    });
    expect(getDetail).toHaveBeenCalledWith(20, 200, null);
  });

  it('propagates the estado query param to the detail request', async () => {
    const detail = createDetail('Pago pendiente');
    getDetail.mockReturnValue(of(detail));

    await resolve({ idProducto: '20', idProceso: '200', estado: 'Pago pendiente' });

    expect(getDetail).toHaveBeenCalledWith(20, 200, 'Pago pendiente');
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
      idProducto: 21,
      idProceso: 200,
      idOfertas: [],
      idNivelProducto: 3,
    });
  });

  it('takes the product level from the nivel param without querying the catalog', async () => {
    const detail = createDetail('En proceso', 21);
    getDetail.mockReturnValue(of(detail));

    await expect(
      resolve({ idProducto: '21', idProceso: '200', nivel: '3' })
    ).resolves.toMatchObject({ idNivelProducto: 3 });
    expect(getCareers).not.toHaveBeenCalled();
  });

  it.each(['0', 'abc', '99'])(
    'falls back to the catalog when the nivel param is %s',
    async nivel => {
      const detail = createDetail('En proceso', 21);
      getDetail.mockReturnValue(of(detail));
      getCareers.mockReturnValue(
        of([{ idProducto: 21, idNivelProducto: 2, nombreProducto: 'Tecnicatura' }])
      );

      await expect(resolve({ idProducto: '21', idProceso: '200', nivel })).resolves.toMatchObject({
        idNivelProducto: 2,
      });
      expect(getCareers).toHaveBeenCalled();
    }
  );

  it('keeps a null level when the careers catalog fails', async () => {
    const detail = createDetail('En proceso', 21);
    getDetail.mockReturnValue(of(detail));
    getCareers.mockReturnValue(throwError(() => new Error('failed')));

    await expect(resolve({ idProducto: '21', idProceso: '200' })).resolves.toEqual({
      intent: 'retomar',
      detail,
      idProducto: 21,
      idProceso: 200,
      idOfertas: [],
      idNivelProducto: null,
    });
  });

  it('resolves the "reactivar" intent when modo=reactivar is present', async () => {
    const detail = createDetail('Cancelada');
    getDetail.mockReturnValue(of(detail));

    await expect(
      resolve({ idProducto: '20', idProceso: '200', modo: 'reactivar' })
    ).resolves.toEqual({
      intent: 'reactivar',
      detail,
      preEnrollment: null,
      idProducto: 20,
      idProceso: 200,
      idOfertas: [],
      idNivelProducto: null,
    });
    expect(getDetail).toHaveBeenCalledWith(20, 200, null);
  });

  it('uses the transient reactivation response without loading detail', async () => {
    TestBed.inject(InscriptionResumeContextStore).saveReactivation(
      {
        idProducto: 20,
        idProceso: 200,
        idOfertas: [310],
        idInscripciones: [7010],
      },
      REACTIVATION_RESPONSE
    );

    await expect(
      resolve({ idProducto: '20', idProceso: '200', modo: 'reactivar' })
    ).resolves.toEqual({
      intent: 'reactivar',
      detail: null,
      preEnrollment: REACTIVATION_RESPONSE,
      idProducto: 20,
      idProceso: 200,
      idOfertas: [310],
      idNivelProducto: null,
    });
    expect(getDetail).not.toHaveBeenCalled();
    expect(TestBed.inject(InscriptionResumeContextStore).takeReactivation(20, 200)).toBeNull();
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
      idProducto: 20,
      idProceso: 200,
      idOfertas: [],
      idNivelProducto: null,
    });
  });

  // Con el Detalle caído los params siguen alcanzando para el nivel del producto, así
  // que el paso 2 arranca con el tipo de propuesta correcto.
  it('resolves the product level from the URL param when the detail fails', async () => {
    getDetail.mockReturnValue(throwError(() => new Error('failed')));
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
      detail: null,
      idProducto: 21,
      idProceso: 200,
      idOfertas: [],
      idNivelProducto: 3,
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
        intent: 'retomar',
        idOfertas: [310, 311],
      })
    );
  });

  it('prefers the offers saved when continuing from the dashboard', async () => {
    getDetail.mockReturnValue(of(createDetail('En proceso', 40)));
    TestBed.inject(InscriptionResumeContextStore).save({
      idProducto: 40,
      idProceso: 210,
      idOfertas: [310, 311],
      idInscripciones: [7010, 7011],
    });

    await expect(resolve({ idProducto: '40', idProceso: '210' })).resolves.toEqual(
      expect.objectContaining({ idOfertas: [310, 311] })
    );
  });

  it('keeps an explicit detail URL authoritative over a previous session', async () => {
    getDetail.mockReturnValue(of(createDetail('En proceso', 40)));
    TestBed.inject(InscriptionResumeContextStore).save({
      idProducto: 40,
      idProceso: 210,
      idOfertas: [310, 311],
      idInscripciones: [7010, 7011],
    });

    await expect(
      resolve({ idProducto: '40', idProceso: '210', idOferta: ['320'] })
    ).resolves.toEqual(expect.objectContaining({ idOfertas: [320] }));
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

  function resolve(
    queryParams: Record<string, string | string[]>
  ): Promise<InscripcionEntryResolved> {
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
              comienzo: 'Abril 2026',
              turno: 'Noche',
            },
      intereses: [],
      pagoPendiente: null,
      seniaMinima: null,
      confirmada: null,
    };
  }
});
