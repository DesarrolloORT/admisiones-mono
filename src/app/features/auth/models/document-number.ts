export const NATIONAL_ID_DOCUMENT_TYPE = 'CI';

const DOCUMENT_NUMBER_LABELS: Record<string, string> = {
  CI: 'Nro. de cédula',
  PS: 'Nro. de pasaporte',
  DE: 'Nro. de documento extranjero',
};

export function isNationalIdDocumentType(documentType: string): boolean {
  return documentType === NATIONAL_ID_DOCUMENT_TYPE;
}

export function cleanDocumentNumber(documentType: string, documentNumber: string): string {
  const trimmed = documentNumber.trim();

  return isNationalIdDocumentType(documentType) ? trimmed.replaceAll(/\D/g, '') : trimmed;
}

export function formatDocumentForBackend(documentType: string, documentNumber: string): string {
  const cleaned = cleanDocumentNumber(documentType, documentNumber);

  if (!isNationalIdDocumentType(documentType)) {
    return cleaned;
  }

  if (!cleaned) {
    return '';
  }

  return `${cleaned.slice(0, -1)}-${cleaned.slice(-1)}`;
}

export function getDocumentNumberLabel(documentType: string): string {
  return DOCUMENT_NUMBER_LABELS[documentType] ?? 'Nro. de documento';
}
