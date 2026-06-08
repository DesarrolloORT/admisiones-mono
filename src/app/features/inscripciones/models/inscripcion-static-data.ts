import { CoordinatorContact, InscripcionOption } from './inscripcion-flow';

export const SECONDARY_STATUS_OPTIONS: readonly InscripcionOption[] = [
  { value: 'cursando', label: 'Sí, estoy cursando' },
  { value: 'no-cursando', label: 'No' },
];

export const SECONDARY_PLACE_OPTIONS: readonly InscripcionOption[] = [
  { value: 'uruguay', label: 'Uruguay' },
  { value: 'exterior', label: 'En el exterior' },
];

export const WORK_STATUS_OPTIONS: readonly InscripcionOption[] = [
  { value: 'trabaja', label: 'Sí, trabajo actualmente' },
  { value: 'buscando', label: 'Estoy buscando trabajo' },
  { value: 'no-trabaja', label: 'No trabajo actualmente' },
];

export const PAYMENT_OPTIONS: readonly InscripcionOption[] = [
  { value: 'cuenta-bancaria', label: 'Cuenta bancaria' },
  { value: 'tarjeta-credito', label: 'Tarjeta de crédito', hint: 'Mastercard y Visa' },
  { value: 'cuenta-personal', label: 'Cuenta personal', hint: 'Monto disponible $70.000,00' },
  { value: 'banred', label: 'Banred' },
  { value: 'abitab', label: 'Abitab' },
  { value: 'paganza', label: 'Paganza' },
];

export const COORDINATORS: readonly CoordinatorContact[] = [
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
