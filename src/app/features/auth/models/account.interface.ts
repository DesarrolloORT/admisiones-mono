import type { StoredPhoneNumber } from 'src/app/shared/forms/phone';

import type { AuthPhoneNumber } from './auth.interface';

export interface AccountPersonalData {
  documentType: string;
  documentNumber: string;
  firstName: string;
  secondName: string;
  firstLastName: string;
  secondLastName: string;
  birthDate: string;
  sex: string;
  countryCode: number | null;
  stateCode: number | null;
  cityCode: number | null;
  address: string;
  phone: StoredPhoneNumber;
  email: string;
  emailVerification: string;
  identityRestricted: boolean;
}

export interface UpdateAccountPersonalDataPayload {
  countryCode?: number;
  stateCode?: number;
  cityCode?: number;
  address: string;
  phone: AuthPhoneNumber;
  email: string;
  emailVerification: string;
}

export interface AccountChangePasswordPayload {
  currentPassword: string;
  password: string;
}
