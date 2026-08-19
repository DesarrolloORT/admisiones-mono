import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';

import type { ScholarshipVariant } from '../../../../models/scholarship-personal-forms';
import { ScholarshipSuccessStep } from './scholarship-success-step';

describe('ScholarshipSuccessStep', () => {
  let fixture: ComponentFixture<ScholarshipSuccessStep>;

  function createFixture(variant: ScholarshipVariant): void {
    fixture = TestBed.createComponent(ScholarshipSuccessStep);
    fixture.componentRef.setInput('variant', variant);
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ScholarshipSuccessStep],
      providers: [provideRouter([])],
    });
  });

  it('confirms that the application was sent', () => {
    createFixture('fbr');

    expect(fixture.nativeElement.querySelector('#scholarship-success-title').textContent).toContain(
      '¡Postulación enviada!'
    );
  });

  // fbr y fcl no rinden prueba, asi que no muestran la tarjeta de inscripcion.
  it('shows the enrollment card only for the variants that need it', () => {
    createFixture('fbr');
    expect(fixture.nativeElement.querySelector('ort-card')).toBeFalsy();

    createFixture('fexaCon');
    expect(fixture.nativeElement.querySelector('ort-card')).toBeTruthy();
  });

  it('goes back home when the person leaves the screen', async () => {
    createFixture('fbc');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    fixture.componentInstance['goHome']();

    expect(navigate).toHaveBeenCalledWith(['/inicio']);
  });
});
