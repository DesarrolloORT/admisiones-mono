import { DocumentRecognitionFileError } from './document-recognition-error';

describe('DocumentRecognitionFileError', () => {
  it('should expose the file error code', () => {
    const error = new DocumentRecognitionFileError('maxFileSize');

    expect(error.code).toBe('maxFileSize');
    expect(error.message).toBe('maxFileSize');
  });
});
