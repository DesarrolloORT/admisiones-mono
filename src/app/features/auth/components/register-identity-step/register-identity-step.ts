import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtSelectModule,
} from '@desarrolloort/components';
import { Observable } from 'rxjs';

import { DocumentType } from '../../../catalogs/models/catalog.interface';
import { IdentityForm } from '../../forms/auth-forms';

@Component({
  selector: 'app-register-identity-step',
  imports: [
    AsyncPipe,
    OrtButtonModule,
    OrtFormFieldModule,
    OrtIconModule,
    OrtInputModule,
    OrtSelectModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './register-identity-step.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterIdentityStep {
  public readonly form = input.required<FormGroup<IdentityForm>>();
  public readonly documentTypes = input.required<Observable<DocumentType[]>>();
  public readonly isCedulaInput = input(false);
  public readonly selectedFileName = input<string | null>(null);
  public readonly isSubmitting = input(false);
  public readonly isRecognizingDocument = input(false);
  public readonly error = input<string | null>(null);
  public readonly recognitionError = input<string | null>(null);
  public readonly recognitionSuccessMessage = input<string | null>(null);

  public readonly documentSelected = output<Event>();
  public readonly continueStep = output<void>();
}

