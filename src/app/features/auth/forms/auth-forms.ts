import { FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import type { OrtPhoneInputValue } from '@desarrolloort/components';
import {
  ortCedulaValidator as ortNationalIdValidator,
  ortPhoneValidator,
} from '@desarrolloort/components';
import {
  matchingFieldsValidator,
  normalizeEmailValue,
} from 'src/app/shared/forms/matching-fields.validator';
import { uruguayPhoneMaxLengthValidator } from 'src/app/shared/forms/phone';

import { LocationValue } from '../../catalogs/models/location-value';
import { isNationalIdDocumentType } from '../models/document-number';

const DOCUMENT_TYPE_VALIDATORS = [Validators.required, Validators.pattern(/^(CI|PS|DE)$/)];
const NAME_MAX_LENGTH = 100;
const EMAIL_MAX_LENGTH = 254;
const ADDRESS_MAX_LENGTH = 200;

const nationalIdValidator: ValidatorFn = control =>
  ortNationalIdValidator(control) ? { nationalId: true } : null;

export interface LoginForm {
  documentType: FormControl<string>;
  documentNumber: FormControl<string>;
  password: FormControl<string>;
}

export interface IdentityForm {
  documentType: FormControl<string>;
  documentNumber: FormControl<string>;
}

export interface PersonalForm {
  firstName: FormControl<string>;
  middleName: FormControl<string>;
  firstSurname: FormControl<string>;
  secondSurname: FormControl<string>;
  birthDate: FormControl<string | Date | null>;
  sex: FormControl<string>;
  location: FormControl<LocationValue>;
  address: FormControl<string>;
  primaryPhone: FormControl<OrtPhoneInputValue | null>;
  email: FormControl<string>;
  emailConfirmation: FormControl<string>;
}

export interface RecoverAccessForm {
  documentType: FormControl<string>;
  documentNumber: FormControl<string>;
  firstSurname: FormControl<string>;
}

export function createLoginForm(): FormGroup<LoginForm> {
  return new FormGroup<LoginForm>({
    documentType: new FormControl('CI', {
      nonNullable: true,
      validators: DOCUMENT_TYPE_VALIDATORS,
    }),
    documentNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, nationalIdValidator],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });
}

export function createIdentityForm(): FormGroup<IdentityForm> {
  return new FormGroup<IdentityForm>({
    documentType: new FormControl('CI', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    documentNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, nationalIdValidator],
    }),
  });
}

export const NON_NATIONAL_ID_DOCUMENT_NUMBER_VALIDATORS: ValidatorFn[] = [
  Validators.required,
  Validators.maxLength(30),
  Validators.pattern(/^[0-9A-Za-z-]+$/),
];

export const NATIONAL_ID_DOCUMENT_NUMBER_VALIDATORS: ValidatorFn[] = [
  Validators.required,
  nationalIdValidator,
];

export function getDocumentNumberValidators(documentType: string): ValidatorFn[] {
  return isNationalIdDocumentType(documentType)
    ? NATIONAL_ID_DOCUMENT_NUMBER_VALIDATORS
    : NON_NATIONAL_ID_DOCUMENT_NUMBER_VALIDATORS;
}

export function syncDocumentNumberValidators(
  control: FormControl<string>,
  documentType: string
): void {
  control.setValidators(getDocumentNumberValidators(documentType));
  control.updateValueAndValidity({ emitEvent: false });
}

export function createPersonalForm(): FormGroup<PersonalForm> {
  return new FormGroup<PersonalForm>(
    {
      firstName: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(NAME_MAX_LENGTH)],
      }),
      middleName: new FormControl('', {
        nonNullable: true,
        validators: [Validators.maxLength(NAME_MAX_LENGTH)],
      }),
      firstSurname: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(NAME_MAX_LENGTH)],
      }),
      secondSurname: new FormControl('', {
        nonNullable: true,
        validators: [Validators.maxLength(NAME_MAX_LENGTH)],
      }),
      birthDate: new FormControl<string | Date | null>(null, {
        validators: [Validators.required],
      }),
      sex: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      location: new FormControl<LocationValue>(
        { countryCode: null, stateCode: null, cityCode: null },
        { nonNullable: true }
      ),
      address: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(ADDRESS_MAX_LENGTH)],
      }),
      primaryPhone: new FormControl<OrtPhoneInputValue | null>(null, {
        validators: [Validators.required, ortPhoneValidator, uruguayPhoneMaxLengthValidator],
        updateOn: 'blur',
      }),
      email: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email, Validators.maxLength(EMAIL_MAX_LENGTH)],
      }),
      emailConfirmation: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email, Validators.maxLength(EMAIL_MAX_LENGTH)],
      }),
    },
    {
      validators: [
        matchingFieldsValidator('email', 'emailConfirmation', {
          errorKey: 'emailMismatch',
          normalize: normalizeEmailValue,
        }),
      ],
    }
  );
}

export function createRecoverAccessForm(): FormGroup<RecoverAccessForm> {
  return new FormGroup<RecoverAccessForm>({
    documentType: new FormControl('CI', {
      nonNullable: true,
      validators: DOCUMENT_TYPE_VALIDATORS,
    }),
    documentNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, nationalIdValidator],
    }),
    firstSurname: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(NAME_MAX_LENGTH)],
    }),
  });
}
