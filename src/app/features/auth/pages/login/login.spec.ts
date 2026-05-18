import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../../catalogs/services/catalogs';
import { Auth } from '../../services/auth';
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
        { provide: Auth, useValue: authMock },
        { provide: Catalogs, useValue: catalogsMock },
      ],
    });

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

  it('should submit document credentials', () => {
    component['form'].setValue({
      documentType: 'CI',
      documentNumber: '12345678',
      password: 'secret',
    });

    component['onSubmit']();

    expect(authMock.login).toHaveBeenCalledWith({
      documentType: 'CI',
      documentNumber: '12345678',
      password: 'secret',
    });
    expect(component['successMessage']()).toBe('Sesión iniciada correctamente.');
  });

  it('should not submit when form is invalid', () => {
    component['onSubmit']();

    expect(authMock.login).not.toHaveBeenCalled();
  });
});
