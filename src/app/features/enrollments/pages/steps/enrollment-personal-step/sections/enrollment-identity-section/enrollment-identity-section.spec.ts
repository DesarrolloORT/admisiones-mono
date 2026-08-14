import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { vi } from 'vitest';

import { EnrollmentSurveyFacade } from '../../../../../facades/enrollment-survey';
import type { IdentityFileTarget } from '../../../../../models/enrollment-flow-forms';
import { EnrollmentIdentitySection } from './enrollment-identity-section';

function createIdentityForm() {
  return new FormGroup({
    documentExpiration: new FormControl<Date | null>(null, Validators.required),
    isIdentityCorrect: new FormControl(false, { nonNullable: true }),
  });
}

type IdentityForm = ReturnType<typeof createIdentityForm>;

describe('EnrollmentIdentitySection', () => {
  let fixture: ComponentFixture<EnrollmentIdentitySection>;
  let identityForm: IdentityForm;
  let missingFiles: Set<IdentityFileTarget>;
  let requiresIdentityConfirmation: ReturnType<typeof signal<boolean>>;
  const updateIdentityFileSpy = vi.fn();

  beforeEach(() => {
    identityForm = createIdentityForm();
    missingFiles = new Set();
    requiresIdentityConfirmation = signal(false);
    updateIdentityFileSpy.mockClear();

    const facade = {
      identityForm,
      identity: {
        acceptedImageTypes: ['image/jpeg', 'image/png'],
        initialIdentityFiles: () => ({ front: [], back: [], selfie: [] }),
        updateIdentityFile: updateIdentityFileSpy,
        requiresIdentityConfirmation,
      },
      isIdentityFileMissing: (target: IdentityFileTarget) => missingFiles.has(target),
    };

    TestBed.configureTestingModule({
      imports: [EnrollmentIdentitySection],
      providers: [{ provide: EnrollmentSurveyFacade, useValue: facade }],
    });
  });

  function createFixture(): void {
    fixture = TestBed.createComponent(EnrollmentIdentitySection);
    fixture.detectChanges();
  }

  it('renders the document and selfie uploaders with their labels', () => {
    createFixture();

    const labels = Array.from(fixture.nativeElement.querySelectorAll('ort-file-uploader')).map(
      element => (element as Element).getAttribute('label')
    );

    expect(labels).toContain('Frente del documento *');
    expect(labels).toContain('Dorso del documento *');
    expect(labels).toContain('Foto del rostro *');
  });

  it('shows the exact missing-file error message per file type', () => {
    missingFiles = new Set(['front', 'back', 'selfie']);
    createFixture();

    const errors = Array.from(
      fixture.nativeElement.querySelectorAll('p.enrollment-field-error[role="alert"]')
    ).map(element => (element as Element).textContent?.trim());

    expect(errors).toContain('Adjuntá la imagen del frente del documento');
    expect(errors).toContain('Adjuntá la imagen del dorso del documento');
    expect(errors).toContain('Adjuntá tu imagen de perfil');
  });

  it('hides the confirmation checkbox when identity confirmation is not required', () => {
    createFixture();

    expect(fixture.nativeElement.querySelector('ort-checkbox')).toBeFalsy();
  });

  it('shows the confirmation checkbox with the new error id when required and invalid', () => {
    requiresIdentityConfirmation.set(true);
    createFixture();

    const checkbox = fixture.nativeElement.querySelector(
      'ort-checkbox[formcontrolname="isIdentityCorrect"]'
    );
    expect(checkbox).toBeTruthy();

    identityForm.controls.isIdentityCorrect.setValidators(Validators.requiredTrue);
    identityForm.controls.isIdentityCorrect.updateValueAndValidity();
    identityForm.controls.isIdentityCorrect.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('#identity-confirmation-error');
    expect(error).toBeTruthy();
    expect(error.getAttribute('role')).toBe('alert');
    expect(error.textContent).toContain('Confirmá que la identidad es correcta');

    const checkboxInput = fixture.nativeElement.querySelector(
      'ort-checkbox[formcontrolname="isIdentityCorrect"] input'
    );
    expect(checkboxInput.getAttribute('aria-describedby')).toContain('identity-confirmation-error');
  });
});
