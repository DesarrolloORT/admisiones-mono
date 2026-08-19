import { computed, inject } from '@angular/core';
import { createProcessFlow } from 'src/app/shared/process-flow/process-flow';

import { SCHOLARSHIP_STEPS } from '../models/scholarship-process';
import { ScholarshipProposalFacade } from './scholarship-proposal';

/**
 * Fachada del proceso de postulación a becas. Es el único objeto que la página
 * `fbr` debería conocer para orquestar el paso a paso. Equivale a
 * `EnrollmentProcessFacade`.
 *
 * Responsabilidades:
 * - Ser dueña del estado del proceso (`flow`). Se provee a nivel de la página,
 *   así que cada postulación tiene el suyo. A medida que el flujo crezca,
 *   agregá acá las señales compartidas entre pasos (p. ej. la respuesta de la
 *   API de postulación).
 * - Exponer al template lo que el `app-process-layout` necesita: el paso actual
 *   (`currentStep`), los items del stepper (`stepItems`), el subtítulo
 *   (`stepLabel`) y si se puede volver (`canGoBack`).
 * - Centralizar la navegación entre pasos en `continue()` / `back()`.
 *
 * Cómo crecerá: cada paso tiene su fachada de sección (validar formularios,
 * llamar a la API, etc.). Esas fachadas se inyectan acá y `continue()` delega
 * en la del paso activo: la sección responde `canContinue()` y **el proceso**
 * avanza el flow. Las secciones nunca llaman a `flow.next()`, y el template
 * nunca avanza el flow: pasá siempre por `continue()`.
 */
export class ScholarshipProcessFacade {
  private readonly flow = createProcessFlow(SCHOLARSHIP_STEPS, 'application-info');

  public readonly proposal = inject(ScholarshipProposalFacade);

  public readonly currentStep = this.flow.currentStep;
  public readonly stepItems = computed(() => [...this.flow.stepItems()]);
  public readonly canGoBack = this.flow.canGoBack;

  public readonly stepNumber = computed(() => this.flow.currentIndex() + 1);
  public readonly stepLabel = computed(() => {
    const step = this.stepItems()[this.flow.currentIndex()];
    return `Paso ${this.stepNumber()} de ${this.stepItems().length} - ${step.title}`;
  });

  /** Avanza al siguiente paso si la sección activa lo permite. */
  public continue(): void {
    switch (this.currentStep()) {
      case 'application-info':
        if (this.proposal.canContinue()) this.flow.next();
        break;
      case 'personal-info':
        this.flow.next();
        break;
      case 'confirmation':
        // Paso terminal: acá se confirmará la postulación.
        break;
    }
  }

  /** Retrocede un paso si es posible. */
  public back(): void {
    if (this.canGoBack()) this.flow.previous();
  }
}
