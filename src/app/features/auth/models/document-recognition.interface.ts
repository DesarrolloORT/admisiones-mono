export interface DocumentRecognitionRequest {
  mimeType: string;
  attachment: {
    fileName: string;
    content: string;
  };
}

export interface DocumentRecognitionFields {
  documentType?: string | null;
  documentNumber?: string | null;
  firstName?: string | null;
  middleName?: string | null;
  firstSurname?: string | null;
  secondSurname?: string | null;
  birthDate?: string | null;
  birthplace?: string | null;
  sex?: string | null;
}

export interface DocumentRecognitionData {
  fields?: DocumentRecognitionFields;
}
