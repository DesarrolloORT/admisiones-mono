import type { PhoneNumberValue } from 'src/app/shared/forms/phone';

// Frontend input for login form/use case. Mapped to LoginPayload in AuthSessionService.
export interface AuthLoginRequest {
  documentType: string;
  documentNumber: string;
  password: string;
}

// Cookie-backed session metadata kept only in memory.
export interface AuthSession {
  documentType: string;
  documentNumber: string;
  firstName: string;
}

export interface AuthIdentityData {
  documentType: string;
  documentNumber: string;
}

export type AuthPhoneNumber = PhoneNumberValue;

export interface AuthRegisterPersonalData {
  firstName: string;
  middleName: string;
  firstSurname: string;
  secondSurname: string;
  birthDate: string;
  sex: string;
  countryCode: number | null;
  stateCode: number | null;
  cityCode: number | null;
  address: string;
  primaryPhone: AuthPhoneNumber;
  email: string;
  emailConfirmation: string;
}

export interface AuthRegisterRequest {
  identity: AuthIdentityData;
  personal: AuthRegisterPersonalData;
}
