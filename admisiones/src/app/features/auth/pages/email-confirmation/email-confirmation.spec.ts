import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';

import {
  REGISTER_EMAIL_CONFIRMATION,
  TWO_FACTOR_EMAIL_CONFIRMATION,
} from '../../models/email-confirmation';
import { EmailConfirmation } from './email-confirmation';

describe('EmailConfirmation', () => {
  let fixture: ComponentFixture<EmailConfirmation>;
  let navigateByUrlSpy: ReturnType<typeof vi.spyOn>;

  afterEach(() => {
    vi.restoreAllMocks();
    history.replaceState({}, '');
  });

  it('renders configured confirmation content and navigates to its action route', () => {
    setup(REGISTER_EMAIL_CONFIRMATION);

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('¡Cuenta creada con éxito!');
    expect(element.textContent).toContain('Volver al inicio de sesión');

    clickPrimaryButton(element);

    expect(navigateByUrlSpy).toHaveBeenCalledWith('/iniciar-sesion');
  });

  it('continues a two-factor flow without router state', () => {
    setup(TWO_FACTOR_EMAIL_CONFIRMATION);

    clickPrimaryButton(fixture.nativeElement as HTMLElement);

    expect(navigateByUrlSpy).toHaveBeenCalledWith('/verificar-codigo');
  });

  function setup(confirmation: unknown): void {
    TestBed.configureTestingModule({
      imports: [EmailConfirmation],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              data: { confirmation },
            },
          },
        },
      ],
    });

    navigateByUrlSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(EmailConfirmation);
    fixture.detectChanges();
  }

  function clickPrimaryButton(element: HTMLElement): void {
    const button = element.querySelector('button') as HTMLButtonElement;
    button.click();
  }
});
