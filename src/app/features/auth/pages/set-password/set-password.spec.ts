import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { AuthSessionService } from '../../services/auth-session';
import { PasswordActivationService } from '../../services/password-activation';
import { SetPassword } from './set-password';

describe('SetPassword', () => {
  let fixture: ComponentFixture<SetPassword>;
  let component: SetPassword;
  let passwordActivationMock: {
    activateLink: ReturnType<typeof vi.fn>;
    completePassword: ReturnType<typeof vi.fn>;
  };
  let authSessionMock: {
    hydrateAuthenticatedSession: ReturnType<typeof vi.fn>;
  };
  let snackbarMock: { error: ReturnType<typeof vi.fn>; success: ReturnType<typeof vi.fn> };

  function setup(queryParams: Record<string, string | null> = { token: 'token-123' }) {
    passwordActivationMock = {
      activateLink: vi.fn().mockReturnValue(of(undefined)),
      completePassword: vi.fn().mockReturnValue(of(undefined)),
    };
    authSessionMock = {
      hydrateAuthenticatedSession: vi.fn().mockReturnValue(of(undefined)),
    };
    snackbarMock = { error: vi.fn(), success: vi.fn() };

    TestBed.configureTestingModule({
      imports: [SetPassword],
      providers: [
        provideRouter([]),
        { provide: PasswordActivationService, useValue: passwordActivationMock },
        { provide: AuthSessionService, useValue: authSessionMock },
        { provide: SnackbarHandler, useValue: snackbarMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: { get: (key: string) => queryParams[key] ?? null },
            },
          },
        },
      ],
    });

    fixture = TestBed.createComponent(SetPassword);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should show creation texts by default', () => {
    setup();
    expect(component).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Creá tu contraseña');
  });

  it('should show recovery texts when flow=recovery', () => {
    setup({ token: 'token-123', flow: 'recovery' });
    expect(fixture.nativeElement.textContent).toContain('Recuperar acceso');
  });

  it('should activate the token on creation', () => {
    setup();

    expect(passwordActivationMock.activateLink).toHaveBeenCalledWith('token-123');
    expect(component['tokenError']()).toBeNull();
  });

  it('should render accessible visibility toggles for both password fields', () => {
    setup();
    const buttons = fixture.nativeElement.querySelectorAll('.password-visibility-toggle');

    expect(buttons).toHaveLength(2);
    expect([...buttons].every(button => button.getAttribute('aria-pressed') === 'false')).toBe(
      true
    );
  });

  it('should complete password creation, hydrate the session and navigate home', () => {
    setup();
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    component['form'].setValue({
      password: 'NuevaPassword1!',
      confirmPassword: 'NuevaPassword1!',
    });
    component['submit']();

    expect(passwordActivationMock.completePassword).toHaveBeenCalledWith('NuevaPassword1!');
    expect(authSessionMock.hydrateAuthenticatedSession).toHaveBeenCalled();
    expect(navigateSpy).toHaveBeenCalledWith('/inicio');
  });

  it('should complete recovery and return to login without creating a session', () => {
    setup({ token: 'token-123', flow: 'recovery' });
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    component['form'].setValue({
      password: 'NuevaPassword1!',
      confirmPassword: 'NuevaPassword1!',
    });
    component['submit']();

    expect(authSessionMock.hydrateAuthenticatedSession).not.toHaveBeenCalled();
    expect(navigateSpy).toHaveBeenCalledWith('/iniciar-sesion');
  });

  it('should report an expired or already used token', () => {
    setup();
    passwordActivationMock.activateLink.mockReturnValue(throwError(() => new Error('expired')));

    component['activateToken']();

    expect(component['tokenError']()).toContain('expiró');
  });

  it('should enable submit only when the password form is valid', () => {
    setup();

    expect(component['canSubmit']()).toBe(false);

    component['form'].setValue({
      password: 'NuevaPassword1!',
      confirmPassword: 'OtraPassword1!',
    });

    expect(component['canSubmit']()).toBe(false);

    component['form'].controls.confirmPassword.setValue('NuevaPassword1!');

    expect(component['canSubmit']()).toBe(true);
  });

  it('should show password creation errors in snackbar', () => {
    setup();
    passwordActivationMock.completePassword.mockReturnValue(
      throwError(() => new Error('request failed'))
    );

    component['form'].setValue({
      password: 'NuevaPassword1!',
      confirmPassword: 'NuevaPassword1!',
    });
    component['submit']();

    expect(snackbarMock.error).toHaveBeenCalledWith(
      'No se pudo crear la contraseña. Intentá de nuevo.'
    );
  });

  it('should expose password strength only when there is input', () => {
    setup();

    expect(component['strength']()).toBeNull();

    component['form'].controls.password.setValue('NuevaPassword1!');

    const strength = component['strength']();
    expect(strength).not.toBeNull();
    expect(strength!.score).toBeGreaterThan(0);
    expect(strength!.score).toBeLessThanOrEqual(100);
    expect(['weak', 'moderate', 'strong']).toContain(strength!.level);
    expect(strength!.rating).toBeTruthy();
  });
});
