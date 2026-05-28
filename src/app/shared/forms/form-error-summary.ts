import { AbstractControl, FormGroup, ValidationErrors } from '@angular/forms';
import type { OrtErrorItem } from '@desarrolloort/components';

export interface FormErrorField {
  controlName: string;
  fieldId: string | ((control: AbstractControl) => string);
  label: string;
  messages?: Partial<Record<string, string>>;
}

interface FormErrorSummaryOptions {
  includeFieldLinks?: boolean;
}

/**
 * TODO(a11y-ort-component): OrtInput, OrtSelect and OrtRadioGroup generate
 * internal ids and do not expose a public stable focus target for
 * OrtErrorSummary links. Use this option in ORT form usage points so the
 * summary remains announced without rendering broken anchors. Remove it when
 * @desarrolloort/components exposes a supported field target/focus API.
 */
export const ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED: FormErrorSummaryOptions = {
  includeFieldLinks: false,
};

export function buildFormErrorSummary(
  form: FormGroup,
  fields: FormErrorField[],
  options: FormErrorSummaryOptions = {}
): OrtErrorItem[] {
  const errors: OrtErrorItem[] = [];
  const includeFieldLinks = options.includeFieldLinks ?? true;

  for (const field of fields) {
    const control = form.get(field.controlName);

    if (!control?.invalid) {
      continue;
    }

    const item: OrtErrorItem = {
      message: getControlErrorMessage(control, field.label, field.messages),
    };

    if (includeFieldLinks) {
      item.fieldId = resolveFieldId(field.fieldId, control);
    }

    errors.push(item);
  }

  return errors;
}

function resolveFieldId(fieldId: FormErrorField['fieldId'], control: AbstractControl): string {
  return typeof fieldId === 'function' ? fieldId(control) : fieldId;
}

export function getControlErrorMessage(
  control: AbstractControl,
  label: string,
  messages: Partial<Record<string, string>> = {}
): string {
  const errors = control.errors ?? {};
  const firstErrorKey = Object.keys(errors)[0];

  if (!firstErrorKey) {
    return `Revisá ${label.toLowerCase()}.`;
  }

  return messages[firstErrorKey] ?? getDefaultErrorMessage(label, firstErrorKey, errors);
}

function getDefaultErrorMessage(label: string, errorKey: string, errors: ValidationErrors): string {
  switch (errorKey) {
    case 'required':
      return `${label} es obligatorio.`;
    case 'email':
      return 'Ingresá un e-mail válido.';
    case 'cedula':
      return 'Ingresá un número de cédula válido.';
    case 'pattern':
      return `El formato de ${label.toLowerCase()} no es válido.`;
    case 'minlength':
      return `${label} debe tener al menos ${errors['minlength']?.requiredLength} caracteres.`;
    case 'maxlength':
      return `${label} debe tener como máximo ${errors['maxlength']?.requiredLength} caracteres.`;
    case 'passwordMismatch':
      return 'Las contraseñas no coinciden.';
    case 'confirmPasswordMismatch':
      return 'Las contraseñas no coinciden.';
    case 'minChar':
    case 'maxChar':
    case 'hasUppercase':
    case 'hasLowercase':
    case 'hasNumbers':
    case 'hasSpecialChars':
      return 'Debe cumplir con los requisitos de seguridad.';
    case 'emailMismatch':
      return 'Los e-mails ingresados no coinciden.';
    case 'locationRequired':
      return 'Seleccioná el país de residencia.';
    case 'locationStateRequired':
      return 'Seleccioná el departamento o estado.';
    case 'locationCityRequired':
      return 'Seleccioná la ciudad.';
    default:
      return `Revisá ${label.toLowerCase()}.`;
  }
}
