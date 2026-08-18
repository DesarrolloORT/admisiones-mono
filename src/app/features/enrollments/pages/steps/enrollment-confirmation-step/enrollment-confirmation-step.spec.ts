import { readFileSync } from 'node:fs';

import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { vi } from 'vitest';

import { EnrollmentPaymentFacade } from '../../../facades/enrollment-payment';
import { EnrollmentConfirmationStep } from './enrollment-confirmation-step';

describe('EnrollmentConfirmationStep', () => {
  const confirmSpy = vi.fn();
  const breakpoint = signal({
    isXSmall: true,
    isSmall: false,
    isMedium: false,
    isLarge: false,
    currentBreakpoint: 'xs',
    screenWidth: 375,
  });

  beforeEach(() => {
    confirmSpy.mockClear();
    breakpoint.set({
      isXSmall: true,
      isSmall: false,
      isMedium: false,
      isLarge: false,
      currentBreakpoint: 'xs',
      screenWidth: 375,
    });

    TestBed.configureTestingModule({
      imports: [EnrollmentConfirmationStep],
      providers: [
        {
          provide: BreakpointService,
          useValue: { breakpoint },
        },
        {
          provide: EnrollmentPaymentFacade,
          useValue: { confirm: confirmSpy },
        },
      ],
    }).overrideComponent(EnrollmentConfirmationStep, {
      set: {
        imports: [],
        template:
          '<form (keydown.enter)="onFormEnter($event)" (submit)="facade.confirm()"><input type="radio" name="payment" /><button type="submit">Pagar</button></form>',
      },
    });
  });

  it('creates with its step facade', () => {
    expect(TestBed.createComponent(EnrollmentConfirmationStep).componentInstance).toBeTruthy();
  });

  it('does not submit the payment when Enter is pressed from a focused radio', () => {
    const fixture = TestBed.createComponent(EnrollmentConfirmationStep);
    fixture.detectChanges();

    const radio = fixture.nativeElement.querySelector('input[type="radio"]') as HTMLInputElement;
    const event = new KeyboardEvent('keydown', {
      key: 'Enter',
      bubbles: true,
      cancelable: true,
    });

    radio.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(confirmSpy).not.toHaveBeenCalled();
  });

  it('uses vertical radio groups on small breakpoints', () => {
    const fixture = TestBed.createComponent(EnrollmentConfirmationStep);
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
    const fixture = TestBed.createComponent(EnrollmentConfirmationStep);
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
      'src/app/features/enrollments/pages/steps/enrollment-confirmation-step/enrollment-confirmation-step.html',
      'utf8'
    );

    expect(template).toContain('<app-responsive-select');
    expect(template).toContain('[loading]="facade.loadingBanks()"');
    expect(template).toContain('[options]="facade.bankOptions()"');
    expect(template).toContain('formControlName="bank"');
  });
});
