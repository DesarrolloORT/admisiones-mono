import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import type { ScholarshipVariant } from '../../../../models/scholarship-personal-forms';
import { ScholarshipOnboardingStep } from './scholarship-onboarding-step';

describe('ScholarshipOnboardingStep', () => {
  let fixture: ComponentFixture<ScholarshipOnboardingStep>;

  function createFixture(variant: ScholarshipVariant): void {
    fixture = TestBed.createComponent(ScholarshipOnboardingStep);
    fixture.componentRef.setInput('variant', variant);
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ScholarshipOnboardingStep] });
  });

  it('titles the screen with the scholarship fund of the variant', () => {
    createFixture('fbr');

    expect(fixture.componentInstance.title()).toBe('Fondo de becas de reválidas');
    expect(
      fixture.nativeElement.querySelector('#scholarship-onboarding-title').textContent
    ).toContain('Fondo de becas de reválidas');
  });

  // fexaCon y fexaSin son la misma beca partida por el modo de postulacion: el
  // titulo no las distingue.
  it('shows the same title for both fexa variants', () => {
    createFixture('fexaCon');
    const withMode = fixture.componentInstance.title();

    createFixture('fexaSin');

    expect(fixture.componentInstance.title()).toBe(withMode);
    expect(withMode).toBe('Fondo de Excelencia Académica');
  });

  it('emits continueRequested when the person starts the application', () => {
    createFixture('fcl');
    const continueRequested = vi.fn();
    fixture.componentInstance.continueRequested.subscribe(continueRequested);

    (
      fixture.nativeElement.querySelector('button[ort-primary-button]') as HTMLButtonElement
    ).click();

    expect(continueRequested).toHaveBeenCalledOnce();
  });
});
