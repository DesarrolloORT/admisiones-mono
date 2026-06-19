export interface EmailConfirmationData {
  actionLabel: string;
  actionRoute: string;
  description: string;
  icon: string;
  requiresTwoFactorState?: boolean;
  secondaryDescription?: string;
  title: string;
}

export interface TwoFactorConfirmationState {
  documentNumber: string;
  documentType: string;
  email: string;
  sessionId: string;
}

export const EMAIL_CONFIRMATION_SPAM_HINT =
  'Si no encontrás el correo, revisá la carpeta de spam o correo no deseado.';

export const REGISTER_EMAIL_CONFIRMATION: EmailConfirmationData = {
  actionLabel: 'Volver al inicio de sesión',
  actionRoute: '/iniciar-sesion',
  description:
    'Revisá tu casilla de e-mail. Te enviamos un enlace de activación para crear tu contraseña y finalizar el registro.',
  icon: 'how_to_reg',
  secondaryDescription: EMAIL_CONFIRMATION_SPAM_HINT,
  title: '¡Cuenta creada con éxito!',
};

export const RECOVER_ACCESS_EMAIL_CONFIRMATION: EmailConfirmationData = {
  actionLabel: 'Volver al inicio de sesión',
  actionRoute: '/iniciar-sesion',
  description: 'Si los datos coinciden, te enviamos un enlace para actualizar tu contraseña.',
  icon: 'mark_email_read',
  secondaryDescription: EMAIL_CONFIRMATION_SPAM_HINT,
  title: 'Revisá tu correo',
};

export const TWO_FACTOR_EMAIL_CONFIRMATION: EmailConfirmationData = {
  actionLabel: 'Ingresar código',
  actionRoute: '/verificar-codigo',
  description: 'Te enviamos un código de verificación a tu correo electrónico.',
  icon: 'mark_email_read',
  requiresTwoFactorState: true,
  secondaryDescription: EMAIL_CONFIRMATION_SPAM_HINT,
  title: 'Código enviado',
};

export function isEmailConfirmationData(value: unknown): value is EmailConfirmationData {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const data = value as Partial<EmailConfirmationData>;

  return (
    typeof data.actionLabel === 'string' &&
    typeof data.actionRoute === 'string' &&
    typeof data.description === 'string' &&
    typeof data.icon === 'string' &&
    typeof data.title === 'string'
  );
}

export function toTwoFactorConfirmationState(value: unknown): TwoFactorConfirmationState | null {
  if (!value || typeof value !== 'object') {
    return null;
  }

  const state = value as Partial<TwoFactorConfirmationState>;
  if (typeof state.sessionId !== 'string' || !state.sessionId) {
    return null;
  }

  return {
    documentNumber: typeof state.documentNumber === 'string' ? state.documentNumber : '',
    documentType: typeof state.documentType === 'string' ? state.documentType : '',
    email: typeof state.email === 'string' ? state.email : '',
    sessionId: state.sessionId,
  };
}
