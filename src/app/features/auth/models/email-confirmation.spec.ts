import { isEmailConfirmationData, RECOVER_ACCESS_EMAIL_CONFIRMATION } from './email-confirmation';

describe('EmailConfirmationData', () => {
  it('accepts complete confirmation data', () => {
    expect(isEmailConfirmationData(RECOVER_ACCESS_EMAIL_CONFIRMATION)).toBe(true);
  });

  it('rejects incomplete confirmation data', () => {
    expect(isEmailConfirmationData({ title: 'Incomplete' })).toBe(false);
  });
});
