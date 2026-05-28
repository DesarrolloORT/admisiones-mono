import { FormControl, FormGroup } from '@angular/forms';

import { matchingFieldsValidator, normalizeEmailValue } from './matching-fields.validator';

describe('matchingFieldsValidator', () => {
  it('should set and clear a mismatch error on the confirmation control', () => {
    const form = new FormGroup(
      {
        email: new FormControl('ana@example.com', { nonNullable: true }),
        confirmEmail: new FormControl('other@example.com', { nonNullable: true }),
      },
      {
        validators: [
          matchingFieldsValidator('email', 'confirmEmail', {
            errorKey: 'emailMismatch',
            normalize: normalizeEmailValue,
          }),
        ],
      }
    );

    expect(form.controls.confirmEmail.hasError('emailMismatch')).toBe(true);

    form.controls.confirmEmail.setValue('ANA@example.com');

    expect(form.controls.confirmEmail.hasError('emailMismatch')).toBe(false);
  });
});
