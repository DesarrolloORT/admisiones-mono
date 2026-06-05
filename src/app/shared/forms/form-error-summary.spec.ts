import { FormControl, FormGroup, Validators } from '@angular/forms';

import {
  buildFormErrorSummary,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from './form-error-summary';

describe('buildFormErrorSummary', () => {
  it('should build one error item per invalid configured control', () => {
    const form = new FormGroup({
      email: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      name: new FormControl('Ana', { nonNullable: true, validators: [Validators.required] }),
    });

    const result = buildFormErrorSummary(form, [
      { controlName: 'email', fieldId: 'email', label: 'E-mail' },
      { controlName: 'name', fieldId: 'name', label: 'Nombre' },
    ]);

    expect(result).toEqual([{ fieldId: 'email', message: 'E-mail es obligatorio.' }]);
  });

  it('should use custom messages when provided', () => {
    const form = new FormGroup({
      documentNumber: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
    });

    const result = buildFormErrorSummary(form, [
      {
        controlName: 'documentNumber',
        fieldId: 'document-number',
        label: 'Documento',
        messages: { required: 'Ingresá tu documento.' },
      },
    ]);

    expect(result[0]?.message).toBe('Ingresá tu documento.');
  });

  it('should omit field links when the component target is unsupported', () => {
    const form = new FormGroup({
      email: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    });

    const result = buildFormErrorSummary(
      form,
      [{ controlName: 'email', fieldId: 'email', label: 'E-mail' }],
      ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED
    );

    expect(result).toEqual([{ message: 'E-mail es obligatorio.' }]);
  });
});
