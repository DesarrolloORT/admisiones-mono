import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../../../shared/ui/snackbar/snackbar-handler';
import { AuthSessionService } from '../../services/auth-session';
import { TwoFactorValidationPage } from './two-factor-validation';

describe('TwoFactorValidationPage', () => {
  let fixture: ComponentFixture<TwoFactorValidationPage>;

  beforeEach(() => {
    history.replaceState({ email: 'a@b.com', sessionId: 'abc-123' }, '');

    TestBed.configureTestingModule({
      imports: [TwoFactorValidationPage],
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: { completeTwoFactor: vi.fn().mockReturnValue(of(undefined)) },
        },
        { provide: SnackbarHandler, useValue: { success: vi.fn() } },
      ],
    });

    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(TwoFactorValidationPage);
    fixture.detectChanges();
  });

  it('renders the two-factor validation component inside the auth shell', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('app-two-factor-validation')).toBeTruthy();
    expect(element.textContent).toContain('Autenticación requerida');
  });
});

