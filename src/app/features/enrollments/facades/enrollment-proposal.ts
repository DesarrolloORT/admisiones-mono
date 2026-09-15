import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { OrtErrorItem } from '@desarrolloort/components';
import { finalize } from 'rxjs/operators';
import { getApiErrorMessage } from 'src/app/shared/errors/api-error-message';
import {
  DEFAULT_ERROR_ALERT,
  type ErrorAlertState,
} from 'src/app/shared/ui/error-alert/error-alert';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { EnrollmentsApi } from '../api/enrollments.api';
import type { EnrollmentInitialSurvey } from '../models/enrollment-flow';
import { buildFormErrors, ENROLLMENT_FORMS } from '../models/enrollment-flow-forms';
import { ENROLLMENT_PROCESS_STATE } from '../models/enrollment-process';

export class EnrollmentProposalFacade {
  private readonly enrollments = inject(EnrollmentsApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formsStore = inject(ENROLLMENT_FORMS);
  private readonly process = inject(ENROLLMENT_PROCESS_STATE);

  public readonly selection = inject(AcademicProposalSelection);
  public readonly academicForm = this.formsStore.forms.academicForm;
  public readonly proposalOptions = this.selection.proposalOptions;
  public readonly degreeProgramOptions = this.selection.degreeProgramOptions;
  public readonly intakeOptions = this.selection.intakeOptions;
  public readonly shiftOptions = this.selection.shiftOptions;
  public readonly catalogError = this.selection.catalogError;
  public readonly initialized = this.selection.initialized;

  private readonly submitted = signal(false);
  private readonly productInterestError = signal<string | null>(null);
  private resumeInProgress = false;

  public readonly registeringProductInterest = signal(false);
  public readonly academicErrors = computed<OrtErrorItem[]>(() => {
    if (!this.submitted()) return [];

    const terminology = this.selection.terminology();
    const formErrors = buildFormErrors(this.academicForm, [
      { controlName: 'proposalType', fieldId: '', label: 'Propuesta académica' },
      { controlName: 'degreeProgram', fieldId: '', label: terminology.degreeProgramLabel },
      { controlName: 'intake', fieldId: '', label: terminology.intakeLabel },
      { controlName: 'shift', fieldId: '', label: 'Turno' },
      { controlName: 'seminars', fieldId: '', label: this.selection.seminarLabel() },
    ]);
    const interestError = this.productInterestError();
    return interestError ? [...formErrors, { message: interestError }] : formErrors;
  });
  public readonly academicErrorAlert = computed<ErrorAlertState | null>(() => {
    const interestError = this.productInterestError();
    if (interestError) return { title: 'No pudimos continuar', message: interestError };

    return this.academicErrors().length > 0 ? DEFAULT_ERROR_ALERT : null;
  });

  constructor() {
    this.selection.connect(this.academicForm);
  }

  public continue(): void {
    if (this.registeringProductInterest()) return;

    if (this.resumeInProgress) {
      this.process.flow.next();
      return;
    }

    this.submitted.set(true);
    this.productInterestError.set(null);
    if (this.academicForm.invalid) {
      this.academicForm.markAllAsTouched();
      return;
    }

    const payload = this.buildProductInterestPayload();
    if (!payload) {
      this.productInterestError.set('Seleccioná una propuesta válida para continuar.');
      return;
    }

    this.registeringProductInterest.set(true);
    this.enrollments
      .registerProductInterest(payload)
      .pipe(
        finalize(() => this.registeringProductInterest.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => this.process.flow.next(),
        error: (error: unknown) =>
          this.productInterestError.set(
            getApiErrorMessage(
              error,
              'No se pudo registrar el interés por la propuesta seleccionada.'
            )
          ),
      });
  }

  public degreePrograms() {
    return this.selection.degreePrograms();
  }

  public setProposalType(value: string): void {
    this.selection.setProposalType(value);
  }

  public loadAcademicOptionsForSurvey(survey: EnrollmentInitialSurvey): void {
    this.selection.loadOptions(survey.degreeProgramId, survey.intakeId, survey.shiftId);
  }

  public disableForResume(): void {
    this.resumeInProgress = true;
    this.academicForm.disable({ emitEvent: false });
  }

  private buildProductInterestPayload(): {
    offeringIds: number[];
    selectedAdmissionProcessId: number;
    productId: number;
  } | null {
    const productId = toNullableNumber(this.academicForm.controls.degreeProgram.value);
    if (productId === null) return null;

    if (this.selection.isProfessionalUpdate()) {
      const seminars = this.selection.seminars();
      const selected = this.academicForm.controls.seminars.value
        .map(value => seminars.find(seminar => seminar.offeringId.toString() === value))
        .filter(seminar => seminar !== undefined);
      if (selected.length === 0) return null;

      return {
        offeringIds: selected.map(seminar => seminar!.offeringId),
        selectedAdmissionProcessId: selected[0]!.admissionProcessId,
        productId,
      };
    }

    const selectedAdmissionProcessId = toNullableNumber(this.academicForm.controls.intake.value);
    const offeringId = toNullableNumber(this.academicForm.controls.shift.value);
    return selectedAdmissionProcessId === null || offeringId === null
      ? null
      : { offeringIds: [offeringId], selectedAdmissionProcessId, productId };
  }
}

function toNullableNumber(value: string | number | null | undefined): number | null {
  if (value === null || value === undefined || value === '') return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}
