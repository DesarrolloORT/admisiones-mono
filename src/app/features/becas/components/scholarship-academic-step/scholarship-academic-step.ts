import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  OrtAccordionModule,
  OrtButtonModule,
  OrtFormFieldModule,
  OrtInputModule,
  OrtRadioModule,
} from '@desarrolloort/components';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

// eslint-disable-next-line no-restricted-imports
import {
  ScholarshipAcademicStepData,
  ScholarshipEndpoint,
} from '../../endpoints/scholarship.endpoint';
import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
import { ScholarshipVariant } from '../scholarship-personal-step/scholarship-personal-step';

@Component({
  selector: 'app-scholarship-academic-step',
  imports: [
    OrtButtonModule,
    ReactiveFormsModule,
    OrtAccordionModule,
    OrtFormFieldModule,
    OrtInputModule,
    OrtRadioModule,
    ErrorAlert,
  ],
  templateUrl: './scholarship-academic-step.html',
  styleUrls: ['../../pages/fbr/fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipAcademicStep {
  protected readonly facade = inject(ScholarshipProcessFacade);
  private readonly scholarshipEndpoint = inject(ScholarshipEndpoint);
  readonly variant = input.required<ScholarshipVariant>();

  protected readonly academicStepData = signal<ScholarshipAcademicStepData[]>([]);
  protected readonly submitted = signal(false);
  protected readonly selectedInscription = signal<string | null>(null);
  protected readonly inscriptionForm = new FormGroup({
    selectedInscription: new FormControl<string | null>(null, Validators.required),
    applicationMode: new FormControl<string | null>(null),
    evaluationDate: new FormControl<string | null>(null),
  });
  protected readonly selectionControl = this.inscriptionForm.controls.selectedInscription;
  protected readonly applicationModeControl = this.inscriptionForm.controls.applicationMode;
  protected readonly evaluationDateControl = this.inscriptionForm.controls.evaluationDate;
  protected readonly selectedAcademicStepData = computed(() => {
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

  constructor() {
    this.scholarshipEndpoint.getAcademicStepData().subscribe(data => {
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

  protected onInscriptionSelectionChange(value: string | null): void {
    this.selectedInscription.set(value);
    this.selectionControl.setValue(value);
    this.selectionControl.markAsTouched();
  }

  protected onContinue(): void {
    this.submitted.set(true);
    this.inscriptionForm.markAllAsTouched();

    if (this.inscriptionForm.invalid) {
      return;
    }

    this.facade.continue();
  }

  private emptyAcademicStepData(): ScholarshipAcademicStepData {
    return { carrera: '', comienzo: '', turno: '' };
  }

  protected showErrorAlert(): boolean {
    return this.submitted() && this.inscriptionForm.invalid;
  }
}
