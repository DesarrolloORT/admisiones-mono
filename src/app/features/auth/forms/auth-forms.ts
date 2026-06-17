import { FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { ortCedulaValidator } from '@desarrolloort/components';
import {
  matchingFieldsValidator,
  normalizeEmailValue,
} from 'src/app/shared/forms/matching-fields.validator';

import { isCedulaDocumentType } from '../models/document-number';
import { LocationValue } from '../models/location-value';

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
  telefono1: FormControl<string>;
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
      validators: [Validators.required],
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
      primerNombre: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      segundoNombre: new FormControl('', { nonNullable: true }),
      primerApellido: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      segundoApellido: new FormControl('', { nonNullable: true }),
      fechaNacimiento: new FormControl<string | Date | null>(null, {
        validators: [Validators.required],
      }),
      sexo: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      location: new FormControl<LocationValue>(
        { codigoPais: null, codigoEstado: null, codigoCiudad: null },
        { nonNullable: true }
      ),
      direccion: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      telefono1: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      mail: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email],
      }),
      verificacionMail: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email],
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

export function emailsMatch(form: FormGroup<PersonalForm>): boolean {
  const { mail, verificacionMail } = form.getRawValue();
  return mail.trim().toLowerCase() === verificacionMail.trim().toLowerCase();
}

export function createRecoverAccessForm(): FormGroup<RecoverAccessForm> {
  return new FormGroup<RecoverAccessForm>({
    documentType: new FormControl('CI', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    documentNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, ortCedulaValidator],
    }),
    primerApellido: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });
}
