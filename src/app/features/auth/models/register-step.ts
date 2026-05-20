export type RegisterStep = 'identity' | 'personal' | 'career';

export interface RegisterStepViewModel {
  title: string;
  description: string;
  heroIcon: string;
  heroTitle: string;
  heroDescription: string;
  stepLabel: string | null;
  stepTitle: string | null;
  cardSize: 'default' | 'long';
}

export interface AcademicLevel {
  id: number;
  nombre: string;
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
    description: 'Paso 1 de 2',
    heroIcon: 'public',
    heroTitle: 'Proyección global.',
    heroDescription: 'Validá tu talento con una formación alineada a estándares internacionales.',
    stepLabel: 'Paso 1 de 2',
    stepTitle: 'Datos personales',
    cardSize: 'long',
  },
  career: {
    title: 'Interés académico',
    description: 'Paso 2 de 2',
    heroIcon: 'school',
    heroTitle: 'Tu futuro empieza aquí.',
    heroDescription: 'Elegí la propuesta académica que se adapta a tu perfil profesional.',
    stepLabel: 'Paso 2 de 2',
    stepTitle: 'Interés académico',
    cardSize: 'long',
  },
};
