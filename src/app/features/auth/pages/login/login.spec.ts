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

  beforeEach(() => {
    authMock = {
      login: vi.fn().mockReturnValue(
        of({
          kind: 'authenticated',
          session: {
            documentType: 'CI',
            documentNumber: '12345678',
            primerNombre: 'Ana',
          },
        })
      ),
    };
    TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), { provide: AuthSessionService, useValue: authMock }],
    });

    navigateByUrlSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
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

  it('should expose the password visibility toggle as a pressed button', async () => {
    const button = fixture.nativeElement.querySelector(
      '.password-visibility-toggle'
    ) as HTMLButtonElement;

    expect(button.getAttribute('aria-pressed')).toBe('false');

    button.click();
    await fixture.whenStable();

    expect(button.getAttribute('aria-pressed')).toBe('true');
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
    expect(component['form'].controls.password.value).toBe('');
    expect(navigateByUrlSpy).toHaveBeenCalledWith('/inicio');
  });

  it('should not submit when form is invalid', () => {
    component['submit']();

    expect(authMock.login).not.toHaveBeenCalled();
  });

  it('should show auth errors inline', () => {
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

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Credenciales inválidas.');
  });

  it('should navigate to email confirmation without putting identity in router state', () => {
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

    expect(navigateByUrlSpy).toHaveBeenCalledWith('/confirmacion-correo/verificar-codigo');
    expect(component['form'].controls.password.value).toBe('');
  });
});
