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
  sexo?: string | null;
}

export interface DocumentRecognitionData {
  campos?: DocumentRecognitionFields;
}
