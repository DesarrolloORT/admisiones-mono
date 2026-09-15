export type DocumentRecognitionFileErrorCode = 'invalidMimeType' | 'maxFileSize' | 'readFailed';

export class DocumentRecognitionFileError extends Error {
  public constructor(public readonly code: DocumentRecognitionFileErrorCode) {
    super(code);
  }
}
