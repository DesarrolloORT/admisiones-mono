import { ORT_PASSWORD_REQUIREMENTS } from './password-validation';

describe('ORT_PASSWORD_REQUIREMENTS', () => {
  it('lists the password rules shown to the user', () => {
    expect(ORT_PASSWORD_REQUIREMENTS.map(requirement => requirement.errorKey)).toContain('minChar');
  });
});
