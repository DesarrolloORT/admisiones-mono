import type { RegisterResult } from '../endpoints/auth.endpoint';

// Frontend input for login form/use case. Mapped to LoginPayload in Auth service.
export interface AuthLoginRequest {
  documentType: string;
  documentNumber: string;
  password: string;
}

// Frontend session model used by local state and storage.
export interface AuthSession {
  token: string | null;
  documentType: string;
  documentNumber: string;
  expiresAt: string | null;
}

export interface AuthIdentityData {
  documentType: string;
  documentNumber: string;
}

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
  telefono1: string;
  mail: string;
  verificacionMail: string;
}

export interface AuthRegisterRequest {
  identity: AuthIdentityData;
  personal: AuthRegisterPersonalData;
}

// Stable type from endpoint adapter (not a backend DTO).
export type AuthRegisterResponse = RegisterResult;

