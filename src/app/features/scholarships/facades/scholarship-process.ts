import { computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { createProcessFlow } from 'src/app/shared/process-flow/process-flow';

import { createScholarshipPersonalForms } from '../models/scholarship-personal-forms';
import { createScholarshipApplicationForm, SCHOLARSHIP_STEPS } from '../models/scholarship-process';

/**
 * Fachada del proceso de postulación a becas. Es el único objeto con scope de
 * página; se provee en `ScholarshipProcess` para que cada postulación tenga el
 * suyo.
 *
 * Responsabilidades:
 * - Ser dueña del estado del proceso (`flow`) y de **todos los formularios**.
 *   Los forms viven acá, y no en cada paso, porque los step components se
 *   destruyen al navegar entre pasos: si fueran suyos, volver atrás perdería lo
 *   cargado. No hay capa `store/` intermedia: los `FormGroup` se arman con las
 *   factories de `models/`.
 * - Exponer al template lo que el `app-process-layout` necesita: el paso actual
 *   (`currentStep`), los items del stepper (`stepItems`), el subtítulo
 *   (`stepLabel`) y si se puede volver (`canGoBack`).
 *
 * **Quién avanza el flujo:** la fachada de sección del paso activo. Valida lo
 * suyo y, si está OK, llama a `continue()`. Esta fachada no conoce a las de
 * sección — no puede: cada step se las provee a sí mismo, así que solo existen
 * mientras ese paso está montado.
 */
export class ScholarshipProcessFacade {
  private readonly flow = createProcessFlow(SCHOLARSHIP_STEPS, 'application-info');

  /** Paso 1: la inscripción elegida y el período de evaluación. */
  public readonly applicationForm = createScholarshipApplicationForm();

  /** Paso 2: el acordeón de información personal, una sección por `FormGroup`. */
  private readonly personalForms = createScholarshipPersonalForms();

  public readonly personalDataForm = this.personalForms.personalDataForm;
  public readonly educationInfoForm = this.personalForms.educationInfoForm;
  public readonly educationInfoFbrForm = this.personalForms.educationInfoFbrForm;
  public readonly educationInfoFclForm = this.personalForms.educationInfoFclForm;
  public readonly workHistoryForm = this.personalForms.workHistoryForm;
  public readonly declarationForm = this.personalForms.declarationForm;

  /**
   * Modo de postulación elegido en el paso 1. La página lo usa para resolver la
   * variante de `fexa`, que no la define la ruta sino el propio formulario.
   */
  public readonly applicationMode = toSignal(
    this.applicationForm.controls.inscription.controls.applicationMode.valueChanges,
    { initialValue: this.applicationForm.controls.inscription.controls.applicationMode.value }
  );

  public readonly currentStep = this.flow.currentStep;
  public readonly stepItems = computed(() => [...this.flow.stepItems()]);
  public readonly canGoBack = this.flow.canGoBack;

  public readonly stepNumber = computed(() => this.flow.currentIndex() + 1);
  public readonly stepLabel = computed(() => {
    const step = this.stepItems()[this.flow.currentIndex()];
    return `Paso ${this.stepNumber()} de ${this.stepItems().length} - ${step.title}`;
  });

  /**
   * Avanza al siguiente paso. La validación ya la hizo la fachada de sección que
   * llama acá; en el último paso `next()` no hace nada.
   */
  public continue(): void {
    this.flow.next();
  }

  /** Retrocede un paso si es posible. */
  public back(): void {
    if (this.canGoBack()) this.flow.previous();
  }
}
