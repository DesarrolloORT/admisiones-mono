import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { firstValueFrom, Observable, of } from 'rxjs';
import { vi } from 'vitest';

import { AuthSessionService } from '../../features/auth/services/auth-session';
import { authMatchGuard } from './auth';

describe('authMatchGuard', () => {
  let authSessionMock: { ensureAuthenticatedSession: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authSessionMock = {
      ensureAuthenticatedSession: vi.fn().mockReturnValue(of(true)),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: authSessionMock,
        },
      ],
    });
  });

  it('should allow authenticated users', async () => {
    const result = TestBed.runInInjectionContext(() =>
      authMatchGuard({} as never, [], {} as never)
    );

    await expect(firstValueFrom(result as Observable<true | UrlTree>)).resolves.toBe(true);
    expect(authSessionMock.ensureAuthenticatedSession).toHaveBeenCalled();
  });

  it('should redirect anonymous users to login', async () => {
    authSessionMock.ensureAuthenticatedSession.mockReturnValue(of(false));
    const router = TestBed.inject(Router);

    const result = TestBed.runInInjectionContext(() =>
      authMatchGuard({} as never, [], {} as never)
    );
    const resolved = await firstValueFrom(result as Observable<true | UrlTree>);

    expect(router.serializeUrl(resolved as UrlTree)).toBe('/iniciar-sesion');
  });
});
