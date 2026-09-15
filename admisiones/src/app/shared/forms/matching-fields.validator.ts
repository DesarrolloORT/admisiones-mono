import { AbstractControl, ValidatorFn } from '@angular/forms';

export interface MatchingFieldsValidatorOptions {
  errorKey?: string;
  normalize?: (value: unknown) => unknown;
}

export function matchingFieldsValidator(
  sourceName: string,
  confirmationName: string,
  options: MatchingFieldsValidatorOptions = {}
): ValidatorFn {
  const errorKey = options.errorKey ?? 'fieldMismatch';
  const normalize = options.normalize ?? (value => value);

  return control => {
    const source = control.get(sourceName);
    const confirmation = control.get(confirmationName);

    if (!source || !confirmation) {
      return null;
    }

    const sourceValue = normalize(source.value);
    const confirmationValue = normalize(confirmation.value);

    if (
      isEmptyValue(sourceValue) ||
      isEmptyValue(confirmationValue) ||
      hasBlockingErrors(source, errorKey) ||
      hasBlockingErrors(confirmation, errorKey)
    ) {
      clearControlError(confirmation, errorKey);
      return null;
    }

    if (sourceValue !== confirmationValue) {
      setControlError(confirmation, errorKey);
      return { [errorKey]: true };
    }

    clearControlError(confirmation, errorKey);
    return null;
  };
}

export function normalizeEmailValue(value: unknown): string {
  return String(value ?? '')
    .trim()
    .toLowerCase();
}

function isEmptyValue(value: unknown): boolean {
  return value === null || value === undefined || value === '';
}

function hasBlockingErrors(control: AbstractControl, transientErrorKey: string): boolean {
  const errors = control.errors ?? {};

  return Object.keys(errors).some(errorKey => errorKey !== transientErrorKey);
}

function setControlError(control: AbstractControl, errorKey: string): void {
  if (control.hasError(errorKey)) {
    return;
  }

  control.setErrors({
    ...(control.errors ?? {}),
    [errorKey]: true,
  });
}

function clearControlError(control: AbstractControl, errorKey: string): void {
  if (!control.hasError(errorKey)) {
    return;
  }

  const errors = { ...(control.errors ?? {}) };
  delete errors[errorKey];

  control.setErrors(Object.keys(errors).length ? errors : null);
}
