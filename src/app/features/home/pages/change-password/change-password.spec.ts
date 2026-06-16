import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { AccountService } from '../../../auth/services/account';
import { ChangePassword } from './change-password';

describe('ChangePassword', () => {
  let fixture: ComponentFixture<ChangePassword>;
  let component: ChangePassword;
  let account: { changePassword: ReturnType<typeof vi.fn> };
  let navigateSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    account = {
      changePassword: vi.fn().mockReturnValue(of(null)),
    };

    TestBed.configureTestingModule({
      imports: [ChangePassword],
      providers: [provideRouter([]), { provide: AccountService, useValue: account }],
    });

    navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(ChangePassword);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should submit a valid password change through AccountService', () => {
    component['form'].setValue({
      currentPassword: 'ActualPassword1!',
      password: 'NuevaPassword1!',
      confirmPassword: 'NuevaPassword1!',
    });

    component['submit']();

    expect(account.changePassword).toHaveBeenCalledWith({
      currentPassword: 'ActualPassword1!',
      password: 'NuevaPassword1!',
    });
    expect(navigateSpy).toHaveBeenCalledWith(['/inicio']);
  });

  it('should not submit invalid forms', () => {
    component['form'].setValue({
      currentPassword: '',
      password: '',
      confirmPassword: '',
    });

    component['submit']();

    expect(account.changePassword).not.toHaveBeenCalled();
  });

  it('should expose a friendly error when password change fails', () => {
    account.changePassword.mockReturnValue(throwError(() => new Error('invalid password')));
    component['form'].setValue({
      currentPassword: 'ActualPassword1!',
      password: 'NuevaPassword1!',
      confirmPassword: 'NuevaPassword1!',
    });

    component['submit']();

    expect(component['error']()).toBe(
      'No se pudo cambiar la contraseña. Verificá que la actual sea correcta.'
    );
    expect(navigateSpy).not.toHaveBeenCalled();
  });
});
