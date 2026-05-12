import {
  DocumentRecognitionFileError,
  DocumentRecognitionRequestError,
} from './document-recognition-error';

describe('DocumentRecognitionFileError', () => {
  it('should expose the file error code', () => {
    const error = new DocumentRecognitionFileError('maxFileSize');

    expect(error.code).toBe('maxFileSize');
    expect(error.message).toBe('maxFileSize');
  });
});

describe('DocumentRecognitionRequestError', () => {
  it('should expose the request status', () => {
    const error = new DocumentRecognitionRequestError(500);

    expect(error.status).toBe(500);
    expect(error.message).toBe('documentRecognition');
  });
});
