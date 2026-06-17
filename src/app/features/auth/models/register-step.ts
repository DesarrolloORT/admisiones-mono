export type RegisterStep = 'identity' | 'personal';

export interface RegisterStepViewModel {
  title: string;
  description?: string;
  heroIcon: string;
  heroTitle: string;
  heroDescription: string;
  stepLabel: string | null;
  stepTitle: string | null;
  cardSize: 'default' | 'long';
}

export const REGISTER_STEP_VIEW_MODELS: Record<RegisterStep, RegisterStepViewModel> = {
  identity: {
    title: 'Crear cuenta',
    description: 'El registro te llevará solo unos minutos.',
    heroIcon: 'settings_suggest',
    heroTitle: 'Tecnología aplicada en aulas.',
    heroDescription: 'Aprendizaje práctico en laboratorios de vanguardia desde el primer día.',
    stepLabel: null,
    stepTitle: null,
    cardSize: 'default',
  },
  personal: {
    title: 'Datos personales',
    heroIcon: 'public',
    heroTitle: 'Proyección global.',
    heroDescription: 'Validá tu talento con una formación alineada a estándares internacionales.',
    stepLabel: null,
    stepTitle: 'Datos personales',
    cardSize: 'long',
  },
};

