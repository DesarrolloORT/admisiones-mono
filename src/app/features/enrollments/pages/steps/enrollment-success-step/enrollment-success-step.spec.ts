import { readFileSync } from 'node:fs';

import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { EnrollmentPaymentFacade } from '../../../facades/enrollment-payment';
import { EnrollmentSuccessStep } from './enrollment-success-step';

describe('EnrollmentSuccessStep', () => {
  const breakpoint = signal({
    isXSmall: true,
    isSmall: false,
    isMedium: false,
    isLarge: false,
    currentBreakpoint: 'xs',
    screenWidth: 375,
  });
  const subjects = ['Materia 1', 'Materia 2', 'Materia 3', 'Materia 4', 'Materia 5'];

  beforeEach(() => {
    breakpoint.set({
      isXSmall: true,
      isSmall: false,
      isMedium: false,
      isLarge: false,
      currentBreakpoint: 'xs',
      screenWidth: 375,
    });
    TestBed.configureTestingModule({
      imports: [EnrollmentSuccessStep],
      providers: [
        { provide: BreakpointService, useValue: { breakpoint } },
        {
          provide: EnrollmentPaymentFacade,
          useValue: {
            subjects: () => subjects,
            visibleSubjects: () => subjects.slice(0, 4),
            canToggleSubjects: () => true,
          },
        },
      ],
    }).overrideComponent(EnrollmentSuccessStep, { set: { imports: [], template: '' } });
  });

  it('creates with its step facade', () => {
    expect(TestBed.createComponent(EnrollmentSuccessStep).componentInstance).toBeTruthy();
  });

  it('limits subjects and exposes the toggle only on small screens', () => {
    const fixture = TestBed.createComponent(EnrollmentSuccessStep);
    const component = fixture.componentInstance as unknown as {
      displayedSubjects: () => readonly string[];
      canToggleSubjects: () => boolean;
    };

    expect(component.displayedSubjects()).toHaveLength(4);
    expect(component.canToggleSubjects()).toBe(true);

    breakpoint.set({
      isXSmall: false,
      isSmall: false,
      isMedium: true,
      isLarge: false,
      currentBreakpoint: 'md',
      screenWidth: 768,
    });

    expect(component.displayedSubjects()).toEqual(subjects);
    expect(component.canToggleSubjects()).toBe(false);
  });

  it('connects the toggle accessibility state with the subject list', () => {
    const template = readFileSync(
      'src/app/features/enrollments/pages/steps/enrollment-success-step/enrollment-success-step.html',
      'utf8'
    );

    expect(template).toContain('id="degree-program-subject-list"');
    expect(template).toContain('aria-controls="degree-program-subject-list"');
    expect(template).toContain('[attr.aria-expanded]="facade.showAllSubjects()"');
  });
});
