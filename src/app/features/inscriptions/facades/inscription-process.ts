import { computed, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { merge } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { detailToPreEnrollment, type InscripcionDetail } from '../models/inscription-detail';
import type { BorradorInscripcion, EscenarioInscripcion } from '../models/inscription-flow';
import { getSurveyValues, parseDate, serializeDate } from '../models/inscription-flow-mappers';
import { InscripcionDraft } from '../services/inscription-draft';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionPaymentFacade } from './inscription-payment';
import { InscripcionProposalFacade } from './inscription-proposal';
import { InscripcionSurveyFacade } from './inscription-survey';

const DRAFT_SCENARIOS: readonly EscenarioInscripcion[] = [
  'primera-vez',
  'parcial',
  'encuesta-completa',
];

export class InscripcionProcessFacade {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly forms = inject(InscripcionFormsStore);
  private readonly process = inject(InscripcionProcessStore);
  private readonly draft = inject(InscripcionDraft);
  private draftTimer: ReturnType<typeof setTimeout> | null = null;
  private draftRestored = false;
  private lastCheckpoint = 0;
  private readonly draftReady = signal(false);

  public readonly proposal = inject(InscripcionProposalFacade);
  public readonly survey = inject(InscripcionSurveyFacade);
  public readonly payment = inject(InscripcionPaymentFacade);

  public readonly currentStep = this.process.flow.currentStep;
  public readonly stepItems = computed(() => [...this.process.flow.stepItems()]);
  public readonly stepNumber = computed(() => this.process.flow.currentIndex() + 1);
  public readonly stepLabel = computed(() => {
    const step = this.stepItems()[this.process.flow.currentIndex()];
    return `Paso ${this.stepNumber()} de ${this.stepItems().length} - ${step.title}`;
  });
  public readonly backLabel = computed(() => {
    if (this.currentStep() === 'pago') return 'Volver al paso 2';
    if (this.currentStep() !== 'encuesta') return 'Volver al paso anterior';
    if (this.survey.readerOpen()) return 'Volver a Reglamento estudiantil';

    const sections = this.survey.visibleSections();
    return sections.indexOf(this.survey.activeSection()) > 0
      ? 'Volver a la sección anterior'
      : 'Volver al paso 1';
  });
  public readonly exitConfirmationOpen = signal(false);
  public readonly surveySaveError = signal<string | null>(null);
  public readonly savingSurvey = signal(false);
  public readonly catalogError = computed(
    () => this.proposal.catalogError() ?? this.survey.catalogError()
  );
  public readonly showStepper = computed(
    () =>
      !this.survey.loadingSurveyState() &&
      !this.survey.surveyLoadError() &&
      !this.payment.outcome() &&
      this.payment.view() !== 'processing'
  );
  public readonly canGoBack = computed(() => {
    if (this.payment.outcome() || this.payment.view() === 'processing') return false;
    if (this.currentStep() === 'propuesta') return false;
    if (this.currentStep() === 'pago') return this.payment.view() === 'editing';
    if (this.survey.readerOpen()) return true;
    return (
      this.survey.visibleSections().indexOf(this.survey.activeSection()) > 0 ||
      this.process.flow.canGoBack()
    );
  });

  constructor() {
    this.restoreDraftWhenReady();
    this.observeDraftChanges();
    this.clearDraftAtTerminalOutcome();
    this.destroyRef.onDestroy(() => {
      if (this.draftTimer) clearTimeout(this.draftTimer);
    });
  }

  public continue(): void {
    switch (this.currentStep()) {
      case 'propuesta':
        this.proposal.continue();
        break;
      case 'encuesta':
        this.survey.continue();
        break;
      case 'pago':
        this.payment.requestConfirmation();
        break;
    }
  }

  public back(): void {
    if (!this.canGoBack()) return;
    if (this.currentStep() === 'encuesta') {
      this.survey.back();
      return;
    }
    if (this.currentStep() === 'pago') this.process.flow.previous();
  }

  public requestExit(): void {
    this.surveySaveError.set(null);
    this.flushDraft();
    this.exitConfirmationOpen.set(true);
  }

  public cancelExit(): void {
    this.surveySaveError.set(null);
    this.exitConfirmationOpen.set(false);
  }

  public confirmExit(): void {
    if (this.savingSurvey()) return;
    this.flushDraft();

    if (!this.survey.hasInitialSurveyRight()) {
      this.exitConfirmationOpen.set(false);
      void this.router.navigateByUrl('/inicio');
      return;
    }

    this.surveySaveError.set(null);
    this.savingSurvey.set(true);
    this.survey
      .savePartial()
      .pipe(
        finalize(() => this.savingSurvey.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: saved => {
          if (!saved) {
            this.surveySaveError.set('No se pudo guardar la encuesta. Intentá nuevamente.');
            return;
          }
          this.exitConfirmationOpen.set(false);
          void this.router.navigateByUrl('/inicio');
        },
        error: () =>
          this.surveySaveError.set('No se pudo guardar la encuesta. Intentá nuevamente.'),
      });
  }

  private restoreDraftWhenReady(): void {
    effect(() => {
      if (!this.proposal.initialized() || !this.survey.initialized() || this.draftRestored) return;
      untracked(() => this.restoreDraft());
    });
  }

  private restoreDraft(): void {
    this.draftRestored = true;
    const saved = this.draft.load(this.survey.scenario());
    if (saved) {
      this.forms.academicForm.patchValue(saved.propuesta, { emitEvent: false });
      this.proposal.setProposalType(saved.propuesta.tipoPropuesta);
      this.forms.educationForm.patchValue(saved.encuesta.educacion, { emitEvent: false });
      this.forms.academicDecisionForm.patchValue(saved.encuesta.decisionAcademica, {
        emitEvent: false,
      });
      this.forms.ortExperienceForm.patchValue(saved.encuesta.experienciaOrt, { emitEvent: false });
      this.forms.workForm.patchValue(saved.encuesta.situacionLaboral, { emitEvent: false });
      this.forms.identityForm.controls.vencimientoDocumento.setValue(
        parseDate(saved.identidad.vencimientoDocumento),
        { emitEvent: false }
      );
      this.forms.regulationForm.patchValue(saved.reglamento, { emitEvent: false });
      this.survey.restoreSectionState(saved.seccionActiva, saved.seccionesCompletas);
      this.payment.restore(saved.pago.metodoPago, saved.preinscription);
      this.process.flow.goTo(
        saved.paso === 'pago' && !saved.preinscription ? 'encuesta' : saved.paso
      );
    }
    this.applyResumeContext();
    this.draftReady.set(true);
  }

  // Si la inscripción se retoma desde el panel con un detalle resuelto, reconstruye
  // el contexto y posiciona el flujo en el paso pendiente: "Pago pendiente" precarga
  // seña/vencimiento/resumen y salta al paso de pago; "Confirmada" precarga el
  // resumen y muestra el success step terminal. El resto sigue el flujo normal ya
  // configurado por la encuesta.
  private applyResumeContext(): void {
    const detail = this.route.snapshot.data['inscriptionDetail'] as InscripcionDetail | null;
    if (detail?.estado !== 'Pago pendiente' && detail?.estado !== 'Confirmada') return;

    const preEnrollment = detailToPreEnrollment(detail);
    if (preEnrollment) this.process.preEnrollmentResponse.set(preEnrollment);

    if (detail.estado === 'Pago pendiente') this.process.flow.goTo('pago');
    else this.payment.outcome.set('inscription-confirmada');
  }

  private observeDraftChanges(): void {
    merge(
      this.forms.academicForm.valueChanges,
      this.forms.educationForm.valueChanges,
      this.forms.academicDecisionForm.valueChanges,
      this.forms.ortExperienceForm.valueChanges,
      this.forms.workForm.valueChanges,
      this.forms.identityForm.valueChanges,
      this.forms.regulationForm.valueChanges,
      this.forms.paymentForm.valueChanges
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.scheduleDraftSave());

    effect(() => {
      const ready = this.draftReady();
      this.currentStep();
      this.survey.activeSection();
      this.survey.completedSectionIds();
      this.process.preEnrollmentResponse();
      const checkpoint = this.process.checkpoint();
      if (!ready || this.payment.outcome()) return;

      if (checkpoint > this.lastCheckpoint) {
        this.lastCheckpoint = checkpoint;
        untracked(() => this.flushDraft());
      } else {
        untracked(() => this.scheduleDraftSave());
      }
    });
  }

  private scheduleDraftSave(): void {
    if (!this.draftReady() || this.payment.outcome()) return;
    if (this.draftTimer) clearTimeout(this.draftTimer);
    this.draftTimer = setTimeout(() => {
      this.draftTimer = null;
      this.saveDraft();
    }, 300);
  }

  private flushDraft(): void {
    if (!this.draftReady() || this.payment.outcome()) return;
    if (this.draftTimer) {
      clearTimeout(this.draftTimer);
      this.draftTimer = null;
    }
    this.saveDraft();
  }

  private saveDraft(): void {
    const scenario = this.survey.scenario();
    for (const candidate of DRAFT_SCENARIOS) {
      if (candidate !== scenario) this.draft.clear(candidate);
    }
    this.draft.save(this.buildDraft(scenario));
  }

  private buildDraft(scenario: EscenarioInscripcion): BorradorInscripcion {
    return {
      version: 2,
      escenario: scenario,
      paso: this.currentStep(),
      seccionActiva: this.survey.activeSection(),
      seccionesCompletas: [...this.survey.completedSectionIds()],
      propuesta: this.forms.academicForm.getRawValue(),
      encuesta: getSurveyValues(this.forms.forms),
      identidad: {
        vencimientoDocumento: serializeDate(
          this.forms.identityForm.controls.vencimientoDocumento.value
        ),
      },
      reglamento: this.forms.regulationForm.getRawValue(),
      pago: this.forms.paymentForm.getRawValue(),
      preinscription: this.process.preEnrollmentResponse(),
    };
  }

  private clearDraftAtTerminalOutcome(): void {
    effect(() => {
      if (!this.payment.outcome()) return;
      untracked(() => {
        if (this.draftTimer) {
          clearTimeout(this.draftTimer);
          this.draftTimer = null;
        }
        for (const scenario of DRAFT_SCENARIOS) this.draft.clear(scenario);
      });
    });
  }
}
