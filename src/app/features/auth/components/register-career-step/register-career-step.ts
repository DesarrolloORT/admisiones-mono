import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtRadioModule,
  OrtSelectModule,
} from '@desarrolloort/components';
import {
  buildFormErrorSummary,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from 'src/app/shared/forms/form-error-summary';

import { Career, Comienzo } from '../../../catalogs/models/catalog.interface';
import { CareerForm } from '../../forms/auth-forms';
import { AcademicLevel } from '../../models/register-step';

@Component({
  selector: 'app-register-career-step',
  imports: [
    OrtButtonModule,
    OrtFormFieldModule,
    OrtIconModule,
    OrtRadioModule,
    OrtSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './register-career-step.html',
  styleUrl: './register-career-step.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterCareerStep {
  public readonly form = input.required<FormGroup<CareerForm>>();
  public readonly academicLevels = input.required<AcademicLevel[]>();
  public readonly filteredCareers = input.required<Career[]>();
  public readonly comienzos = input.required<Comienzo[]>();
  public readonly isSubmitting = input(false);
  public readonly error = input<string | null>(null);
  public readonly successMessage = input<string | null>(null);

  public readonly back = output<void>();
  public readonly propuestaChange = output<void>();
  public readonly carreraChange = output<void>();
  public readonly submitStep = output<void>();

  public readonly submitted = signal(false);
  public readonly errorSummary = computed(() => {
    if (!this.submitted()) {
      return [];
    }

    return buildFormErrorSummary(
      this.form(),
      [
        {
          controlName: 'propuestaAcademica',
          fieldId: 'academic-level-group',
          label: 'Propuesta académica',
          messages: { required: 'Seleccioná una propuesta académica.' },
        },
        { controlName: 'carrera', fieldId: 'career-select', label: 'Carrera' },
        { controlName: 'comienzo', fieldId: 'start-select', label: 'Comienzo' },
      ],
      ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED
    );
  });

  public onPropuestaChange(): void {
    this.propuestaChange.emit();
  }

  public onCarreraChange(): void {
    this.carreraChange.emit();
  }

  public onSubmit(): void {
    this.submitted.set(true);
    this.submitStep.emit();
  }
}
