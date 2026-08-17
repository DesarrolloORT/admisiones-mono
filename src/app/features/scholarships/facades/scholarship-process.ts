import { computed, inject } from '@angular/core';

import { ScholarshipProcessStore } from '../store/scholarship-process';

/**
 * Fachada del proceso de postulación a becas. Es el único objeto que la página
 * `fbr` debería conocer para orquestar el paso a paso. Equivale a
 * `EnrollmentProcessFacade`.
 *
 * Responsabilidades:
 * - Exponer al template lo que el `app-process-layout` necesita: el paso actual
 *   (`currentStep`), los items del stepper (`stepItems`), el subtítulo
 *   (`stepLabel`) y si se puede volver (`canGoBack`).
 * - Centralizar la navegación entre pasos en `continue()` / `back()`.
 *
 * Cómo crecerá: cada paso suele tener su propia fachada de sección (validar
 * formularios, llamar a la API, etc.), tal como inscripciones tiene
 * `EnrollmentProposalFacade`, `EnrollmentSurveyFacade` y
 * `EnrollmentPaymentFacade`. Esas fachadas se inyectan acá y `continue()`
 * delega en la del paso activo: la sección decide si puede avanzar y, cuando
 * corresponde, llama a `this.process.flow.next()`. NO avances el flow desde el
 * template: pasá siempre por `continue()`.
 */
export class ScholarshipProcessFacade {
  private readonly process = inject(ScholarshipProcessStore);

  public readonly currentStep = this.process.flow.currentStep;
  public readonly stepItems = computed(() => [...this.process.flow.stepItems()]);
  public readonly canGoBack = this.process.flow.canGoBack;

  public readonly stepNumber = computed(() => this.process.flow.currentIndex() + 1);
  public readonly stepLabel = computed(() => {
    const step = this.stepItems()[this.process.flow.currentIndex()];
    return `Paso ${this.stepNumber()} de ${this.stepItems().length} - ${step.title}`;
  });

  /**
   * Avanza al siguiente paso. Hoy avanza sin validar; cuando cada paso tenga su
   * fachada de sección, reemplazá cada `case` por una delegación
   * (p. ej. `this.postulation.continue()`) que valide y, si está OK, llame a
   * `this.process.flow.next()`.
   */
  public continue(): void {
    switch (this.currentStep()) {
      case 'application-info':
      case 'personal-info':
        this.process.flow.next();
        break;
      case 'confirmation':
        // Paso terminal: acá se confirmará la postulación.
        break;
    }
  }

  /** Retrocede un paso si es posible. */
  public back(): void {
    if (this.canGoBack()) this.process.flow.previous();
  }
}
