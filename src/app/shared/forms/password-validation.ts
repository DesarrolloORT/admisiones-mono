import { AbstractControl, ValidatorFn, Validators } from '@angular/forms';
import { ValidationUtils } from '@desarrolloort/ngx-utils';

export type OrtPasswordErrorKey =
  | 'minChar'
  | 'maxChar'
  | 'hasUppercase'
  | 'hasLowercase'
  | 'hasNumbers'
  | 'hasSpecialChars';

export interface PasswordRequirement {
  label: string;
  errorKey: OrtPasswordErrorKey;
}

export interface PasswordRequirementStatus {
  label: string;
  met: boolean;
}

export const ORT_PASSWORD_VALIDATORS: ValidatorFn[] = [
  Validators.required,
  ValidationUtils.validateOrtPassword,
];

export const ORT_PASSWORD_GENERIC_ERROR_MESSAGE = 'Debe cumplir con los requisitos de seguridad.';

export const ORT_PASSWORD_ERROR_MESSAGES: Partial<Record<OrtPasswordErrorKey, string>> = {
  minChar: ORT_PASSWORD_GENERIC_ERROR_MESSAGE,
  maxChar: ORT_PASSWORD_GENERIC_ERROR_MESSAGE,
  hasUppercase: ORT_PASSWORD_GENERIC_ERROR_MESSAGE,
  hasLowercase: ORT_PASSWORD_GENERIC_ERROR_MESSAGE,
  hasNumbers: ORT_PASSWORD_GENERIC_ERROR_MESSAGE,
  hasSpecialChars: ORT_PASSWORD_GENERIC_ERROR_MESSAGE,
};

export const ORT_PASSWORD_REQUIREMENTS: PasswordRequirement[] = [
  { label: '12 caracteres como mínimo', errorKey: 'minChar' },
  { label: '20 caracteres como máximo', errorKey: 'maxChar' },
  { label: 'Una letra mayúscula', errorKey: 'hasUppercase' },
  { label: 'Una letra minúscula', errorKey: 'hasLowercase' },
  { label: 'Un número', errorKey: 'hasNumbers' },
  { label: 'Un carácter especial', errorKey: 'hasSpecialChars' },
];

export function buildOrtPasswordRequirements(
  control: AbstractControl
): PasswordRequirementStatus[] {
  return ORT_PASSWORD_REQUIREMENTS.map(requirement => ({
    label: requirement.label,
    met: !control.hasError(requirement.errorKey),
  }));
}

