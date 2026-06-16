import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { PasswordActivationService } from '../../services/password-activation';
import { SetPassword } from './set-password';

describe('SetPassword', () => {
  let fixture: ComponentFixture<SetPassword>;
  let component: SetPassword;
  let passwordActivationMock: {
    activateLink: ReturnType<typeof vi.fn>;
    completePassword: ReturnType<typeof vi.fn>;
  };

  function setup(queryParams: Record<string, string | null> = { token: 'token-123' }) {
    passwordActivationMock = {
      activateLink: vi.fn().mockReturnValue(of(undefined)),
      completePassword: vi.fn().mockReturnValue(of(undefined)),
    };

    TestBed.configureTestingModule({
      imports: [SetPassword],
      providers: [
        provideRouter([]),
        { provide: PasswordActivationService, useValue: passwordActivationMock },
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

  it('should complete password creation and navigate to login', () => {
    setup();
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    component['form'].setValue({
      password: 'NuevaPassword1!',
      confirmPassword: 'NuevaPassword1!',
    });
    component['submit']();

    expect(passwordActivationMock.completePassword).toHaveBeenCalledWith('NuevaPassword1!');
    expect(navigateSpy).toHaveBeenCalledWith(['/iniciar-sesion']);
  });

  it('should surface password creation errors', () => {
    setup();
    passwordActivationMock.completePassword.mockReturnValue(
      throwError(() => new Error('request failed'))
    );

    component['form'].setValue({
      password: 'NuevaPassword1!',
      confirmPassword: 'NuevaPassword1!',
    });
    component['submit']();

    expect(component['error']()).toBe('No se pudo crear la contraseña. Intentá de nuevo.');
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
