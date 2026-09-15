import { AbstractControl, ValidatorFn, Validators } from '@angular/forms';
import { PasswordStrengthConfig, ValidationUtils } from '@desarrolloort/ngx-utils';

export type OrtPasswordErrorKey =
  'minChar' | 'maxChar' | 'hasUppercase' | 'hasLowercase' | 'hasNumbers' | 'hasSpecialChars';

export interface PasswordRequirement {
  label: string;
  errorKey: OrtPasswordErrorKey;
}

/** `pending` es el estado inicial: todavía no se escribió nada, así que no se marca error. */
export type PasswordRequirementState = 'pending' | 'met' | 'unmet';

export interface PasswordRequirementStatus {
  label: string;
  state: PasswordRequirementState;
  icon: string;
  srLabel: string;
}

const REQUIREMENT_ICONS: Record<PasswordRequirementState, string> = {
  pending: 'chevron_right',
  met: 'check',
  unmet: 'close',
};

const REQUIREMENT_SR_LABELS: Record<PasswordRequirementState, string> = {
  pending: 'Pendiente: ',
  met: 'Cumplido: ',
  unmet: 'No cumplido: ',
};

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
  { label: 'Como mínimo 12 caracteres', errorKey: 'minChar' },
  { label: 'Como máximo 20 caracteres', errorKey: 'maxChar' },
  { label: 'Al menos una letra mayúscula', errorKey: 'hasUppercase' },
  { label: 'Al menos una letra minúscula', errorKey: 'hasLowercase' },
  { label: 'Al menos un número', errorKey: 'hasNumbers' },
  { label: 'Al menos un caracter especial: $%@_!.-', errorKey: 'hasSpecialChars' },
];

export function buildOrtPasswordRequirements(
  control: AbstractControl
): PasswordRequirementStatus[] {
  const isPending = !control.value;

  return ORT_PASSWORD_REQUIREMENTS.map(requirement => {
    const state: PasswordRequirementState = isPending
      ? 'pending'
      : control.hasError(requirement.errorKey)
        ? 'unmet'
        : 'met';

    return {
      label: requirement.label,
      state,
      icon: REQUIREMENT_ICONS[state],
      srLabel: REQUIREMENT_SR_LABELS[state],
    };
  });
}

export type OrtPasswordStrengthLevel = 'weak' | 'moderate' | 'strong';

export interface OrtPasswordStrength {
  score: number;
  rating: string;
  level: OrtPasswordStrengthLevel;
}

const ORT_PASSWORD_STRENGTH_CONFIG: PasswordStrengthConfig = {
  ratingTexts: {
    weak: 'Débil',
    moderate: 'Moderada',
    strong: 'Fuerte',
  },
};

export function buildOrtPasswordStrength(password: string): OrtPasswordStrength | null {
  if (!password) {
    return null;
  }

  const { score, rating } = ValidationUtils.evalPasswordStrength(
    password,
    ORT_PASSWORD_STRENGTH_CONFIG
  );

  return { score, rating, level: toStrengthLevel(score) };
}

function toStrengthLevel(score: number): OrtPasswordStrengthLevel {
  if (score >= 70) {
    return 'strong';
  }
  if (score >= 40) {
    return 'moderate';
  }
  return 'weak';
}
