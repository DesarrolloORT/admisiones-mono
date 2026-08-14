import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { OrtErrorItem } from '@desarrolloort/components';
import { finalize } from 'rxjs/operators';
import {
  DEFAULT_ERROR_ALERT,
  type ErrorAlertState,
} from 'src/app/shared/ui/error-alert/error-alert';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import type { InscripcionInitialSurvey } from '../models/inscription-flow';
import { buildFormErrors } from '../models/inscription-flow-forms';
import { Inscripciones } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';

export class InscripcionProposalFacade {
  private readonly inscriptions = inject(Inscripciones);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formsStore = inject(InscripcionFormsStore);
  private readonly process = inject(InscripcionProcessStore);

  public readonly selection = inject(AcademicProposalSelection);
  public readonly academicForm = this.formsStore.academicForm;
  public readonly proposalOptions = this.selection.proposalOptions;
  public readonly careerOptions = this.selection.degreeProgramOptions;
  public readonly startOptions = this.selection.intakeOptions;
  public readonly turnoOptions = this.selection.shiftOptions;
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
      { controlName: 'intake', fieldId: '', label: terminology.startLabel },
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
    this.inscriptions
      .registerProductInterest(payload)
      .pipe(
        finalize(() => this.registeringProductInterest.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: registered => {
          if (!registered) {
            this.productInterestError.set(
              'No se pudo registrar el interés por la propuesta seleccionada.'
            );
            return;
          }
          this.process.flow.next();
        },
        error: () =>
          this.productInterestError.set(
            'No se pudo registrar el interés por la propuesta seleccionada.'
          ),
      });
  }

  public careers() {
    return this.selection.degreePrograms();
  }

  public setProposalType(value: string): void {
    this.selection.setProposalType(value);
  }

  public loadAcademicOptionsForSurvey(survey: InscripcionInitialSurvey): void {
    this.selection.loadOptions(survey.carreraId, survey.comienzoId, survey.turnoId);
  }

  public disableForResume(): void {
    this.resumeInProgress = true;
    this.academicForm.disable({ emitEvent: false });
  }

  private buildProductInterestPayload(): {
    idOfertas: number[];
    idProcesoSeleccionado: number;
    idProducto: number;
  } | null {
    const idProducto = toNullableNumber(this.academicForm.controls.degreeProgram.value);
    if (idProducto === null) return null;

    if (this.selection.isProfessionalUpdate()) {
      const seminars = this.selection.seminars();
      const selected = this.academicForm.controls.seminars.value
        .map(value => seminars.find(seminar => seminar.offeringId.toString() === value))
        .filter(seminar => seminar !== undefined);
      if (selected.length === 0) return null;

      return {
        idOfertas: selected.map(seminar => seminar!.offeringId),
        idProcesoSeleccionado: selected[0]!.admissionProcessId,
        idProducto,
      };
    }

    const idProcesoSeleccionado = toNullableNumber(this.academicForm.controls.intake.value);
    const idOferta = toNullableNumber(this.academicForm.controls.shift.value);
    return idProcesoSeleccionado === null || idOferta === null
      ? null
      : { idOfertas: [idOferta], idProcesoSeleccionado, idProducto };
  }
}

function toNullableNumber(value: string | number | null | undefined): number | null {
  if (value === null || value === undefined || value === '') return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}
