export interface DocumentRecognitionRequest {
  tipoMime: string;
  archivoAdjunto: {
    nombreArchivo: string;
    archivo: string;
  };
}

export interface DocumentRecognitionFields {
  tipoDocumento?: string | null;
  numeroDocumento?: string | null;
  primerNombre?: string | null;
  segundoNombre?: string | null;
  primerApellido?: string | null;
  segundoApellido?: string | null;
  fechaNacimiento?: string | null;
  lugarNacimiento?: string | null;
  departamento?: string | null;
  sexo?: string | null;
  fechaVencimiento?: string | null;
  nacionalidad?: string | null;
}

export interface DocumentRecognitionFile {
  nombreArchivo?: string | null;
  contentType?: string | null;
  archivo?: string | null;
}

export interface DocumentRecognitionData {
  requiereRevision?: boolean;
  campos?: DocumentRecognitionFields;
  caraPersona?: DocumentRecognitionFile;
}

export type DocumentRecognitionResponse = DocumentRecognitionData;
