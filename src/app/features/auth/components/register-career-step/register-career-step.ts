import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtSelectModule,
} from '@desarrolloort/components';

import { Career, Comienzo } from '../../../catalogs/models/catalog.interface';
import { CareerForm } from '../../forms/auth-forms';
import { AcademicLevel } from '../../models/register-step';

@Component({
  selector: 'app-register-career-step',
  imports: [
    OrtButtonModule,
    OrtFormFieldModule,
    OrtIconModule,
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
}
