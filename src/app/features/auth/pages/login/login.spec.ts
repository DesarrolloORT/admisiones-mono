import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { Catalogs } from '../../../catalogs/services/catalogs';
import { AuthSessionService } from '../../services/auth-session';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let component: Login;
  let authMock: {
    login: ReturnType<typeof vi.fn>;
  };
  let catalogsMock: {
    getDocumentTypes: ReturnType<typeof vi.fn>;
  };
  let navigateByUrlSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    authMock = {
      login: vi.fn().mockReturnValue(
        of({
          token: 'token-123',
          documentType: 'CI',
          documentNumber: '12345678',
          expiresAt: null,
        })
      ),
    };
    catalogsMock = {
      getDocumentTypes: vi.fn().mockReturnValue(
        of([
          { id: 1, label: 'Cédula de identidad', code: 'CI' },
          { id: 2, label: 'Pasaporte', code: 'PASS' },
          { id: 3, label: 'DNI', code: 'DNI' },
        ])
      ),
    };

    TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([]),
        { provide: AuthSessionService, useValue: authMock },
        { provide: Catalogs, useValue: catalogsMock },
        {
          provide: SnackbarHandler,
          useValue: { success: vi.fn(), error: vi.fn(), show: vi.fn() },
        },
      ],
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

  it('should submit document credentials and redirect to home', () => {
    const facade = component['facade'];

    facade.form.setValue({
      documentType: 'CI',
      documentNumber: '11111111',
      password: 'secret',
    });

    facade.submit();

    expect(authMock.login).toHaveBeenCalledWith({
      documentType: 'CI',
      documentNumber: '11111111',
      password: 'secret',
    });
    expect(facade.successMessage()).toBe('Sesión iniciada correctamente.');
    expect(facade.form.controls.password.value).toBe('');
    expect(navigateByUrlSpy).toHaveBeenCalledWith('/inicio');
  });

  it('should not submit when form is invalid', () => {
    component['facade'].submit();

    expect(authMock.login).not.toHaveBeenCalled();
  });
});

