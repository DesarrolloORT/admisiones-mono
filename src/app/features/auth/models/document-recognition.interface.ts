export interface DocumentRecognitionRequest {
  tipoMime: string;
  archivoAdjunto: {
    nombreArchivo: string;
    archivo: string;
  };
}

export interface DocumentRecognitionFields {
  tipoDocumento?: string;
  numeroDocumento?: string;
  primerNombre?: string;
  segundoNombre?: string;
  primerApellido?: string;
  segundoApellido?: string;
  fechaNacimiento?: string;
  lugarNacimiento?: string;
  departamento?: string;
  sexo?: string;
  fechaVencimiento?: string;
  nacionalidad?: string;
  [key: string]: unknown;
}

export interface DocumentRecognitionFile {
  nombreArchivo?: string;
  contentType?: string;
  archivo?: string;
}

export interface DocumentRecognitionData {
  requiereRevision?: boolean;
  campos?: DocumentRecognitionFields;
  caraPersona?: DocumentRecognitionFile;
}

export interface DocumentRecognitionResponse {
  success?: boolean;
  httpCode?: number;
  errorCode?: string;
  method?: string;
  message?: string;
  data?: DocumentRecognitionData;
  [key: string]: unknown;
}
