import { computed } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';

import { Auth } from '../../features/auth/services/auth';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let isAuthenticatedValue: boolean;

  beforeEach(() => {
    isAuthenticatedValue = true;

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: Auth,
          useValue: {
            isAuthenticated: computed(() => isAuthenticatedValue),
          },
        },
      ],
    });
  });

  it('should allow authenticated users', () => {
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(result).toBe(true);
  });

  it('should redirect anonymous users to login', () => {
    isAuthenticatedValue = false;
    const router = TestBed.inject(Router);

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(router.serializeUrl(result as UrlTree)).toBe('/login');
  });
});
