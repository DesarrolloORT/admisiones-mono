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
  primerNombre: string;
}

export interface AuthIdentityData {
  documentType: string;
  documentNumber: string;
}

export type AuthPhoneNumber = PhoneNumberValue;

export interface AuthRegisterPersonalData {
  primerNombre: string;
  segundoNombre: string;
  primerApellido: string;
  segundoApellido: string;
  fechaNacimiento: string;
  sexo: string;
  codigoPais: number | null;
  codigoEstado: number | null;
  codigoCiudad: number | null;
  direccion: string;
  telefono1: AuthPhoneNumber;
  mail: string;
  verificacionMail: string;
}

export interface AuthRegisterRequest {
  identity: AuthIdentityData;
  personal: AuthRegisterPersonalData;
}
