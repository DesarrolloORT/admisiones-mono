import { FormControl } from '@angular/forms';

import {
  buildOrtPasswordRequirements,
  ORT_PASSWORD_REQUIREMENTS,
  ORT_PASSWORD_VALIDATORS,
} from './password-validation';

describe('ORT_PASSWORD_REQUIREMENTS', () => {
  it('lists the password rules shown to the user', () => {
    expect(ORT_PASSWORD_REQUIREMENTS.map(requirement => requirement.errorKey)).toContain('minChar');
  });
});

describe('buildOrtPasswordRequirements', () => {
  function createControl(value = '') {
    return new FormControl(value, { nonNullable: true, validators: ORT_PASSWORD_VALIDATORS });
  }

  it('keeps every requirement neutral while nothing was typed', () => {
    const statuses = buildOrtPasswordRequirements(createControl());

    expect(statuses.every(status => status.state === 'pending')).toBe(true);
    expect(statuses.every(status => status.icon === 'chevron_right')).toBe(true);
  });

  it('marks unmet requirements with a cross once there is a value', () => {
    const statuses = buildOrtPasswordRequirements(createControl('abc'));
    const minChar = statuses[0];

    expect(minChar.state).toBe('unmet');
    expect(minChar.icon).toBe('close');
    expect(minChar.srLabel).toBe('No cumplido: ');
  });

  it('marks met requirements with a check', () => {
    const statuses = buildOrtPasswordRequirements(createControl('Contrasena1$'));

    expect(statuses.every(status => status.state === 'met')).toBe(true);
    expect(statuses.every(status => status.icon === 'check')).toBe(true);
  });
});
