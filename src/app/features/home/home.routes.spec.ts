import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, RouterStateSnapshot } from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { HomeApi } from './api/home.api';
import { homeResolver, routes } from './home.routes';
import { HomeResolved } from './models/home-data';

describe('home routes', () => {
  let getMyEnrollments: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    getMyEnrollments = vi.fn().mockReturnValue(of([]));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: HomeApi,
          useValue: { getMyEnrollments, getMyScholarships: () => of([]) },
        },
      ],
    });
  });

  it('should resolve home data whenever Home is activated', () => {
    const homeRoute = routes[0].children?.find(route => route.path === '');

    expect(homeRoute?.resolve?.['homeData']).toBe(homeResolver);
    expect(homeRoute?.runGuardsAndResolvers).toBe('always');
  });

  it('should resolve the dashboard data on success', async () => {
    await expect(resolve()).resolves.toEqual({ enrollments: [], scholarships: [] });
  });

  it('should carry the backend message through the load failure', async () => {
    getMyEnrollments.mockReturnValue(
      throwError(() => ({
        status: 403,
        message: 'Tu usuario no tiene inscripciones habilitadas.',
        action: 'notify',
        isOperationResult: true,
        originalError: new Error('forbidden'),
      }))
    );

    await expect(resolve()).resolves.toEqual({
      loadError: 'Tu usuario no tiene inscripciones habilitadas.',
    });
  });

  it('should fall back to the generic copy when the error carries no message', async () => {
    getMyEnrollments.mockReturnValue(throwError(() => new Error('network down')));

    await expect(resolve()).resolves.toEqual({
      loadError: 'Hubo un error al cargar tu información.',
    });
  });

  function resolve(): Promise<HomeResolved> {
    const result = TestBed.runInInjectionContext(() =>
      homeResolver(new ActivatedRouteSnapshot(), {} as RouterStateSnapshot)
    );
    return firstValueFrom(result as Observable<HomeResolved>);
  }
});
