import { readFileSync } from 'node:fs';

import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { vi } from 'vitest';

import { InscripcionPaymentFacade } from '../../facades/inscription-payment';
import { InscripcionConfirmationStep } from './inscription-confirmation-step';

describe('InscripcionConfirmationStep', () => {
  const requestConfirmationSpy = vi.fn();
  const breakpoint = signal({
    isXSmall: true,
    isSmall: false,
    isMedium: false,
    isLarge: false,
    currentBreakpoint: 'xs',
    screenWidth: 375,
  });

  beforeEach(() => {
    requestConfirmationSpy.mockClear();
    breakpoint.set({
      isXSmall: true,
      isSmall: false,
      isMedium: false,
      isLarge: false,
      currentBreakpoint: 'xs',
      screenWidth: 375,
    });

    TestBed.configureTestingModule({
      imports: [InscripcionConfirmationStep],
      providers: [
        {
          provide: BreakpointService,
          useValue: { breakpoint },
        },
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

  it('uses vertical radio groups on small breakpoints', () => {
    const fixture = TestBed.createComponent(InscripcionConfirmationStep);
    const component = fixture.componentInstance as unknown as {
      radioGroupOrientation: () => 'vertical' | 'horizontal';
    };

    expect(component.radioGroupOrientation()).toBe('vertical');
  });

  it('uses horizontal radio groups from medium and large breakpoints', () => {
    breakpoint.set({
      isXSmall: false,
      isSmall: false,
      isMedium: true,
      isLarge: false,
      currentBreakpoint: 'md',
      screenWidth: 768,
    });
    const fixture = TestBed.createComponent(InscripcionConfirmationStep);
    const component = fixture.componentInstance as unknown as {
      radioGroupOrientation: () => 'vertical' | 'horizontal';
    };

    expect(component.radioGroupOrientation()).toBe('horizontal');

    breakpoint.set({
      isXSmall: false,
      isSmall: false,
      isMedium: false,
      isLarge: true,
      currentBreakpoint: 'lg',
      screenWidth: 1280,
    });

    expect(component.radioGroupOrientation()).toBe('horizontal');
  });

  it('keeps bank selection usable with logo options in the real template', () => {
    const template = readFileSync(
      'src/app/features/inscriptions/components/inscription-confirmation-step/inscription-confirmation-step.html',
      'utf8'
    );

    expect(template).toContain('<app-responsive-select');
    expect(template).toContain('[loading]="facade.loadingBanks()"');
    expect(template).toContain('[options]="facade.bankOptions()"');
    expect(template).toContain('formControlName="banco"');
  });
});
