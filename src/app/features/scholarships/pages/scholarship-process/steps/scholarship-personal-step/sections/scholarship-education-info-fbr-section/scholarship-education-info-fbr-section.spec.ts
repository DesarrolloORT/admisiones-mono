import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { OrtFileUploaderChange } from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipPersonalFacade } from '../../../../../../facades/scholarship-personal';
import { ScholarshipProcessFacade } from '../../../../../../facades/scholarship-process';
import { ScholarshipEducationInfoFbrSection } from './scholarship-education-info-fbr-section';

function revalidationUpload(isValid: boolean): OrtFileUploaderChange {
  return {
    value: [{ file: new File(['x'], 'revalidas.pdf', { type: 'application/pdf' }), isValid }],
  } as unknown as OrtFileUploaderChange;
}

describe('ScholarshipEducationInfoFbrSection', () => {
  let fixture: ComponentFixture<ScholarshipEducationInfoFbrSection>;
  let facade: ScholarshipPersonalFacade;
  let breakpoint: ReturnType<typeof signal<{ isXSmall: boolean; isSmall: boolean }>>;

  beforeEach(() => {
    breakpoint = signal({ isXSmall: false, isSmall: false });

    TestBed.configureTestingModule({
      imports: [ScholarshipEducationInfoFbrSection],
      providers: [
        ScholarshipProcessFacade,
        ScholarshipPersonalFacade,
        { provide: BreakpointService, useValue: { breakpoint } },
      ],
    });

    facade = TestBed.inject(ScholarshipPersonalFacade);
    facade.setVariant('fbr');
    fixture = TestBed.createComponent(ScholarshipEducationInfoFbrSection);
    fixture.detectChanges();
  });

  it('asks where the person studied, before and at university', () => {
    const legends = Array.from(fixture.nativeElement.querySelectorAll('ort-radio-group')).map(
      group => (group as Element).getAttribute('legend')
    );

    expect(legends).toEqual([
      '¿Dónde cursaste el último año de secundaria?',
      'Indica el último año cursado',
      '¿Dónde cursaste los estudios universitarios a revalidar?',
    ]);
  });

  it('asks for the revalidation figures', () => {
    const labels = Array.from(fixture.nativeElement.querySelectorAll('ort-label')).map(label =>
      (label as Element).textContent?.trim()
    );

    expect(labels).toEqual([
      'Carrera cursada o en curso',
      'Cantidad de materias aprobadas',
      'Cantidad de materias',
      'Promedio de calificaciones',
      'Promedio total de calificaciones',
    ]);
  });

  it('shows an error for every empty field once the person tried to continue', () => {
    facade.submitted.set(true);
    fixture.detectChanges();

    const errors = Array.from(fixture.nativeElement.querySelectorAll('ort-error')).map(error =>
      (error as Element).textContent?.trim()
    );

    // Los nueve controles del form son obligatorios para reválidas.
    expect(errors).toHaveLength(9);
    expect(errors).toContain('Adjuntá el formulario de reválidas');
  });

  it('clears a field error as soon as it is answered', () => {
    facade.submitted.set(true);
    fixture.detectChanges();

    facade.educationInfoFbrForm.controls.career.setValue('Ingeniería');
    fixture.detectChanges();

    const errors = Array.from(fixture.nativeElement.querySelectorAll('ort-error')).map(error =>
      (error as Element).textContent?.trim()
    );

    expect(errors).not.toContain('Ingresá la carrera cursada o en curso');
  });

  it('marks the revalidation form as attached only for a valid file', () => {
    const control = facade.educationInfoFbrForm.controls.revalidationFormFile;

    fixture.componentInstance.onRevalidationFormFilesChanged(revalidationUpload(true));
    expect(control.value).toBe(true);

    fixture.componentInstance.onRevalidationFormFilesChanged(revalidationUpload(false));
    expect(control.value).toBe(false);
  });

  it('stacks the radio groups on small screens', () => {
    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('horizontal');

    breakpoint.set({ isXSmall: true, isSmall: false });

    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('vertical');
    expect(fixture.componentInstance['radioGroupIndicatorPosition']()).toBe('right');
  });
});
