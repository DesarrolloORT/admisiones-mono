import type {
  ContactoCoordinador,
  InstruccionReserva,
  MetodoPago,
  OpcionInscripcion,
} from './inscripcion-flow';

export const WORK_STATUS_OPTIONS: readonly OpcionInscripcion[] = [
  { value: 'trabaja', label: 'Sí, trabajo actualmente' },
  { value: 'buscando', label: 'Estoy buscando trabajo' },
  { value: 'no-trabaja', label: 'No trabajo actualmente' },
];

export const PAYMENT_OPTIONS: readonly (OpcionInscripcion & {
  value: MetodoPago;
  badges?: readonly string[];
})[] = [
  {
    value: 'cuenta-bancaria',
    label: 'Cuenta bancaria',
    hint: 'Pagá desde tu banco por Sistarbanc',
  },
  {
    value: 'tarjeta-credito',
    label: 'Tarjeta de crédito',
    hint: 'Podrás seleccionar tu tarjeta de crédito dentro de Sistarbanc',
    badges: ['Mastercard', 'Visa'],
  },
  { value: 'cuenta-personal', label: 'Cuenta personal', hint: 'Monto disponible $70.000,00' },
  {
    value: 'banred',
    label: 'Banred',
    hint: 'Mediante la aplicación ingresando tu número de estudiante',
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

export const RESERVATION_INSTRUCTIONS: Record<
  Extract<MetodoPago, 'abitab' | 'paganza' | 'banred'>,
  InstruccionReserva
> = {
  abitab: {
    title: '¡Inscripción reservada!',
    description:
      'Tenés tiempo hasta el 4 de marzo de 2027 para realizar el pago de la seña. Pasada esa fecha, la inscripción se dará de baja automáticamente.',
    items: [
      'Cédula de identidad: documento registrado',
      'Número de estudiante: 397654',
      'Monto a pagar: $ 15.500',
    ],
    help: 'El pago puede demorar hasta 24 horas hábiles en acreditarse en el sistema.',
  },
  paganza: {
    title: '¡Inscripción reservada!',
    description:
      'Tenés tiempo hasta el 4 de marzo de 2027 para realizar el pago de la seña desde Paganza.',
    items: ['Buscá Universidad ORT Uruguay', 'Ingresá tu número de estudiante: 397654'],
    help: 'La acreditación puede demorar hasta 24 horas hábiles.',
  },
  banred: {
    title: '¡Inscripción reservada!',
    description:
      'Tenés tiempo hasta el 4 de marzo de 2027 para realizar el pago de la seña desde Banred.',
    items: ['Ingresá tu número de estudiante: 397654'],
    help: 'La acreditación puede demorar hasta 24 horas hábiles.',
  },
};

export const COORDINATORS: readonly ContactoCoordinador[] = [
  {
    role: 'Coordinador(a) Académico:',
    name: 'María Rodríguez',
    email: 'maria.rodriguez@ort.edu.uy',
  },
  {
    role: 'Coordinador(a) de Cursos:',
    name: 'Carlos Fernández',
    email: 'carlos.fernandez@ort.edu.uy',
  },
];

export const SUBJECTS: readonly string[] = [
  'Arte y estética I',
  'Representación expresiva I',
  'Diseño y comunicación visual I',
  'Fotografía y edición de video',
  'Tipografía I',
  'Historia del diseño',
];
