export const CEDULA_DOCUMENT_TYPE = 'CI';

const DOCUMENT_NUMBER_LABELS: Record<string, string> = {
  CI: 'Nro. de cédula',
  PS: 'Nro. de pasaporte',
  DE: 'Nro. de documento extranjero',
};

export function isCedulaDocumentType(documentType: string): boolean {
  return documentType === CEDULA_DOCUMENT_TYPE;
}

export function cleanDocumentNumber(documentType: string, documentNumber: string): string {
  const trimmed = documentNumber.trim();

  return isCedulaDocumentType(documentType) ? trimmed.replaceAll(/\D/g, '') : trimmed;
}

export function formatDocumentForBackend(documentType: string, documentNumber: string): string {
  const cleaned = cleanDocumentNumber(documentType, documentNumber);

  if (!isCedulaDocumentType(documentType)) {
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
