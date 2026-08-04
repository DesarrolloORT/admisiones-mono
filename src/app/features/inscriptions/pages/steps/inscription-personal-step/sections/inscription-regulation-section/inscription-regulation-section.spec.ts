import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { vi } from 'vitest';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';
import { InscripcionRegulationSection } from './inscription-regulation-section';

function createRegulationForm() {
  return new FormGroup({
    aceptaReglamento: new FormControl(false, {
      nonNullable: true,
      validators: Validators.requiredTrue,
    }),
  });
}

type RegulationForm = ReturnType<typeof createRegulationForm>;

describe('InscripcionRegulationSection', () => {
  let fixture: ComponentFixture<InscripcionRegulationSection>;
  let regulationForm: RegulationForm;
  let hasAcceptedStudentRegulation: ReturnType<typeof signal<boolean>>;
  let submittedAcceptanceDate: ReturnType<typeof signal<Date | null>>;
  const openRegulationReaderSpy = vi.fn();

  beforeEach(() => {
    regulationForm = createRegulationForm();
    hasAcceptedStudentRegulation = signal(false);
    submittedAcceptanceDate = signal<Date | null>(null);
    openRegulationReaderSpy.mockClear();

    const facade = {
      regulationForm,
      hasAcceptedStudentRegulation,
      submittedAcceptanceDate,
      openRegulationReader: openRegulationReaderSpy,
    };

    TestBed.configureTestingModule({
      imports: [InscripcionRegulationSection],
      providers: [{ provide: InscripcionSurveyFacade, useValue: facade }],
    });
  });

  function createFixture(): void {
    fixture = TestBed.createComponent(InscripcionRegulationSection);
    fixture.detectChanges();
  }

  it('shows the accepted state with the formatted date and no checkbox', () => {
    hasAcceptedStudentRegulation.set(true);
    submittedAcceptanceDate.set(new Date(2026, 0, 15));
    createFixture();

    expect(fixture.nativeElement.textContent).toContain('15/01/2026');
    expect(fixture.nativeElement.querySelector('ort-checkbox')).toBeFalsy();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.textContent.trim()).toBe('Ver reglamento');
  });

  it('shows the checkbox and button when not accepted yet', () => {
    createFixture();

    expect(fixture.nativeElement.querySelector('ort-checkbox')).toBeTruthy();
    const button = fixture.nativeElement.querySelector('button');
    expect(button.textContent.trim()).toBe('Ver reglamento');
  });

  it('shows the inline error with the new id when touched and invalid', () => {
    createFixture();

    regulationForm.controls.aceptaReglamento.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('#regulation-acceptance-error');
    expect(error).toBeTruthy();
    expect(error.getAttribute('role')).toBe('alert');
    expect(error.textContent).toContain('Aceptá el reglamento estudiantil para continuar');

    const checkboxInput = fixture.nativeElement.querySelector(
      'ort-checkbox[formcontrolname="aceptaReglamento"] input'
    );
    expect(checkboxInput.getAttribute('aria-describedby')).toContain('regulation-acceptance-error');
  });

  it('calls facade.openRegulationReader() when clicking "Ver reglamento"', () => {
    createFixture();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.click();

    expect(openRegulationReaderSpy).toHaveBeenCalledOnce();
  });
});
