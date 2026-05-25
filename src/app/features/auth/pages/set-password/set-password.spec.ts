import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { PasswordActivationService } from '../../services/password-activation';
import { SetPassword } from './set-password';

describe('SetPassword', () => {
  let fixture: ComponentFixture<SetPassword>;
  let component: SetPassword;

  function setup(queryParams: Record<string, string | null> = { token: 'token-123' }) {
    TestBed.configureTestingModule({
      imports: [SetPassword],
      providers: [
        {
          provide: PasswordActivationService,
          useValue: {
            activateLink: vi.fn().mockReturnValue(of(undefined)),
            completePassword: vi.fn().mockReturnValue(of(undefined)),
          },
        },
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
});
