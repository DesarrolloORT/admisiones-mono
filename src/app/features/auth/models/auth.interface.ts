export interface AuthLoginRequest {
  documentType: string;
  documentNumber: string;
  password: string;
}

export interface AuthLoginResponse {
  token?: string;
  accessToken?: string;
  expiresAt?: string;
  data?: {
    token?: string;
    accessToken?: string;
    expiresAt?: string;
  };
  [key: string]: unknown;
}

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
  firstName: string;
  secondName: string;
  firstLastName: string;
  secondLastName: string;
  birthDate: string;
  sex: string;
  country: string;
  address: string;
  phone: string;
  email: string;
  confirmEmail: string;
}

export interface AuthRegisterRequest {
  identity: AuthIdentityData;
  personal: AuthRegisterPersonalData;
}

export interface AuthRegisterResponse {
  success?: boolean;
  message?: string;
  [key: string]: unknown;
}
