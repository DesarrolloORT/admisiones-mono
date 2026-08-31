import type { EnrollmentOption, PaymentMethod, StudentServiceLink } from './enrollment-flow';

export const SANTANDER_ACCOUNT_URL = 'https://misolicitud.santander.com.uy/vb/?productType=OBD';

export type PaymentOption = EnrollmentOption & {
  value: PaymentMethod;
  badges?: readonly string[];
  availableAmount?: number;
  disabled?: boolean;
};

export const PAYMENT_OPTIONS: readonly PaymentOption[] = [
  {
    value: 'bank-account',
    label: 'Pago con banco',
    hint: 'Desde tu banco por Sistarbanc',
  },
  {
    value: 'personal-account',
    label: 'Cuenta personal',
    hint: 'Monto disponible no informado',
  },
  {
    value: 'banred',
    label: 'Banred',
    hint: 'Mediante la aplicación ingresando tu número de estudiante',
  },
  {
    value: 'geopay',
    label: 'Tarjeta de crédito',
    hint: 'Podes seleccionar tu tarjeta de crédito dentro de Sistarbanc',
    badges: ['Mastercard', 'Visa'],
  },
  {
    value: 'abitab',
    label: 'Abitab',
    hint: 'Presencial en red de cobranza presentando tu número de estudiante',
  },
  {
    value: 'paganza',
    label: 'Paganza',
    hint: 'Mediante la aplicación ingresando tu número de estudiante',
  },
];

export const STUDENT_SERVICE_LINKS: readonly StudentServiceLink[] = [
  { label: 'Biblioteca', icon: 'local_library', url: 'https://bibliotecas.ort.edu.uy/' },
  { label: 'Deportes', icon: 'sports_soccer', url: 'https://www.ort.edu.uy/deportes' },
  {
    label: 'Mentorías',
    icon: 'groups',
    url: 'https://www.ort.edu.uy/estudiantes/programa-de-mentorias',
  },
];
