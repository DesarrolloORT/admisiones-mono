/**
 * Estado de una sección de acordeón para mostrar el icono de "completo" y el
 * mensaje "Información pendiente". Compartido entre las fachadas de sección
 * (`ScholarshipProposalFacade`, `ScholarshipPersonalFacade`) para que cada una
 * no reimplemente la misma lógica de icono/error.
 */
export interface SectionStatus {
  isComplete(): boolean;
  hasError(): boolean;
}

export function createSectionStatus(
  isValid: () => boolean,
  submitted: () => boolean
): SectionStatus {
  return {
    isComplete: () => isValid(),
    hasError: () => submitted() && !isValid(),
  };
}
