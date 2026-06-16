import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { AuthSessionService } from '../../services/auth-session';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let component: Login;
  let authMock: {
    login: ReturnType<typeof vi.fn>;
  };
  let navigateByUrlSpy: ReturnType<typeof vi.spyOn>;
  let navigateSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    authMock = {
      login: vi.fn().mockReturnValue(
        of({
          kind: 'authenticated',
          session: {
            token: 'token-123',
            documentType: 'CI',
            documentNumber: '12345678',
            primerNombre: 'Ana',
            expiresAt: null,
          },
        })
      ),
    };

    TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), { provide: AuthSessionService, useValue: authMock }],
    });

    navigateByUrlSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should create the login page', () => {
    expect(component).toBeTruthy();
  });

  it('should submit document credentials and redirect to home', () => {
    component['form'].setValue({
      documentType: 'CI',
      documentNumber: '11111111',
      password: 'secret',
    });

    component['submit']();

    expect(authMock.login).toHaveBeenCalledWith({
      documentType: 'CI',
      documentNumber: '11111111',
      password: 'secret',
    });
    expect(component['successMessage']()).toBe('Sesión iniciada correctamente.');
    expect(component['form'].controls.password.value).toBe('');
    expect(navigateByUrlSpy).toHaveBeenCalledWith('/inicio');
  });

  it('should not submit when form is invalid', () => {
    component['submit']();

    expect(authMock.login).not.toHaveBeenCalled();
  });

  it('should clear a previous API error before validating a new submission', () => {
    component['error'].set('Credenciales inválidas.');

    component['submit']();

    expect(component['error']()).toBeNull();
    expect(authMock.login).not.toHaveBeenCalled();
  });

  it('should expose auth errors in UI state', () => {
    authMock.login.mockReturnValue(
      throwError(() => ({
        status: 401,
        message: 'Credenciales inválidas.',
        action: 'notify',
        isOperationResult: true,
        originalError: new Error('boom'),
      }))
    );
    component['form'].setValue({
      documentType: 'CI',
      documentNumber: '11111111',
      password: 'secret',
    });

    component['submit']();

    expect(component['error']()).toBe('Credenciales inválidas.');
  });

  it('should navigate to /verificar-codigo with state when 2FA is required', () => {
    authMock.login.mockReturnValue(
      of({
        kind: 'twoFactorRequired',
        sessionId: 'ab4df653422a4c19be2867c08355fa27',
        maskedEmail: 'c******a@gmail.******',
        message: 'Se envió un código de verificación a tu correo electrónico.',
      })
    );
    component['form'].setValue({
      documentType: 'CI',
      documentNumber: '11111111',
      password: 'secret',
    });

    component['submit']();

    expect(navigateSpy).toHaveBeenCalledWith(['/verificar-codigo'], {
      state: {
        email: 'c******a@gmail.******',
        sessionId: 'ab4df653422a4c19be2867c08355fa27',
        documentType: 'CI',
        documentNumber: '11111111',
      },
    });
    expect(navigateByUrlSpy).not.toHaveBeenCalled();
    expect(component['form'].controls.password.value).toBe('');
  });
});
