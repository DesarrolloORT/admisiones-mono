import {
  cleanDocumentNumber,
  formatDocumentForBackend,
  getDocumentNumberLabel,
  isCedulaDocumentType,
} from './document-number';

describe('document-number', () => {
  it('should detect cedula document type', () => {
    expect(isCedulaDocumentType('CI')).toBe(true);
    expect(isCedulaDocumentType('PS')).toBe(false);
  });

  it('should clean CI separators but keep non-CI values trimmed', () => {
    expect(cleanDocumentNumber('CI', '1.234.567-8')).toBe('12345678');
    expect(cleanDocumentNumber('PS', ' AB-123 ')).toBe('AB-123');
  });

  it('should format CI with backend hyphen and leave non-CI documents unchanged', () => {
    expect(formatDocumentForBackend('CI', '1.234.567-8')).toBe('1234567-8');
    expect(formatDocumentForBackend('DE', 'A-123')).toBe('A-123');
  });

  it('should expose labels for known document types', () => {
    expect(getDocumentNumberLabel('CI')).toBe('Nro. de cédula');
    expect(getDocumentNumberLabel('PS')).toBe('Nro. de pasaporte');
    expect(getDocumentNumberLabel('unknown')).toBe('Nro. de documento');
  });
});
