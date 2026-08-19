import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { vi } from 'vitest';

import { ScholarshipProcessFacade } from '../../../../facades/scholarship-process';
import type { ScholarshipVariant } from '../../../../models/scholarship-personal-forms';
import { ScholarshipConfirmationStep } from './scholarship-confirmation-step';

describe('ScholarshipConfirmationStep', () => {
  let fixture: ComponentFixture<ScholarshipConfirmationStep>;
  let component: ScholarshipConfirmationStep;
  let breakpoint: ReturnType<typeof signal<{ isXSmall: boolean; isSmall: boolean }>>;

  function createFixture(variant: ScholarshipVariant = 'fbr'): void {
    fixture = TestBed.createComponent(ScholarshipConfirmationStep);
    fixture.componentRef.setInput('variant', variant);
    fixture.detectChanges();
    component = fixture.componentInstance;
  }

  beforeEach(() => {
    breakpoint = signal({ isXSmall: false, isSmall: false });

    TestBed.configureTestingModule({
      imports: [ScholarshipConfirmationStep],
      providers: [
        ScholarshipProcessFacade,
        { provide: BreakpointService, useValue: { breakpoint } },
      ],
    });
  });

  it('blocks confirming until the terms are accepted', () => {
    createFixture();
    const confirmApplication = vi.fn();
    component.confirmApplication.subscribe(confirmApplication);

    component['onConfirmApplication']();

    expect(component.termsError()).toBe(true);
    expect(confirmApplication).not.toHaveBeenCalled();
  });

  it('confirms once the terms are accepted', () => {
    createFixture();
    const confirmApplication = vi.fn();
    component.confirmApplication.subscribe(confirmApplication);

    component.onTermsAcceptedChange(true);
    component['onConfirmApplication']();

    expect(component.termsError()).toBe(false);
    expect(confirmApplication).toHaveBeenCalledOnce();
  });

  // El checkbox de ORT emite el valor como string.
  it('reads the checkbox value both as boolean and as string', () => {
    createFixture();

    component.onTermsAcceptedChange('true');
    expect(component.termsAccepted()).toBe(true);

    component.onTermsAcceptedChange('false');
    expect(component.termsAccepted()).toBe(false);
  });

  it('clears the terms error as soon as the person accepts', () => {
    createFixture();
    component['onConfirmApplication']();

    component.onTermsAcceptedChange(true);

    expect(component.termsError()).toBe(false);
  });

  it('accepts the terms from the full-screen reading and closes it', () => {
    createFixture();
    component.openTermsAndConditions();

    expect(component.showTermsAndConditions()).toBe(true);

    component.acceptTermsAndConditions();

    expect(component.showTermsAndConditions()).toBe(false);
    expect(component.termsAccepted()).toBe(true);
  });

  it('resolves the layout from the variant', () => {
    createFixture('fexaCon');
    expect(component.isSidebarLayout()).toBe(true);
    expect(component.isStackedLayout()).toBe(false);

    createFixture('fcl');
    expect(component.isSidebarLayout()).toBe(false);
    expect(component.isStackedLayout()).toBe(true);
  });

  it('uses outlined cards on small screens', () => {
    createFixture();
    expect(component.cardVariant()).toBe('elevated');

    breakpoint.set({ isXSmall: true, isSmall: false });

    expect(component.cardVariant()).toBe('outlined');
  });

  it('delegates going back to the process facade', () => {
    createFixture();
    const back = vi.spyOn(TestBed.inject(ScholarshipProcessFacade), 'back');

    component['onBack']();

    expect(back).toHaveBeenCalledOnce();
  });
});
