import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { PasswordActivationService } from '../services/password-activation';
import { SetPasswordFacade } from './set-password.facade';

describe('SetPasswordFacade', () => {
  let facade: SetPasswordFacade;
  let passwordActivationMock: {
    activateLink: ReturnType<typeof vi.fn>;
    completePassword: ReturnType<typeof vi.fn>;
  };
  let routerMock: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    passwordActivationMock = {
      activateLink: vi.fn().mockReturnValue(of(undefined)),
      completePassword: vi.fn().mockReturnValue(of(undefined)),
    };
    routerMock = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        SetPasswordFacade,
        { provide: PasswordActivationService, useValue: passwordActivationMock },
        { provide: Router, useValue: routerMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: { get: () => 'token-123' },
            },
          },
        },
      ],
    });

    facade = TestBed.inject(SetPasswordFacade);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should activate the token on creation', () => {
    expect(passwordActivationMock.activateLink).toHaveBeenCalledWith('token-123');
    expect(facade.tokenError()).toBeNull();
  });

  it('should complete password creation and navigate to login', () => {
    facade.form.setValue({
      password: 'NuevaPassword1!',
      confirmPassword: 'NuevaPassword1!',
    });

    facade.submit();

    expect(passwordActivationMock.completePassword).toHaveBeenCalledWith('NuevaPassword1!');
    expect(routerMock.navigate).toHaveBeenCalledWith(['/iniciar-sesion']);
  });

  it('should surface password creation errors', () => {
    passwordActivationMock.completePassword.mockReturnValue(
      throwError(() => new Error('request failed'))
    );
    facade.form.setValue({
      password: 'NuevaPassword1!',
      confirmPassword: 'NuevaPassword1!',
    });

    facade.submit();

    expect(facade.error()).toBe('No se pudo crear la contraseña. Intentá de nuevo.');
  });

  it('should expose password strength from ngx-utils only when there is input', () => {
    expect(facade.strength()).toBeNull();

    facade.form.controls.password.setValue('NuevaPassword1!');

    const strength = facade.strength();
    expect(strength).not.toBeNull();
    expect(strength!.score).toBeGreaterThan(0);
    expect(strength!.score).toBeLessThanOrEqual(100);
    expect(['weak', 'moderate', 'strong']).toContain(strength!.level);
    expect(strength!.rating).toBeTruthy();
  });
});

