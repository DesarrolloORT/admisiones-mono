/**
 * Tipos propios de la feature para `GET /scholarships/available` y
 * `POST /scholarships/applications`.
 *
 * Los DTO generados (`AvailableScholarshipsResponse`, …) tienen todos los campos
 * opcionales porque el swagger los declara así. Acá no: el adapter colapsa la
 * nullability una sola vez y de esta capa para adentro los campos existen.
 *
 * Archivos **solo de tipos** terminan en `*.interface.ts`: ese sufijo está
 * exento del spec obligatorio en `test-generator.config.json`.
 */

/**
 * Una beca ofrecida por el backend. Ojo: una card **no es un fondo**, es un
 * `NOMBRE_PARA_FRONT` que puede agrupar varios `ID_TIPO_BECA` — por eso
 * `scholarshipTypeIds` es un array. Ver `.api-spec/contracts/becas.contract.json`
 * (`reglasDeDominio.cardPorNombreDeFront`).
 */
export interface AvailableScholarship {
  scholarshipTypeIds: number[];
  name: string;
  description: string;
  /** Si el fondo exige rendir una prueba para postularse. */
  requiresTest: boolean;
}

/** Respuesta completa de `GET /scholarships/available`. */
export interface AvailableScholarships {
  /**
   * `true` ⇒ la persona no tiene una inscripción que la habilite: la lista es el
   * catálogo informativo de fondos vigentes, no becas postulables ya.
   */
  requiresPriorEnrollment: boolean;
  scholarships: AvailableScholarship[];
}

/** Lo que la feature manda para dar de alta una postulación. */
export interface ScholarshipApplicationPayload {
  enrollmentId: number;
  /**
   * Sale de filtrar `tests[]` de `GET /scholarships/application-options` por el
   * `scholarshipTypeId` de la modalidad y el `intakeId` de la inscripción: el
   * front **no** lo calcula.
   */
  testId: number;
}

/** Postulación creada. */
export interface ScholarshipApplication {
  applicationId: number;
  affidavitId: number | null;
  affidavitStatus: string;
}
