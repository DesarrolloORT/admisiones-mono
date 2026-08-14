import type { Bank } from '../../catalogs/models/catalog.interface';
import type { EnrollmentOption } from './enrollment-flow';

/** Carpeta servida por angular.json desde @desarrolloort/fdp-components/assets/icons/banks. */
const BANK_LOGO_BASE_PATH = 'assets/banks';

/** Logos de banco disponibles en el design system. */
const KNOWN_BANK_LOGOS = [
  'bandes',
  'bbva',
  'brou',
  'heritage',
  'hsbc',
  'itau',
  'la_nacion',
  'midinero',
  'santander',
  'scotiabank',
] as const;

const DEFAULT_BANK_LOGO = 'default';

const FALLBACK_BANKS: readonly Bank[] = [
  { id: 'brou', label: 'BROU', code: 'brou' },
  { id: 'santander', label: 'Santander', code: 'santander' },
  { id: 'itau', label: 'Itaú', code: 'itau' },
  { id: 'bbva', label: 'BBVA', code: 'bbva' },
  { id: 'hsbc', label: 'HSBC', code: 'hsbc' },
  { id: 'scotiabank', label: 'Scotiabank', code: 'scotiabank' },
  { id: 'heritage', label: 'Heritage', code: 'heritage' },
];

export const FALLBACK_BANK_OPTIONS: readonly EnrollmentOption[] = FALLBACK_BANKS.map(toBankOption);

/**
 * Resuelve la ruta del logo de un banco a partir de su código o nombre,
 * cayendo a `default.svg` cuando no hay coincidencia.
 */
export function resolveBankLogo(bank: Pick<Bank, 'label' | 'code'>): string {
  const slug = matchBankLogo(bank.code) ?? matchBankLogo(bank.label) ?? DEFAULT_BANK_LOGO;
  return `${BANK_LOGO_BASE_PATH}/${slug}.svg`;
}

/** Convierte un banco de catálogo en opción de combo con su logo. */
export function toBankOption(bank: Bank): EnrollmentOption {
  return {
    value: bank.code ?? bank.id.toString(),
    label: bank.label,
    icon: resolveBankLogo(bank),
  };
}

export function toBankOptions(banks: readonly Bank[]): readonly EnrollmentOption[] {
  const options = banks.filter(bank => bank.label.trim()).map(toBankOption);
  return options.length > 0 ? options : FALLBACK_BANK_OPTIONS;
}

function matchBankLogo(value: string | null | undefined): string | null {
  const normalized = normalize(value);
  if (!normalized) return null;
  return (
    KNOWN_BANK_LOGOS.find(logo => {
      const logoTokens = logo.split('_');
      return logoTokens.every(token => normalized.includes(token));
    }) ?? null
  );
}

function normalize(value: string | null | undefined): string {
  return (value ?? '')
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');
}
