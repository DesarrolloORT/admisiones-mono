import { readFileSync } from 'node:fs';

import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { vi } from 'vitest';

import { EnrollmentSurveyFacade } from '../../../facades/enrollment-survey';
import { EnrollmentPersonalStep } from './enrollment-personal-step';

describe('EnrollmentPersonalStep', () => {
  let fixture: ComponentFixture<EnrollmentPersonalStep>;
  const continueSpy = vi.fn();
  const breakpoint = signal({
    isXSmall: true,
    isSmall: false,
    isMedium: false,
    isLarge: false,
    currentBreakpoint: 'xs',
    screenWidth: 375,
  });

  beforeEach(() => {
    continueSpy.mockClear();
    breakpoint.set({
      isXSmall: true,
      isSmall: false,
      isMedium: false,
      isLarge: false,
      currentBreakpoint: 'xs',
      screenWidth: 375,
    });

    TestBed.configureTestingModule({
      imports: [EnrollmentPersonalStep],
      providers: [
        {
          provide: BreakpointService,
          useValue: { breakpoint },
        },
        {
          provide: EnrollmentSurveyFacade,
          useValue: { continue: continueSpy },
        },
      ],
    }).overrideComponent(EnrollmentPersonalStep, {
      set: {
        imports: [],
        template: `
          <form (keydown.enter)="onFormEnter($event)" (submit)="onSubmit($event)">
            <input type="radio" name="personal" />
            <button type="submit">Continuar</button>
          </form>
        `,
      },
    });

    fixture = TestBed.createComponent(EnrollmentPersonalStep);
    fixture.detectChanges();
  });

  it('prevents native form submission before continuing the flow', () => {
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    const event = new SubmitEvent('submit', { bubbles: true, cancelable: true });

    form.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(continueSpy).toHaveBeenCalledOnce();
  });

  it('does not continue when Enter is pressed from a focused radio', () => {
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
  it('uses vertical radio groups on small breakpoints', () => {
    const component = fixture.componentInstance as unknown as {
      radioGroupOrientation: () => 'vertical' | 'horizontal';
    };

    expect(component.radioGroupOrientation()).toBe('vertical');
  });

  it('uses horizontal radio groups from the desktop breakpoint', () => {
    breakpoint.set({
      isXSmall: false,
      isSmall: false,
      isMedium: true,
      isLarge: false,
      currentBreakpoint: 'md',
      screenWidth: 768,
    });
    const component = fixture.componentInstance as unknown as {
      radioGroupOrientation: () => 'vertical' | 'horizontal';
    };

    expect(component.radioGroupOrientation()).toBe('vertical');

    breakpoint.set({
      isXSmall: false,
      isSmall: false,
      isMedium: true,
      isLarge: false,
      currentBreakpoint: 'md',
      screenWidth: 840,
    });

    expect(component.radioGroupOrientation()).toBe('horizontal');
  });
  it('keeps generated survey fields bound in the section templates', () => {
    const education = readFileSync(
      'src/app/features/enrollments/pages/steps/enrollment-personal-step/sections/enrollment-education-section/enrollment-education-section.html',
      'utf8'
    );
    const decision = readFileSync(
      'src/app/features/enrollments/pages/steps/enrollment-personal-step/sections/enrollment-academic-decision-section/enrollment-academic-decision-section.html',
      'utf8'
    );

    expect(education).toContain('<app-responsive-select');
    expect(education).toContain('[options]="facade.options.orientationOptions()"');
    expect(education).toContain('formControlName="orientation"');
    expect(education).toContain('formControlName="repeatsHighSchoolYear"');
    expect(education).toContain('formControlName="highSchoolYearRepeatCount"');
    expect(education).toContain('ortNumberInput');
    expect(education).toContain('formControlName="otherHigherEducationUniversity"');
    expect(education).toContain('ortInput');
    expect(decision).toContain('formControlName="otherResearchedUniversity"');
    expect(decision).toContain('formControlName="decisionSupport"');
    expect(decision).toContain('<app-responsive-select');
  });

  it('renders the survey catalog load error', () => {
    const template = readFileSync(
      'src/app/features/enrollments/pages/steps/enrollment-personal-step/enrollment-personal-step.html',
      'utf8'
    );

    expect(template).toContain('facade.catalogError()');
  });
});
