import {
  createIdentityForm,
  createLoginForm,
  createPersonalForm,
  getDocumentNumberValidators,
} from './auth-forms';

describe('auth forms', () => {
  it('should create login and identity forms with CI defaults', () => {
    expect(createLoginForm().controls.documentType.value).toBe('CI');
    expect(createIdentityForm().controls.documentType.value).toBe('CI');
  });

  it('should validate phone on blur', () => {
    expect(createPersonalForm().controls.primaryPhone.updateOn).toBe('blur');
  });

  it('should reject more than eight digits only for a Uruguayan phone', () => {
    const phone = createPersonalForm().controls.primaryPhone;

    phone.setValue({ iso2: 'UY', number: '099123456', numberE164: '' });
    expect(phone.hasError('phone')).toBe(true);

    phone.setValue({ iso2: 'AR', number: '91123456789', numberE164: '' });
    expect(phone.hasError('phone')).toBe(false);
  });

  it('should accept matching emails case-insensitively', () => {
    const form = createPersonalForm();

    form.patchValue({
      email: 'Ana@Example.com',
      emailConfirmation: 'ana@example.com',
    });

    expect(form.controls.emailConfirmation.hasError('emailMismatch')).toBe(false);
  });

  it('should attach an email mismatch error to confirmation', () => {
    const form = createPersonalForm();

    form.patchValue({
      email: 'ana@example.com',
      emailConfirmation: 'otro@example.com',
    });

    expect(form.controls.emailConfirmation.hasError('emailMismatch')).toBe(true);
  });

  it('should expose document validators by document type', () => {
    expect(getDocumentNumberValidators('CI').length).toBeGreaterThan(1);
    expect(getDocumentNumberValidators('PS').length).toBeGreaterThan(1);
  });
});
