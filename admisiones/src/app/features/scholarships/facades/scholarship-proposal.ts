import { computed, effect, inject, signal } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';

import type { ScholarshipVariant } from '../models/scholarship-personal-forms';
import { createSectionStatus, SectionStatus } from '../models/section-status';
import {
  ScholarshipAcademicData,
  ScholarshipAcademicStepData,
} from '../services/scholarship-academic-data';
import { ScholarshipProcessFacade } from './scholarship-process';

/**
 * Fachada del paso "Tu inscripción" (propuesta académica). Equivale a
 * `ScholarshipPersonalFacade`: centraliza el `FormGroup` (vía
 * `ScholarshipProcessFacade`), la carga de datos (vía `ScholarshipAcademicData`),
 * los validadores condicionales según `variant` y el estado de completitud de
 * cada sección del acordeón.
 */
export class ScholarshipProposalFacade {
  private readonly process = inject(ScholarshipProcessFacade);
  private readonly academicData = inject(ScholarshipAcademicData);

  public readonly inscriptionForm = this.process.applicationForm;
  public readonly inscriptionSection = this.inscriptionForm.controls.inscription;
  public readonly evaluationSection = this.inscriptionForm.controls.evaluation;

  public readonly selectionControl = this.inscriptionSection.controls.selectedInscription;
  public readonly applicationModeControl = this.inscriptionSection.controls.applicationMode;
  public readonly evaluationDateControl = this.evaluationSection.controls.evaluationDate;

  public readonly variant = signal<ScholarshipVariant | null>(null);
  public readonly submitted = signal(false);
  public readonly academicStepData = signal<ScholarshipAcademicStepData[]>([]);
  public readonly selectedInscription = signal<string | null>(null);

  public readonly selectedAcademicStepData = computed(() => {
    const items = this.academicStepData();
    const selectedValue = this.selectedInscription();
    const selectedItem = items.find(item => item.carrera === selectedValue);

    if (selectedItem) {
      return selectedItem;
    }

    return items.length === 1
      ? (items[0] ?? this.emptyAcademicStepData())
      : this.emptyAcademicStepData();
  });

  public readonly inscriptionStatus: SectionStatus = createSectionStatus(
    () => this.inscriptionSection.valid,
    this.submitted
  );

  public readonly evaluationStatus: SectionStatus = createSectionStatus(
    () => this.evaluationSection.valid,
    this.submitted
  );

  constructor() {
    effect(() => {
      this.syncConditionalValidators();
    });

    this.academicData.getAcademicStepData().subscribe(data => {
      this.academicStepData.set(data);

      if (data.length === 1) {
        const selectedValue = data[0]?.carrera ?? null;
        this.selectedInscription.set(selectedValue);
        this.selectionControl.setValue(selectedValue);
        return;
      }

      this.selectedInscription.set(null);
      this.selectionControl.reset(null);
    });
  }

  public setVariant(variant: ScholarshipVariant): void {
    this.variant.set(variant);
  }

  public shouldShowApplicationMode(): boolean {
    return this.variant() === 'fexaCon' || this.variant() === 'fexaSin';
  }

  public shouldShowEvaluationSection(): boolean {
    return this.variant() === 'fexaCon' || this.variant() === 'fexaSin' || this.variant() === 'fbc';
  }

  public onInscriptionSelectionChange(value: string | null): void {
    this.selectedInscription.set(value);
    this.selectionControl.setValue(value);
    this.selectionControl.markAsTouched();
  }

  public continue(): void {
    this.submitted.set(true);
    this.inscriptionForm.markAllAsTouched();

    if (!this.inscriptionForm.valid) return;

    this.process.continue();
  }

  public showErrorAlert(): boolean {
    return this.submitted() && !this.inscriptionForm.valid;
  }

  private emptyAcademicStepData(): ScholarshipAcademicStepData {
    return { carrera: '', comienzo: '', turno: '' };
  }

  private syncConditionalValidators(): void {
    this.setRequired(this.applicationModeControl, this.shouldShowApplicationMode());
    this.setRequired(this.evaluationDateControl, this.shouldShowEvaluationSection());
    this.setRequired(this.selectionControl, this.academicStepData().length > 1);
  }

  private setRequired(control: FormControl<string | null>, required: boolean): void {
    control.setValidators(required ? Validators.required : null);
    control.updateValueAndValidity({ emitEvent: false });
  }
}
