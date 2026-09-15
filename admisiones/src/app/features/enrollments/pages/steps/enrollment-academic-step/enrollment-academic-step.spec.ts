import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { EnrollmentProposalFacade } from '../../../facades/enrollment-proposal';
import { EnrollmentAcademicStep } from './enrollment-academic-step';

describe('EnrollmentAcademicStep', () => {
  const continueSpy = vi.fn();

  beforeEach(() => {
    continueSpy.mockClear();

    TestBed.configureTestingModule({
      imports: [EnrollmentAcademicStep],
      providers: [{ provide: EnrollmentProposalFacade, useValue: { continue: continueSpy } }],
    }).overrideComponent(EnrollmentAcademicStep, {
      set: {
        imports: [],
        template:
          '<form (keydown.enter)="onFormEnter($event)" (submit)="facade.continue()"><input type="radio" name="academic" /><button type="submit">Continuar</button></form>',
      },
    });
  });

  it('creates with its step facade', () => {
    expect(TestBed.createComponent(EnrollmentAcademicStep).componentInstance).toBeTruthy();
  });

  it('does not continue when Enter is pressed from a focused radio', () => {
    const fixture = TestBed.createComponent(EnrollmentAcademicStep);
    fixture.detectChanges();

    const radio = fixture.nativeElement.querySelector('input[type="radio"]') as HTMLInputElement;
    const event = new KeyboardEvent('keydown', {
      key: 'Enter',
      bubbles: true,
      cancelable: true,
    });

    radio.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(continueSpy).not.toHaveBeenCalled();
  });
});
