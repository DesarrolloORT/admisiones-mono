import { computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, ValidatorFn, Validators } from '@angular/forms';
import type { OrtErrorItem } from '@desarrolloort/components';
import { merge, Observable, of } from 'rxjs';
import { catchError, finalize, switchMap } from 'rxjs/operators';
import {
  DEFAULT_ERROR_ALERT,
  type ErrorAlertState,
} from 'src/app/shared/ui/error-alert/error-alert';

import type { EnrollmentSurveyInit } from '../models/enrollment-entry';
import type {
  EnrollmentInitialSurvey,
  EnrollmentInitialSurveyResponse,
  EnrollmentStudentRegulationAcceptance,
  InitialSurveyStatus,
  SurveySectionId,
  SurveySectionStatus,
} from '../models/enrollment-flow';
import {
  buildFormErrors,
  disallowedHighSchoolYearForUniversity,
  type IdentityFileTarget,
  UNIVERSITY_LEVEL,
} from '../models/enrollment-flow-forms';
import {
  buildConfirmPreEnrollmentPayload,
  buildInitialSurveyPayload,
  hasCompleteUniversityEducation,
  parseDate,
  patchBackendSurveyForms,
} from '../models/enrollment-flow-mappers';
import { getVisibleSections } from '../models/enrollment-flow-policy';
import {
  type EnrollmentInitialSurveyResolved,
  resolveInitialSurvey,
} from '../resolvers/enrollment-initial-survey.resolver';
import { Enrollments } from '../services/enrollments';
import { EnrollmentFormsStore } from '../store/enrollment-forms';
import { EnrollmentProcessStore } from '../store/enrollment-process';
import { EnrollmentPaymentFacade } from './enrollment-payment';
import { EnrollmentProposalFacade } from './enrollment-proposal';
import { EnrollmentSurveyIdentityFacade } from './enrollment-survey-identity';
import { EnrollmentSurveyOptionsFacade } from './enrollment-survey-options';

const IDENTITY_SAVE_ERROR = 'identity-save';

interface SectionProgress {
  completed: boolean;
  submitted: boolean;
}

const EMPTY_PROGRESS: SectionProgress = { completed: false, submitted: false };

/**
 * Orquestador del paso 2 (encuesta inicial + identidad + reglamento): estado de
 * secciones, validadores condicionales y cierre del paso. Los catálogos viven en
 * `EnrollmentSurveyOptionsFacade` (`options`) y la verificación de identidad en
 * `EnrollmentSurveyIdentityFacade` (`identity`).
 */
export class EnrollmentSurveyFacade {
  private readonly enrollments = inject(Enrollments);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formsStore = inject(EnrollmentFormsStore);
  private readonly process = inject(EnrollmentProcessStore);
  private readonly payment = inject(EnrollmentPaymentFacade);
  private readonly proposal = inject(EnrollmentProposalFacade);

  public readonly options = inject(EnrollmentSurveyOptionsFacade);
  public readonly identity = inject(EnrollmentSurveyIdentityFacade);

  // Slice de encuesta prellenada ya aplicado:
  // se conserva para re-aplicarlo cuando llegan los catálogos, respetando su flag
  // `includeAcademicSelection` original.
  private appliedSurveyState: Extract<EnrollmentSurveyInit, { kind: 'prefilled' }> | null = null;

  public readonly educationForm = this.formsStore.educationForm;
  public readonly academicDecisionForm = this.formsStore.academicDecisionForm;
  public readonly ortExperienceForm = this.formsStore.ortExperienceForm;
  public readonly workForm = this.formsStore.workForm;
  public readonly identityForm = this.formsStore.identityForm;
  public readonly regulationForm = this.formsStore.regulationForm;
  private readonly sectionConfig = this.formsStore.sectionConfig;

  // Estado por sección en un único registro; `getSectionState` es el contrato
  // que consume el template.
  private readonly sectionProgress = signal<
    Readonly<Partial<Record<SurveySectionId, SectionProgress>>>
  >({});
  // Suprime la completitud reactiva de identidad tras un fallo de subida: el form
  // sigue válido y los archivos presentes, pero el paso debe reabrirse SIN check
  // hasta que el usuario modifique datos de identidad o arranque un nuevo intento.
  private readonly identityUploadFailed = signal(false);
  public readonly activeSection = signal<SurveySectionId>('education');
  public readonly readerOpen = signal(false);
  public readonly surveyState = signal<InitialSurveyStatus>('not-started');
  public readonly hasInitialSurveyRight = signal(true);
  public readonly hasAcceptedStudentRegulation = signal(false);
  public readonly submittedAcceptanceDate = signal<Date | null>(null);

  public readonly catalogError = this.options.catalogError;
  public readonly loadingInitialSurveyCatalogs = this.options.loadingInitialSurveyCatalogs;
  public readonly initialized = this.options.initialized;
  public readonly surveyLoadError = signal<string | null>(null);
  public readonly preEnrollmentError = signal<string | null>(null);
  public readonly loadingSurveyState = signal(false);
  public readonly finalizingPreEnrollment = signal(false);

  public readonly scenario = computed(() =>
    !this.hasInitialSurveyRight() || this.surveyState() === 'complete'
      ? 'survey-complete'
      : this.surveyState() === 'in-progress'
        ? 'partial'
        : 'first-time'
  );
  public readonly isProfessionalUpdate = this.proposal.selection.isProfessionalUpdate;
  public readonly visibleSections = computed(() =>
    getVisibleSections(this.scenario(), this.isProfessionalUpdate())
  );
  // Único predicado de "hay algo hacia atrás" del paso 2: lo consumen el botón del
  // footer y el `canGoBack` del ProcessFacade (chevron del header).
  public readonly canGoBack = computed(
    () => this.readerOpen() || this.visibleSections().indexOf(this.activeSection()) > 0
  );
  public readonly sectionItems = computed(() =>
    this.visibleSections().map(section => ({
      id: section,
      label: this.sectionConfig[section].label,
      icon: this.sectionConfig[section].icon,
      state: this.getSectionState(section),
    }))
  );
  public readonly activeSectionErrors = computed<OrtErrorItem[]>(() => {
    const section = this.activeSection();
    if (!this.progressOf(section).submitted) return [];
    const formErrors = buildFormErrors(
      this.sectionConfig[section].form,
      this.sectionConfig[section].errorFields
    );
    if (section !== 'identity') return formErrors;

    const files = this.identity.identityFiles();
    return [
      ...formErrors,
      ...(!files.front ? [{ message: 'Frente del documento es obligatorio.' }] : []),
      ...(!files.back ? [{ message: 'Dorso del documento es obligatorio.' }] : []),
      ...(!files.selfie ? [{ message: 'Foto del rostro es obligatoria.' }] : []),
    ];
  });
  public readonly activeSectionErrorAlert = computed<ErrorAlertState | null>(() =>
    this.activeSectionErrors().length > 0 ? DEFAULT_ERROR_ALERT : null
  );

  private regulationAcceptanceRequested = false;

  constructor() {
    this.options.initialize({
      isSurveyStepActive: computed(() => this.process.flow.currentStep() === 'survey'),
      onOptionsChanged: () => this.updateConditionalValidators(),
      onInitialCatalogsApplied: () => this.reapplyBackendSurvey(),
    });
    this.identity.initialize({
      isIdentitySectionActive: computed(
        () => this.process.flow.currentStep() === 'survey' && this.activeSection() === 'identity'
      ),
      surveyLoadError: this.surveyLoadError,
      onIdentityChanged: () => {
        this.identityUploadFailed.set(false);
        this.syncSectionCompletion('identity');
      },
    });
    this.configureConditionalValidators();
    this.observeIdentityRecovery();
    this.observeForms();
    this.observeIdentityConfirmation();
    this.deferStudentRegulationAcceptance();
    let previousFirstVisibleSection = this.visibleSections()[0];
    effect(() => {
      const sections = this.visibleSections();
      if (!sections.includes(this.activeSection()) || sections[0] !== previousFirstVisibleSection) {
        this.activeSection.set(sections[0]);
      }
      previousFirstVisibleSection = sections[0];
    });
    // El posicionamiento del flujo y la aplicación del estado inicial los hace
    // `EnrollmentProcessFacade` (único inicializador) vía `applyInitialState`.
  }

  /**
   * Aplica el slice de encuesta derivado por `deriveInitialEnrollmentState`. NO
   * toca `process.flow`: el paso lo posiciona `ProcessFacade` una sola vez.
   */
  public applyInitialState(state: EnrollmentSurveyInit): void {
    this.identityUploadFailed.set(false);
    switch (state.kind) {
      case 'load-failed':
        this.appliedSurveyState = null;
        this.surveyLoadError.set(
          'No se pudo consultar el estado de tu encuesta. Intentá nuevamente.'
        );
        return;
      case 'identity-only':
        this.appliedSurveyState = null;
        this.hasInitialSurveyRight.set(false);
        this.surveyState.set('complete');
        this.sectionProgress.set({});
        this.activeSection.set('identity');
        return;
      case 'fresh':
        this.appliedSurveyState = null;
        this.hasInitialSurveyRight.set(true);
        this.surveyState.set('not-started');
        this.sectionProgress.set({});
        this.activeSection.set('education');
        return;
      case 'prefilled': {
        const survey = state.response.survey;
        if (!survey) return;
        this.appliedSurveyState = state;
        this.hasInitialSurveyRight.set(true);
        this.surveyState.set(state.surveyState);
        this.applyBackendSurvey(survey, state.response, state.includeAcademicSelection);
        this.sectionProgress.set(
          Object.fromEntries(
            state.completedSections.map(section => [section, { completed: true, submitted: false }])
          )
        );
        this.activeSection.set(state.activeSection);
        return;
      }
    }
  }

  /**
   * Re-consulta el estado de encuesta (para el retry manual). Gestiona los flags de
   * carga/error; el mapeo del 404 lo comparte con el resolver. `ProcessFacade`
   * re-deriva y re-aplica el estado con el resultado.
   */
  public fetchResolvedInitialSurvey(): Observable<EnrollmentInitialSurveyResolved> {
    this.surveyLoadError.set(null);
    this.loadingSurveyState.set(true);
    return resolveInitialSurvey(this.enrollments.getInitialSurvey()).pipe(
      finalize(() => this.loadingSurveyState.set(false))
    );
  }

  public continue(): void {
    if (this.finalizingPreEnrollment()) return;

    const section = this.activeSection();
    this.patchProgress(section, { submitted: true });
    if (!this.isSectionValid(section)) {
      this.sectionConfig[section].form.markAllAsTouched();
      return;
    }

    this.patchProgress(section, { completed: true });
    const nextSection = this.findNextInvalidSection(section);
    if (nextSection) {
      this.activeSection.set(nextSection);
      return;
    }
    this.finishSurveyStep();
  }

  // Retroceder dentro del paso 2: nunca sale del paso. En la primera sección visible no
  // hay a dónde volver (el flujo no permite regresar al paso 1).
  public back(): void {
    if (this.readerOpen()) {
      this.readerOpen.set(false);
      return;
    }

    const sections = this.visibleSections();
    const currentIndex = sections.indexOf(this.activeSection());
    if (currentIndex > 0) this.activeSection.set(sections[currentIndex - 1]);
  }

  public openSection(section: SurveySectionId): void {
    if (this.visibleSections().includes(section)) this.activeSection.set(section);
  }

  public getSectionState(section: SurveySectionId): SurveySectionStatus {
    if (this.progressOf(section).completed || this.canSectionAutoComplete(section)) {
      return 'complete';
    }
    if (this.activeSection() === section) return 'active';
    return 'pending';
  }

  // Una sección válida se marca completa sola (sin apretar Continuar). Identidad es
  // la excepción tras un fallo de subida: sigue válida pero no debe auto-completarse.
  private canSectionAutoComplete(section: SurveySectionId): boolean {
    return (section !== 'identity' || !this.identityUploadFailed()) && this.isSectionValid(section);
  }

  public isSectionPending(section: SurveySectionId): boolean {
    return this.progressOf(section).submitted && !this.isSectionValid(section);
  }

  public isIdentityFileMissing(target: IdentityFileTarget): boolean {
    return this.progressOf('identity').submitted && !this.identity.identityFiles()[target];
  }

  public isUniversityCareer(): boolean {
    const selectedCareer = this.formsStore.academicForm.controls.degreeProgram.value;
    if (!selectedCareer) return false;
    const level = this.proposal
      .careers()
      .find(career => career.productId.toString() === selectedCareer)?.productLevelId;
    return level === UNIVERSITY_LEVEL;
  }

  public isNationalSchoolPlace(): boolean {
    return this.educationForm.controls.highSchoolLocation.value === '1';
  }

  public isForeignSchoolPlace(): boolean {
    return this.educationForm.controls.highSchoolLocation.value === '2';
  }

  public shouldAskHighSchoolOrientation(): boolean {
    const selectedYear = this.educationForm.controls.highSchoolYear.value;
    return (
      this.educationForm.controls.studiesHighSchool.value === 'studying' &&
      !!selectedYear &&
      this.options.orientationOptions().length > 0
    );
  }

  public shouldAskHighSchoolRepeatCount(): boolean {
    return this.educationForm.controls.repeatsHighSchoolYear.value === 'yes';
  }

  public shouldAskHigherEducationUniversities(): boolean {
    return this.educationForm.controls.higherEducationStatus.value === '1';
  }

  public shouldAskHigherEducationOtherUniversity(): boolean {
    return (
      this.shouldAskHigherEducationUniversities() &&
      this.educationForm.controls.higherEducationUniversities.value.includes('0')
    );
  }

  public shouldAskInformedOtherUniversity(): boolean {
    return (
      this.academicDecisionForm.controls.otherUniversities.value === 'yes' &&
      this.academicDecisionForm.controls.researchedUniversities.value.includes('0')
    );
  }

  public shouldAskMotherOrtDegree(): boolean {
    return hasCompleteUniversityEducation(this.educationForm.controls.motherEducation.value);
  }

  public shouldAskFatherOrtDegree(): boolean {
    return hasCompleteUniversityEducation(this.educationForm.controls.fatherEducation.value);
  }

  public openRegulationReader(): void {
    this.readerOpen.set(true);
  }

  public acceptRegulation(): void {
    this.regulationForm.controls.acceptsRegulation.setValue(true);
    this.patchProgress('regulation', { completed: true });
    this.activeSection.set('regulation');
    this.readerOpen.set(false);
  }

  public savePartial(): Observable<boolean> {
    if (
      !this.hasInitialSurveyRight() ||
      this.isProfessionalUpdate() ||
      this.process.preEnrollmentResponse() !== null
    )
      return of(true);
    return this.enrollments.saveInitialSurvey(buildInitialSurveyPayload(this.formsStore.forms));
  }

  private progressOf(section: SurveySectionId): SectionProgress {
    return this.sectionProgress()[section] ?? EMPTY_PROGRESS;
  }

  private patchProgress(section: SurveySectionId, patch: Partial<SectionProgress>): void {
    this.sectionProgress.update(progress => ({
      ...progress,
      [section]: { ...(progress[section] ?? EMPTY_PROGRESS), ...patch },
    }));
  }

  private finishSurveyStep(): void {
    if (!this.ensureAllVisibleSectionsValid()) return;

    const confirmPayload = buildConfirmPreEnrollmentPayload(
      this.formsStore.forms,
      this.isProfessionalUpdate()
    );
    if (!confirmPayload) {
      this.preEnrollmentError.set(
        'No se pudo confirmar la preinscripción con la oferta seleccionada.'
      );
      return;
    }

    this.preEnrollmentError.set(null);
    // Un intento nuevo supersede el fallo anterior: "Continuar" sin modificar reintenta.
    this.identityUploadFailed.set(false);
    this.finalizingPreEnrollment.set(true);
    this.identity
      .saveIdentityChanges()
      .pipe(
        catchError(() => of(false)),
        switchMap(savedIdentity => {
          if (!savedIdentity) throw new Error(IDENTITY_SAVE_ERROR);
          return this.savePartial();
        }),
        switchMap(saved => {
          if (!saved) throw new Error('No se pudo guardar la encuesta inicial final.');
          return this.enrollments.confirmPreEnrollment(confirmPayload);
        }),
        finalize(() => this.finalizingPreEnrollment.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: response => {
          this.process.preEnrollmentResponse.set(response);
          this.surveyState.set('complete');
          // Seña 0: no hay nada que pagar; la pantalla terminal explica cómo continuar.
          if (response.enrollmentDeposit === 0) {
            this.payment.outcome.set('reservation');
            return;
          }
          if (confirmPayload.isCorporateEnrollment) {
            this.payment.outcome.set('enrollment-in-progress');
            return;
          }
          if (response.isWaiting === true) {
            this.payment.outcome.set('enrollment-in-progress');
            return;
          }
          this.process.flow.next();
        },
        error: error => {
          const identitySaveFailed =
            error instanceof Error && error.message === IDENTITY_SAVE_ERROR;
          if (identitySaveFailed) {
            this.identityUploadFailed.set(true);
            this.patchProgress('identity', { completed: false, submitted: true });
            this.activeSection.set('identity');
            this.identityForm.markAllAsTouched();
          }
          this.preEnrollmentError.set(
            identitySaveFailed
              ? 'No se pudo guardar la verificación de identidad. Intentá nuevamente.'
              : 'No se pudo guardar y confirmar la preinscripción. Intentá nuevamente.'
          );
        },
      });
  }

  private findNextInvalidSection(section: SurveySectionId): SurveySectionId | undefined {
    const sections = this.visibleSections();
    return sections.slice(sections.indexOf(section) + 1).find(next => !this.isSectionValid(next));
  }

  private ensureAllVisibleSectionsValid(): boolean {
    const invalidSection = this.visibleSections().find(section => !this.isSectionValid(section));
    if (!invalidSection) return true;

    this.patchProgress(invalidSection, { submitted: true });
    this.activeSection.set(invalidSection);
    this.sectionConfig[invalidSection].form.markAllAsTouched();
    this.preEnrollmentError.set(
      'Completá la información pendiente antes de confirmar la preinscripción.'
    );
    return false;
  }

  private syncSectionCompletion(section: SurveySectionId): void {
    if (this.canSectionAutoComplete(section)) {
      this.patchProgress(section, { completed: true });
      return;
    }
    if (!this.isSectionValid(section)) {
      this.patchProgress(section, { completed: false });
    }
  }

  private isSectionValid(section: SurveySectionId): boolean {
    if (section !== 'identity') return this.sectionConfig[section].form.valid;
    return this.identity.isComplete();
  }

  private observeForms(): void {
    merge(
      this.educationForm.valueChanges,
      this.academicDecisionForm.valueChanges,
      this.ortExperienceForm.valueChanges,
      this.workForm.valueChanges,
      this.identityForm.valueChanges,
      this.regulationForm.valueChanges
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.preEnrollmentError.set(null);
        for (const section of this.visibleSections()) this.syncSectionCompletion(section);
      });
  }

  // Debe suscribirse ANTES que `observeForms`: los subscribers de un mismo
  // `valueChanges` corren en orden de suscripción, así la re-sincronización de
  // completitud ve el flag ya limpio en el mismo tick que el cambio de identidad.
  private observeIdentityRecovery(): void {
    this.identityForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.identityUploadFailed.set(false));
  }

  private observeIdentityConfirmation(): void {
    this.identityForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      const confirmed = this.identityForm.controls.isIdentityCorrect.value;
      if (
        !confirmed ||
        !this.identity.requiresIdentityConfirmation() ||
        !this.isSectionValid('identity')
      ) {
        return;
      }

      this.patchProgress('identity', { completed: true });
      if (this.activeSection() !== 'identity') return;

      const sections = this.visibleSections();
      const nextSection = sections[sections.indexOf('identity') + 1];
      if (nextSection) this.activeSection.set(nextSection);
    });
  }

  private configureConditionalValidators(): void {
    merge(
      this.formsStore.academicForm.controls.proposalType.valueChanges,
      this.formsStore.academicForm.controls.degreeProgram.valueChanges,
      this.educationForm.controls.highSchoolYear.valueChanges,
      this.educationForm.controls.studiesHighSchool.valueChanges,
      this.educationForm.controls.highSchoolLocation.valueChanges,
      this.educationForm.controls.higherEducationStatus.valueChanges,
      this.educationForm.controls.higherEducationUniversities.valueChanges,
      this.educationForm.controls.repeatsHighSchoolYear.valueChanges,
      this.educationForm.controls.motherEducation.valueChanges,
      this.educationForm.controls.fatherEducation.valueChanges,
      this.academicDecisionForm.controls.otherUniversities.valueChanges,
      this.academicDecisionForm.controls.researchedUniversities.valueChanges,
      this.ortExperienceForm.controls.advisingMeeting.valueChanges,
      this.ortExperienceForm.controls.visitedWebsite.valueChanges,
      this.ortExperienceForm.controls.visitedCampus.valueChanges,
      this.ortExperienceForm.controls.recallsAdvertising.valueChanges
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateConditionalValidators());
    this.updateConditionalValidators();
  }

  private updateConditionalValidators(): void {
    const education = this.educationForm.controls;
    const decision = this.academicDecisionForm.controls;
    const experience = this.ortExperienceForm.controls;
    const work = this.workForm.controls;
    const currentlyInSchool = education.studiesHighSchool.value === 'studying';
    const professionalUpdate = this.isProfessionalUpdate();

    const isHighSchoolYearRequired =
      currentlyInSchool && this.options.schoolYearOptions().length > 0;
    education.highSchoolYear.setValidators([
      ...(isHighSchoolYearRequired ? [Validators.required] : []),
      disallowedHighSchoolYearForUniversity(() => this.isUniversityCareer()),
    ]);
    education.highSchoolYear.updateValueAndValidity({ emitEvent: false });
    this.setRequired(education.orientation, this.shouldAskHighSchoolOrientation());
    this.setRequired(education.highSchoolYearRepeatCount, this.shouldAskHighSchoolRepeatCount(), [
      Validators.required,
      Validators.min(1),
    ]);
    this.setRequired(
      education.state,
      this.isNationalSchoolPlace() && this.options.departmentOptions().length > 0
    );
    this.setRequired(
      education.educationalInstitution,
      (this.isNationalSchoolPlace() && this.options.institutionOptions().length > 0) ||
        this.isForeignSchoolPlace()
    );
    this.setRequired(
      education.higherEducationUniversities,
      this.shouldAskHigherEducationUniversities() &&
        this.options.higherEducationUniversityOptions().length > 0
    );
    this.setRequired(
      education.otherHigherEducationUniversity,
      this.shouldAskHigherEducationOtherUniversity()
    );
    this.setRequired(education.motherOrtDegree, this.shouldAskMotherOrtDegree());
    this.setRequired(education.fatherOrtDegree, this.shouldAskFatherOrtDegree());

    this.setRequired(
      decision.researchedUniversities,
      decision.otherUniversities.value === 'yes' && this.options.universityOptions().length > 0
    );
    this.setRequired(decision.otherResearchedUniversity, this.shouldAskInformedOtherUniversity());

    this.setRequired(experience.advisingRating, experience.advisingMeeting.value === 'yes');
    this.setRequired(experience.websiteRating, experience.visitedWebsite.value === 'yes');
    this.setRequired(experience.campusRating, experience.visitedCampus.value === 'yes');
    this.setRequired(
      experience.advertisingChannels,
      experience.recallsAdvertising.value === 'yes' && this.options.advertisingOptions().length > 0
    );

    this.setRequired(work.isCorporate, professionalUpdate);
  }

  private setRequired(
    control: AbstractControl,
    required: boolean,
    validators: ValidatorFn | ValidatorFn[] = Validators.required
  ): void {
    control.setValidators(required ? validators : null);
    control.updateValueAndValidity({ emitEvent: false });
  }

  // Carga diferida del reglamento estudiantil (último subpaso del paso 2): se pide
  // recién al acercarse la sección de identidad/reglamento y una sola vez (el flag
  // evita repetir la consulta ante navegación atrás/adelante o re-render).
  private deferStudentRegulationAcceptance(): void {
    effect(() => {
      const nearRegulation =
        this.process.flow.currentStep() === 'survey' &&
        (this.activeSection() === 'identity' || this.activeSection() === 'regulation');
      if (this.regulationAcceptanceRequested || !nearRegulation) return;
      this.regulationAcceptanceRequested = true;
      this.loadStudentRegulationAcceptance();
    });
  }

  private loadStudentRegulationAcceptance(): void {
    this.enrollments
      .getStudentRegulationAcceptance()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (acceptance: EnrollmentStudentRegulationAcceptance) => {
          const accepted = acceptance.acceptedStudentRegulation === true;
          this.hasAcceptedStudentRegulation.set(accepted);
          if (!accepted) return;
          this.submittedAcceptanceDate.set(parseDate(acceptance.acceptanceDate));
          this.regulationForm.controls.acceptsRegulation.setValue(true);
          this.patchProgress('regulation', { completed: true });
        },
        error: () => this.hasAcceptedStudentRegulation.set(false),
      });
  }

  private reapplyBackendSurvey(): void {
    const state = this.appliedSurveyState;
    const survey = state?.response.survey;
    if (state && survey) {
      this.applyBackendSurvey(survey, state.response, state.includeAcademicSelection);
    }
  }

  private applyBackendSurvey(
    survey: EnrollmentInitialSurvey,
    response: EnrollmentInitialSurveyResponse,
    includeAcademicSelection: boolean
  ): void {
    const proposalType = patchBackendSurveyForms(survey, response, {
      forms: this.formsStore.forms,
      careers: this.proposal.careers(),
      includeAcademicSelection,
    });
    // En una inscripción nueva la encuesta previa no debe pisar el Paso 1.
    if (includeAcademicSelection) {
      this.proposal.setProposalType(proposalType);
      this.proposal.loadAcademicOptionsForSurvey(survey);
    }
    this.options.refreshOrientationOptions();
    this.updateConditionalValidators();
  }
}
