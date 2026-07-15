import { ChangeDetectionStrategy, Component, computed, effect, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { OrtFormFieldModule, OrtInputModule, OrtSelectModule } from '@desarrolloort/components';

import { syncDocumentNumberValidators } from '../../../forms/auth-forms';
import { isCedulaDocumentType } from '../../../models/document-number';

// Solo se exige la parte estructural que usa el componente (`controls`): así los
// FormGroup de login/recuperación/registro, que tienen campos extra, son asignables
// sin pelear con la varianza de FormGroup<T>.
export type DocumentFieldsGroup = Pick<
  FormGroup<{
    documentType: FormControl<string>;
    documentNumber: FormControl<string>;
  }>,
  'controls'
>;

/**
 * Par "tipo de documento + número" compartido por login, recuperar acceso y el
 * paso de identidad del registro. Mantiene los validadores del número en sync
 * con el tipo elegido y genera los ids `${idPrefix}-document-type` /
 * `${idPrefix}-document-number` que usan los error summaries de cada página.
 */
@Component({
  selector: 'app-document-fields',
  imports: [OrtFormFieldModule, OrtInputModule, OrtSelectModule, ReactiveFormsModule],
  templateUrl: './document-fields.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocumentFields {
  public readonly form = input.required<DocumentFieldsGroup>();
  public readonly idPrefix = input.required<string>();
  public readonly autocomplete = input<'username' | null>(null);
  public readonly numberLabel = input('Nro. de documento');

  private readonly documentTypeValue = signal('CI');
  protected readonly isCedulaInput = computed(() => isCedulaDocumentType(this.documentTypeValue()));

  constructor() {
    effect(onCleanup => {
      const documentType = this.form().controls.documentType;
      this.documentTypeValue.set(documentType.value);
      const subscription = documentType.valueChanges.subscribe(value =>
        this.documentTypeValue.set(value)
      );
      onCleanup(() => subscription.unsubscribe());
    });
    effect(() => {
      syncDocumentNumberValidators(this.form().controls.documentNumber, this.documentTypeValue());
    });
  }
}
