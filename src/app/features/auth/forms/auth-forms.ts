import { FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import type { OrtPhoneInputValue } from '@desarrolloort/components';
import { ortCedulaValidator, ortPhoneValidator } from '@desarrolloort/components';
import {
  matchingFieldsValidator,
  normalizeEmailValue,
} from 'src/app/shared/forms/matching-fields.validator';

import { LocationValue } from '../../catalogs/models/location-value';
import { isCedulaDocumentType } from '../models/document-number';

const DOCUMENT_TYPE_VALIDATORS = [Validators.required, Validators.pattern(/^(CI|PS|DE)$/)];
const NAME_MAX_LENGTH = 100;
const EMAIL_MAX_LENGTH = 254;
const ADDRESS_MAX_LENGTH = 200;

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
  primerNombre: FormControl<string>;
  segundoNombre: FormControl<string>;
  primerApellido: FormControl<string>;
  segundoApellido: FormControl<string>;
  fechaNacimiento: FormControl<string | Date | null>;
  sexo: FormControl<string>;
  location: FormControl<LocationValue>;
  direccion: FormControl<string>;
  telefono1: FormControl<OrtPhoneInputValue | null>;
  mail: FormControl<string>;
  verificacionMail: FormControl<string>;
}

export interface RecoverAccessForm {
  documentType: FormControl<string>;
  documentNumber: FormControl<string>;
  primerApellido: FormControl<string>;
}

export function createLoginForm(): FormGroup<LoginForm> {
  return new FormGroup<LoginForm>({
    documentType: new FormControl('CI', {
      nonNullable: true,
      validators: DOCUMENT_TYPE_VALIDATORS,
    }),
    documentNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, ortCedulaValidator],
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
      validators: [Validators.required, ortCedulaValidator],
    }),
  });
}

export const NON_CEDULA_DOCUMENT_NUMBER_VALIDATORS: ValidatorFn[] = [
  Validators.required,
  Validators.maxLength(30),
  Validators.pattern(/^[0-9A-Za-z-]+$/),
];

export const CEDULA_DOCUMENT_NUMBER_VALIDATORS: ValidatorFn[] = [
  Validators.required,
  ortCedulaValidator,
];

export function getDocumentNumberValidators(documentType: string): ValidatorFn[] {
  return isCedulaDocumentType(documentType)
    ? CEDULA_DOCUMENT_NUMBER_VALIDATORS
    : NON_CEDULA_DOCUMENT_NUMBER_VALIDATORS;
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
      primerNombre: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(NAME_MAX_LENGTH)],
      }),
      segundoNombre: new FormControl('', {
        nonNullable: true,
        validators: [Validators.maxLength(NAME_MAX_LENGTH)],
      }),
      primerApellido: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(NAME_MAX_LENGTH)],
      }),
      segundoApellido: new FormControl('', {
        nonNullable: true,
        validators: [Validators.maxLength(NAME_MAX_LENGTH)],
      }),
      fechaNacimiento: new FormControl<string | Date | null>(null, {
        validators: [Validators.required],
      }),
      sexo: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      location: new FormControl<LocationValue>(
        { countryCode: null, stateCode: null, cityCode: null },
        { nonNullable: true }
      ),
      direccion: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(ADDRESS_MAX_LENGTH)],
      }),
      telefono1: new FormControl<OrtPhoneInputValue | null>(null, {
        validators: [Validators.required, ortPhoneValidator],
        updateOn: 'blur',
      }),
      mail: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email, Validators.maxLength(EMAIL_MAX_LENGTH)],
      }),
      verificacionMail: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email, Validators.maxLength(EMAIL_MAX_LENGTH)],
      }),
    },
    {
      validators: [
        matchingFieldsValidator('mail', 'verificacionMail', {
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
      validators: [Validators.required, ortCedulaValidator],
    }),
    primerApellido: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(NAME_MAX_LENGTH)],
    }),
  });
}
