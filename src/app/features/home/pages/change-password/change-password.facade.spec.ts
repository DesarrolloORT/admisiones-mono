import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AccountService } from '../../../auth/services/account';
import { ChangePasswordFacade } from './change-password.facade';

describe('ChangePasswordFacade', () => {
  let facade: ChangePasswordFacade;
  let account: { changePassword: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    account = {
      changePassword: vi.fn().mockReturnValue(of(null)),
    };
    router = {
      navigate: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        ChangePasswordFacade,
        { provide: AccountService, useValue: account },
        { provide: Router, useValue: router },
      ],
    });

    facade = TestBed.inject(ChangePasswordFacade);
  });

  it('should submit a valid password change through AccountService', () => {
    facade.form.setValue({
      currentPassword: 'ActualPassword1!',
      password: 'NuevaPassword1!',
      confirmPassword: 'NuevaPassword1!',
    });

    facade.submit();

    expect(account.changePassword).toHaveBeenCalledWith({
      currentPassword: 'ActualPassword1!',
      password: 'NuevaPassword1!',
    });
    expect(router.navigate).toHaveBeenCalledWith(['/inicio']);
  });

  it('should not submit invalid forms', () => {
    facade.form.setValue({
      currentPassword: '',
      password: '',
      confirmPassword: '',
    });

    facade.submit();

    expect(account.changePassword).not.toHaveBeenCalled();
  });

  it('should expose a friendly error when password change fails', () => {
    account.changePassword.mockReturnValue(throwError(() => new Error('invalid password')));
    facade.form.setValue({
      currentPassword: 'ActualPassword1!',
      password: 'NuevaPassword1!',
      confirmPassword: 'NuevaPassword1!',
    });

    facade.submit();

    expect(facade.error()).toBe(
      'No se pudo cambiar la contraseña. Verificá que la actual sea correcta.'
    );
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
