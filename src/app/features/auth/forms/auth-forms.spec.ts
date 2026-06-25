import {
  createIdentityForm,
  createLoginForm,
  createPersonalForm,
  emailsMatch,
  getDocumentNumberValidators,
} from './auth-forms';

describe('auth forms', () => {
  it('should create login and identity forms with CI defaults', () => {
    expect(createLoginForm().controls.documentType.value).toBe('CI');
    expect(createIdentityForm().controls.documentType.value).toBe('CI');
  });

  it('should validate phone on blur', () => {
    expect(createPersonalForm().controls.telefono1.updateOn).toBe('blur');
  });

  it('should validate matching emails case-insensitively', () => {
    const form = createPersonalForm();

    form.patchValue({
      mail: 'Ana@Example.com',
      verificacionMail: 'ana@example.com',
    });

    expect(emailsMatch(form)).toBe(true);
  });

  it('should attach an email mismatch error to confirmation', () => {
    const form = createPersonalForm();

    form.patchValue({
      mail: 'ana@example.com',
      verificacionMail: 'otro@example.com',
    });

    expect(form.controls.verificacionMail.hasError('emailMismatch')).toBe(true);
  });

  it('should expose document validators by document type', () => {
    expect(getDocumentNumberValidators('CI').length).toBeGreaterThan(1);
    expect(getDocumentNumberValidators('PS').length).toBeGreaterThan(1);
  });
});
