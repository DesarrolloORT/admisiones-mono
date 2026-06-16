import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtSelectModule,
} from '@desarrolloort/components';
import { isNormalizedApiError } from '@desarrolloort/ngx-utils';

import {
  buildFormErrorSummary,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from '../../../../shared/forms/form-error-summary';
import { AuthForm } from '../../components/auth-form/auth-form';
import { createLoginForm, syncDocumentNumberValidators } from '../../forms/auth-forms';
import { cleanDocumentNumber, isCedulaDocumentType } from '../../models/document-number';
import { AuthSessionService } from '../../services/auth-session';

@Component({
  selector: 'app-login',
  imports: [
    AuthForm,
    OrtFormFieldModule,
    OrtInputModule,
    OrtSelectModule,
    OrtButtonModule,
    OrtIconModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Login {
  private readonly authSession = inject(AuthSessionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly passwordInput = viewChild<ElementRef<HTMLInputElement>>('passwordInput');

  protected readonly form = createLoginForm();
  protected readonly isSubmitting = signal(false);
  protected readonly showPassword = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly submitted = signal(false);
  protected readonly passwordInputType = computed(() =>
    this.showPassword() ? 'text' : 'password'
  );
  protected readonly passwordIcon = computed(() =>
    this.showPassword() ? 'visibility_off' : 'visibility'
  );
  protected readonly passwordToggleLabel = computed(() =>
    this.showPassword() ? 'Ocultar contraseña' : 'Mostrar contraseña'
  );
  protected readonly errorSummary = computed(() =>
    this.submitted()
      ? buildFormErrorSummary(
          this.form,
          [
            {
              controlName: 'documentType',
              fieldId: 'login-document-type',
              label: 'Tipo de documento',
            },
            {
              controlName: 'documentNumber',
              fieldId: 'login-document-number',
              label: 'Nro. de documento',
              messages: {
                pattern: 'Ingresá solo caracteres alfanuméricos.',
              },
            },
            {
              controlName: 'password',
              fieldId: 'login-password',
              label: 'Contraseña',
            },
          ],
          ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED
        )
      : []
  );

  private readonly documentTypeValue = toSignal(this.form.controls.documentType.valueChanges, {
    initialValue: this.form.controls.documentType.value,
  });

  protected readonly isCedulaInput = computed(() => isCedulaDocumentType(this.documentTypeValue()));
  protected readonly prefilled = signal(false);

  constructor() {
    window.__TEST_RUN_ID__ = 'manual-front-telemetry-20260529-1';

    effect(() => {
      syncDocumentNumberValidators(this.form.controls.documentNumber, this.documentTypeValue());
    });

    this.prefillFromQueryParams();

    if (this.prefilled()) {
      afterNextRender(() => this.passwordInput()?.nativeElement.focus());
    }
  }

  protected togglePasswordVisibility(): void {
    this.showPassword.update(value => !value);
  }

  protected submit(): void {
    this.submitted.set(true);
    this.error.set(null);
    this.successMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    const { documentType, documentNumber, password } = this.form.getRawValue();
    const cleanedDocumentNumber = cleanDocumentNumber(documentType, documentNumber);

    this.authSession
      .login({
        documentType,
        documentNumber: cleanedDocumentNumber,
        password,
      })
      .subscribe({
        next: outcome => {
          if (outcome.kind === 'twoFactorRequired') {
            this.form.controls.password.reset('');
            this.router
              .navigate(['/verificar-codigo'], {
                state: {
                  email: outcome.maskedEmail,
                  sessionId: outcome.sessionId,
                  documentType,
                  documentNumber: cleanedDocumentNumber,
                },
              })
              .finally(() => this.isSubmitting.set(false));
            return;
          }

          this.successMessage.set('Sesión iniciada correctamente.');
          this.form.controls.password.reset('');
          this.router.navigateByUrl('/inicio').finally(() => this.isSubmitting.set(false));
        },
        error: error => {
          this.isSubmitting.set(false);
          this.error.set(this.getApiErrorMessage(error, 'No se pudo iniciar sesión.'));
        },
      });
  }

  private prefillFromQueryParams(): void {
    const params = this.route.snapshot.queryParamMap;
    const tipoDoc = params.get('tipoDoc');
    const doc = params.get('doc');

    if (tipoDoc) {
      this.form.controls.documentType.setValue(tipoDoc);
    }
    if (doc) {
      this.form.controls.documentNumber.setValue(doc);
    }

    if (tipoDoc && doc) {
      this.prefilled.set(true);
    }
  }

  private getApiErrorMessage(error: unknown, fallback: string): string {
    return isNormalizedApiError(error) ? error.message : fallback;
  }
}
