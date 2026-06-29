import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { OrtErrorItem } from '@desarrolloort/components';
import { finalize } from 'rxjs/operators';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import type { InscripcionBackendSurvey } from '../models/inscripcion-flow';
import { buildFormErrors } from '../models/inscripcion-flow-forms';
import { Inscripciones } from '../services/inscripciones';
import { InscripcionFormsStore } from '../store/inscripcion-forms';
import { InscripcionProcessStore } from '../store/inscripcion-process';

interface InscripcionErrorAlertState {
  title: string;
  message: string;
}

const INCOMPLETE_INSCRIPTION_ERROR_ALERT: InscripcionErrorAlertState = {
  title: 'Información incompleta',
  message: 'Revisá y completá los campos obligatorios para continuar.',
};

export class InscripcionProposalFacade {
  private readonly inscripciones = inject(Inscripciones);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formsStore = inject(InscripcionFormsStore);
  private readonly process = inject(InscripcionProcessStore);

  public readonly selection = inject(AcademicProposalSelection);
  public readonly academicForm = this.formsStore.academicForm;
  public readonly proposalOptions = this.selection.proposalOptions;
  public readonly careerOptions = this.selection.careerOptions;
  public readonly startOptions = this.selection.startOptions;
  public readonly turnoOptions = this.selection.shiftOptions;
  public readonly catalogError = this.selection.catalogError;
  public readonly initialized = this.selection.initialized;

  private readonly submitted = signal(false);
  private readonly productInterestError = signal<string | null>(null);

  public readonly registeringProductInterest = signal(false);
  public readonly academicErrors = computed<OrtErrorItem[]>(() => {
    if (!this.submitted()) return [];

    const formErrors = buildFormErrors(this.academicForm, [
      { controlName: 'tipoPropuesta', fieldId: '', label: 'Propuesta académica' },
      { controlName: 'carrera', fieldId: '', label: 'Carrera' },
      { controlName: 'comienzo', fieldId: '', label: 'Comienzo' },
      { controlName: 'turno', fieldId: '', label: 'Turno' },
    ]);
    const interestError = this.productInterestError();
    return interestError ? [...formErrors, { message: interestError }] : formErrors;
  });
  public readonly academicErrorAlert = computed<InscripcionErrorAlertState | null>(() => {
    const interestError = this.productInterestError();
    if (interestError) return { title: 'No pudimos continuar', message: interestError };

    return this.academicErrors().length > 0 ? INCOMPLETE_INSCRIPTION_ERROR_ALERT : null;
  });

  constructor() {
    this.selection.connect(this.academicForm);
  }

  public continue(): void {
    if (this.registeringProductInterest()) return;

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
    this.inscripciones
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
          this.process.markCheckpoint();
        },
        error: () =>
          this.productInterestError.set(
            'No se pudo registrar el interés por la propuesta seleccionada.'
          ),
      });
  }

  public careers() {
    return this.selection.careers();
  }

  public setProposalType(value: string): void {
    this.selection.setProposalType(value);
  }

  public loadAcademicOptionsForSurvey(survey: InscripcionBackendSurvey): void {
    this.selection.loadOptions(
      survey.idProducto ?? null,
      survey.idProceso ?? null,
      survey.idTurno ?? null
    );
  }

  private buildProductInterestPayload(): {
    idOferta: number;
    idProcesoSeleccionado: number;
    idProducto: number;
  } | null {
    const idProducto = toNullableNumber(this.academicForm.controls.carrera.value);
    const idProcesoSeleccionado = toNullableNumber(this.academicForm.controls.comienzo.value);
    const idOferta = toNullableNumber(this.academicForm.controls.turno.value);
    return idProducto === null || idProcesoSeleccionado === null || idOferta === null
      ? null
      : { idOferta, idProcesoSeleccionado, idProducto };
  }
}

function toNullableNumber(value: string | number | null | undefined): number | null {
  if (value === null || value === undefined || value === '') return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}
