import type {
  ContactoCoordinador,
  InstruccionReserva,
  MetodoPago,
  OpcionInscripcion,
} from './inscripcion-flow';

export const SECONDARY_STATUS_OPTIONS: readonly OpcionInscripcion[] = [
  { value: 'cursando', label: 'Sí, estoy cursando' },
  { value: 'no-cursando', label: 'No' },
];

export const SECONDARY_PLACE_OPTIONS: readonly OpcionInscripcion[] = [
  { value: 'uruguay', label: 'Uruguay' },
  { value: 'exterior', label: 'En el exterior' },
];

export const WORK_STATUS_OPTIONS: readonly OpcionInscripcion[] = [
  { value: 'trabaja', label: 'Sí, trabajo actualmente' },
  { value: 'buscando', label: 'Estoy buscando trabajo' },
  { value: 'no-trabaja', label: 'No trabajo actualmente' },
];

export const YES_NO_OPTIONS: readonly OpcionInscripcion[] = [
  { value: 'si', label: 'Sí' },
  { value: 'no', label: 'No' },
];

export const DECISION_YEAR_OPTIONS: readonly OpcionInscripcion[] = [
  { value: '1-ems', label: '1º EMS (4º año)' },
  { value: '2-ems', label: '2º EMS (5º año)' },
  { value: '3-ems', label: '3º EMS (6º año)' },
  { value: 'otro', label: 'Otro' },
];

export const CERTAINTY_OPTIONS: readonly OpcionInscripcion[] = [
  { value: 'decidido', label: 'Decidido/a' },
  { value: 'con-dudas', label: 'Con dudas' },
];

export const PAYMENT_OPTIONS: readonly (OpcionInscripcion & { value: MetodoPago })[] = [
  { value: 'cuenta-bancaria', label: 'Cuenta bancaria' },
  { value: 'tarjeta-credito', label: 'Tarjeta de crédito', hint: 'Mastercard y Visa' },
  { value: 'cuenta-personal', label: 'Cuenta personal', hint: 'Monto disponible $70.000,00' },
  { value: 'banred', label: 'Banred' },
  { value: 'abitab', label: 'Abitab' },
  { value: 'paganza', label: 'Paganza' },
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
