import type { Bank } from '../../catalogs/models/catalog.interface';
import type { OpcionInscripcion } from './inscripcion-flow';

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

/**
 * Resuelve la ruta del logo de un banco a partir de su código o nombre,
 * cayendo a `default.svg` cuando no hay coincidencia.
 */
export function resolveBankLogo(bank: Pick<Bank, 'label' | 'code'>): string {
  const slug = matchBankLogo(bank.code) ?? matchBankLogo(bank.label) ?? DEFAULT_BANK_LOGO;
  return `${BANK_LOGO_BASE_PATH}/${slug}.svg`;
}

/** Convierte un banco de catálogo en opción de combo con su logo. */
export function toBankOption(bank: Bank): OpcionInscripcion {
  return {
    value: bank.id.toString(),
    label: bank.label,
    icon: resolveBankLogo(bank),
  };
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
