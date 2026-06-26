import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { InscripcionPaymentFacade } from '../../facades/inscripcion-payment';
import { InscripcionConfirmationStep } from './inscripcion-confirmation-step';

describe('InscripcionConfirmationStep', () => {
  const requestConfirmationSpy = vi.fn();

  beforeEach(() => {
    requestConfirmationSpy.mockClear();

    TestBed.configureTestingModule({
      imports: [InscripcionConfirmationStep],
      providers: [
        {
          provide: InscripcionPaymentFacade,
          useValue: { requestConfirmation: requestConfirmationSpy },
        },
      ],
    }).overrideComponent(InscripcionConfirmationStep, {
      set: {
        imports: [],
        template:
          '<form (keydown.enter)="onFormEnter($event)" (submit)="facade.requestConfirmation()"><input type="radio" name="payment" /><button type="submit">Pagar</button></form>',
      },
    });
  });

  it('creates with its step facade', () => {
    expect(TestBed.createComponent(InscripcionConfirmationStep).componentInstance).toBeTruthy();
  });

  it('does not request confirmation when Enter is pressed from a focused radio', () => {
    const fixture = TestBed.createComponent(InscripcionConfirmationStep);
    fixture.detectChanges();

    const radio = fixture.nativeElement.querySelector('input[type="radio"]') as HTMLInputElement;
    const event = new KeyboardEvent('keydown', {
      key: 'Enter',
      bubbles: true,
      cancelable: true,
    });

    radio.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(requestConfirmationSpy).not.toHaveBeenCalled();
  });
});
