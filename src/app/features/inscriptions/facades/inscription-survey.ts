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

import type { InscripcionSurveyInit } from '../models/inscription-entry';
import type {
  EstadoEncuestaInicial,
  EstadoSeccionEncuesta,
  InscripcionInitialSurvey,
  InscripcionInitialSurveyResponse,
  InscripcionStudentRegulationAcceptance,
  SeccionEncuestaId,
} from '../models/inscription-flow';
import {
  buildFormErrors,
  disallowedBachilleratoForUniversity,
  type IdentityFileTarget,
  NIVEL_UNIVERSITARIO,
} from '../models/inscription-flow-forms';
import {
  buildConfirmPreEnrollmentPayload,
  buildInitialSurveyPayload,
  hasCompleteUniversityEducation,
  parseDate,
  patchBackendSurveyForms,
} from '../models/inscription-flow-mappers';
import { getSeccionesVisibles } from '../models/inscription-flow-policy';
import {
  type InscripcionInitialSurveyResolved,
  resolveInitialSurvey,
} from '../resolvers/inscription-initial-survey.resolver';
import { Inscripciones } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionPaymentFacade } from './inscription-payment';
import { InscripcionProposalFacade } from './inscription-proposal';
import { InscripcionSurveyIdentityFacade } from './inscription-survey-identity';
import { InscripcionSurveyOptionsFacade } from './inscription-survey-options';

const IDENTITY_SAVE_ERROR = 'identity-save';

interface SectionProgress {
  completed: boolean;
  submitted: boolean;
}

const EMPTY_PROGRESS: SectionProgress = { completed: false, submitted: false };

/**
 * Orquestador del paso 2 (encuesta inicial + identidad + reglamento): estado de
 * secciones, validadores condicionales y cierre del paso. Los catálogos viven en
 * `InscripcionSurveyOptionsFacade` (`options`) y la verificación de identidad en
 * `InscripcionSurveyIdentityFacade` (`identity`).
 */
export class InscripcionSurveyFacade {
  private readonly inscriptions = inject(Inscripciones);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formsStore = inject(InscripcionFormsStore);
  private readonly process = inject(InscripcionProcessStore);
  private readonly payment = inject(InscripcionPaymentFacade);
  private readonly proposal = inject(InscripcionProposalFacade);

  public readonly options = inject(InscripcionSurveyOptionsFacade);
  public readonly identity = inject(InscripcionSurveyIdentityFacade);

  // Slice de encuesta prellenada ya aplicado:
  // se conserva para re-aplicarlo cuando llegan los catálogos, respetando su flag
  // `includeAcademicSelection` original.
  private appliedSurveyState: Extract<InscripcionSurveyInit, { kind: 'prefilled' }> | null = null;

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
    Readonly<Partial<Record<SeccionEncuestaId, SectionProgress>>>
  >({});
  // Suprime la completitud reactiva de identidad tras un fallo de subida: el form
  // sigue válido y los archivos presentes, pero el paso debe reabrirse SIN check
  // hasta que el usuario modifique datos de identidad o arranque un nuevo intento.
  private readonly identityUploadFailed = signal(false);
  public readonly activeSection = signal<SeccionEncuestaId>('educacion');
  public readonly readerOpen = signal(false);
  public readonly surveyState = signal<EstadoEncuestaInicial>('no-iniciada');
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
    !this.hasInitialSurveyRight() || this.surveyState() === 'completa'
      ? 'encuesta-completa'
      : this.surveyState() === 'en-progreso'
        ? 'parcial'
        : 'primera-vez'
  );
  public readonly isProfessionalUpdate = this.proposal.selection.isProfessionalUpdate;
  public readonly visibleSections = computed(() =>
    getSeccionesVisibles(this.scenario(), this.isProfessionalUpdate())
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
    if (section !== 'identidad') return formErrors;

    const files = this.identity.identityFiles();
    return [
      ...formErrors,
      ...(!files.frente ? [{ message: 'Frente del documento es obligatorio.' }] : []),
      ...(!files.dorso ? [{ message: 'Dorso del documento es obligatorio.' }] : []),
      ...(!files.selfie ? [{ message: 'Foto del rostro es obligatoria.' }] : []),
    ];
  });
  public readonly activeSectionErrorAlert = computed<ErrorAlertState | null>(() =>
    this.activeSectionErrors().length > 0 ? DEFAULT_ERROR_ALERT : null
  );

  private regulationAcceptanceRequested = false;

  constructor() {
    this.options.initialize({
      isSurveyStepActive: computed(() => this.process.flow.currentStep() === 'encuesta'),
      onOptionsChanged: () => this.updateConditionalValidators(),
      onInitialCatalogsApplied: () => this.reapplyBackendSurvey(),
    });
    this.identity.initialize({
      isIdentitySectionActive: computed(
        () => this.process.flow.currentStep() === 'encuesta' && this.activeSection() === 'identidad'
      ),
      surveyLoadError: this.surveyLoadError,
      onIdentityChanged: () => {
        this.identityUploadFailed.set(false);
        this.syncSectionCompletion('identidad');
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
    // `InscripcionProcessFacade` (único inicializador) vía `applyInitialState`.
  }

  /**
   * Aplica el slice de encuesta derivado por `deriveInitialInscripcionState`. NO
   * toca `process.flow`: el paso lo posiciona `ProcessFacade` una sola vez.
   */
  public applyInitialState(state: InscripcionSurveyInit): void {
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
        this.surveyState.set('completa');
        this.sectionProgress.set({});
        this.activeSection.set('identidad');
        return;
      case 'fresh':
        this.appliedSurveyState = null;
        this.hasInitialSurveyRight.set(true);
        this.surveyState.set('no-iniciada');
        this.sectionProgress.set({});
        this.activeSection.set('educacion');
        return;
      case 'prefilled': {
        const encuesta = state.response.encuesta;
        if (!encuesta) return;
        this.appliedSurveyState = state;
        this.hasInitialSurveyRight.set(true);
        this.surveyState.set(state.surveyState);
        this.applyBackendSurvey(encuesta, state.response, state.includeAcademicSelection);
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
  public fetchResolvedInitialSurvey(): Observable<InscripcionInitialSurveyResolved> {
    this.surveyLoadError.set(null);
    this.loadingSurveyState.set(true);
    return resolveInitialSurvey(this.inscriptions.getInitialSurvey()).pipe(
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

  public back(): void {
    if (this.readerOpen()) {
      this.readerOpen.set(false);
      return;
    }

    const sections = this.visibleSections();
    const currentIndex = sections.indexOf(this.activeSection());
    if (currentIndex > 0) {
      this.activeSection.set(sections[currentIndex - 1]);
      return;
    }
    this.process.flow.previous();
  }

  public openSection(section: SeccionEncuestaId): void {
    if (this.visibleSections().includes(section)) this.activeSection.set(section);
  }

  public getSectionState(section: SeccionEncuestaId): EstadoSeccionEncuesta {
    if (this.progressOf(section).completed || this.canSectionAutoComplete(section)) {
      return 'completa';
    }
    if (this.activeSection() === section) return 'activa';
    return 'pendiente';
  }

  // Una sección válida se marca completa sola (sin apretar Continuar). Identidad es
  // la excepción tras un fallo de subida: sigue válida pero no debe auto-completarse.
  private canSectionAutoComplete(section: SeccionEncuestaId): boolean {
    return (
      (section !== 'identidad' || !this.identityUploadFailed()) && this.isSectionValid(section)
    );
  }

  public isSectionPending(section: SeccionEncuestaId): boolean {
    return this.progressOf(section).submitted && !this.isSectionValid(section);
  }

  public isIdentityFileMissing(target: IdentityFileTarget): boolean {
    return this.progressOf('identidad').submitted && !this.identity.identityFiles()[target];
  }

  public isUniversityCareer(): boolean {
    const selectedCareer = this.formsStore.academicForm.controls.carrera.value;
    if (!selectedCareer) return false;
    const nivel = this.proposal
      .careers()
      .find(career => career.idProducto.toString() === selectedCareer)?.idNivelProducto;
    return nivel === NIVEL_UNIVERSITARIO;
  }

  public isNationalSchoolPlace(): boolean {
    return this.educationForm.controls.lugarSecundaria.value === '1';
  }

  public isForeignSchoolPlace(): boolean {
    return this.educationForm.controls.lugarSecundaria.value === '2';
  }

  public shouldAskBaccalaureateOrientation(): boolean {
    const selectedYear = this.educationForm.controls.anioSecundaria.value;
    return (
      this.educationForm.controls.cursaSecundaria.value === 'cursando' &&
      !!selectedYear &&
      this.options.orientationOptions().length > 0
    );
  }

  public shouldAskRecursaCount(): boolean {
    return this.educationForm.controls.recursaAnioBachillerato.value === 'si';
  }

  public shouldAskHigherEducationUniversities(): boolean {
    return this.educationForm.controls.estadoEducacionSuperior.value === '1';
  }

  public shouldAskHigherEducationOtherUniversity(): boolean {
    return (
      this.shouldAskHigherEducationUniversities() &&
      this.educationForm.controls.universidadesEducacionSuperior.value.includes('0')
    );
  }

  public shouldAskInformedOtherUniversity(): boolean {
    return (
      this.academicDecisionForm.controls.otrasUniversidades.value === 'si' &&
      this.academicDecisionForm.controls.universidadesInformadas.value.includes('0')
    );
  }

  public shouldAskMotherOrtDegree(): boolean {
    return hasCompleteUniversityEducation(this.educationForm.controls.formacionMadre.value);
  }

  public shouldAskFatherOrtDegree(): boolean {
    return hasCompleteUniversityEducation(this.educationForm.controls.formacionPadre.value);
  }

  public openRegulationReader(): void {
    this.readerOpen.set(true);
  }

  public acceptRegulation(): void {
    this.regulationForm.controls.aceptaReglamento.setValue(true);
    this.patchProgress('reglamento', { completed: true });
    this.activeSection.set('reglamento');
    this.readerOpen.set(false);
  }

  public savePartial(): Observable<boolean> {
    if (!this.hasInitialSurveyRight() || this.isProfessionalUpdate()) return of(true);
    return this.inscriptions.saveInitialSurvey(buildInitialSurveyPayload(this.formsStore.forms));
  }

  private progressOf(section: SeccionEncuestaId): SectionProgress {
    return this.sectionProgress()[section] ?? EMPTY_PROGRESS;
  }

  private patchProgress(section: SeccionEncuestaId, patch: Partial<SectionProgress>): void {
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
          return this.inscriptions.confirmPreEnrollment(confirmPayload);
        }),
        finalize(() => this.finalizingPreEnrollment.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: response => {
          this.process.preEnrollmentResponse.set(response);
          this.surveyState.set('completa');
          if (confirmPayload.esInscripcionCorporativa) {
            this.payment.outcome.set('inscription-en-proceso');
            return;
          }
          if (response.enEspera === true) {
            this.payment.outcome.set('inscription-en-proceso');
            return;
          }
          this.process.flow.next();
        },
        error: error => {
          const identitySaveFailed =
            error instanceof Error && error.message === IDENTITY_SAVE_ERROR;
          if (identitySaveFailed) {
            this.identityUploadFailed.set(true);
            this.patchProgress('identidad', { completed: false, submitted: true });
            this.activeSection.set('identidad');
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

  private findNextInvalidSection(section: SeccionEncuestaId): SeccionEncuestaId | undefined {
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

  private syncSectionCompletion(section: SeccionEncuestaId): void {
    if (this.canSectionAutoComplete(section)) {
      this.patchProgress(section, { completed: true });
      return;
    }
    if (!this.isSectionValid(section)) {
      this.patchProgress(section, { completed: false });
    }
  }

  private isSectionValid(section: SeccionEncuestaId): boolean {
    if (section !== 'identidad') return this.sectionConfig[section].form.valid;
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
      const confirmed = this.identityForm.controls.identidadCorrecta.value;
      if (
        !confirmed ||
        !this.identity.requiresIdentityConfirmation() ||
        !this.isSectionValid('identidad')
      ) {
        return;
      }

      this.patchProgress('identidad', { completed: true });
      if (this.activeSection() !== 'identidad') return;

      const sections = this.visibleSections();
      const nextSection = sections[sections.indexOf('identidad') + 1];
      if (nextSection) this.activeSection.set(nextSection);
    });
  }

  private configureConditionalValidators(): void {
    merge(
      this.formsStore.academicForm.controls.tipoPropuesta.valueChanges,
      this.formsStore.academicForm.controls.carrera.valueChanges,
      this.educationForm.controls.anioSecundaria.valueChanges,
      this.educationForm.controls.cursaSecundaria.valueChanges,
      this.educationForm.controls.lugarSecundaria.valueChanges,
      this.educationForm.controls.estadoEducacionSuperior.valueChanges,
      this.educationForm.controls.universidadesEducacionSuperior.valueChanges,
      this.educationForm.controls.recursaAnioBachillerato.valueChanges,
      this.educationForm.controls.formacionMadre.valueChanges,
      this.educationForm.controls.formacionPadre.valueChanges,
      this.academicDecisionForm.controls.otrasUniversidades.valueChanges,
      this.academicDecisionForm.controls.universidadesInformadas.valueChanges,
      this.ortExperienceForm.controls.reunionAsesoramiento.valueChanges,
      this.ortExperienceForm.controls.visitoWeb.valueChanges,
      this.ortExperienceForm.controls.visitoSede.valueChanges,
      this.ortExperienceForm.controls.recuerdaPublicidad.valueChanges
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
    const currentlyInSchool = education.cursaSecundaria.value === 'cursando';
    const professionalUpdate = this.isProfessionalUpdate();

    const anioBachilleratoRequired =
      currentlyInSchool && this.options.schoolYearOptions().length > 0;
    education.anioSecundaria.setValidators([
      ...(anioBachilleratoRequired ? [Validators.required] : []),
      disallowedBachilleratoForUniversity(() => this.isUniversityCareer()),
    ]);
    education.anioSecundaria.updateValueAndValidity({ emitEvent: false });
    this.setRequired(education.orientacion, this.shouldAskBaccalaureateOrientation());
    this.setRequired(education.vecesRecursaAnioBachillerato, this.shouldAskRecursaCount(), [
      Validators.required,
      Validators.min(1),
    ]);
    this.setRequired(
      education.departamento,
      this.isNationalSchoolPlace() && this.options.departmentOptions().length > 0
    );
    this.setRequired(
      education.institucionEducativa,
      (this.isNationalSchoolPlace() && this.options.institutionOptions().length > 0) ||
        this.isForeignSchoolPlace()
    );
    this.setRequired(
      education.universidadesEducacionSuperior,
      this.shouldAskHigherEducationUniversities() &&
        this.options.higherEducationUniversityOptions().length > 0
    );
    this.setRequired(
      education.universidadEducacionSuperiorOtro,
      this.shouldAskHigherEducationOtherUniversity()
    );
    this.setRequired(education.tituloOrtMadre, this.shouldAskMotherOrtDegree());
    this.setRequired(education.tituloOrtPadre, this.shouldAskFatherOrtDegree());

    this.setRequired(
      decision.universidadesInformadas,
      decision.otrasUniversidades.value === 'si' && this.options.universityOptions().length > 0
    );
    this.setRequired(decision.universidadInformadaOtro, this.shouldAskInformedOtherUniversity());

    this.setRequired(
      experience.calificacionAsesoramiento,
      experience.reunionAsesoramiento.value === 'si'
    );
    this.setRequired(experience.calificacionWeb, experience.visitoWeb.value === 'si');
    this.setRequired(experience.calificacionSede, experience.visitoSede.value === 'si');
    this.setRequired(
      experience.mediosPublicidad,
      experience.recuerdaPublicidad.value === 'si' && this.options.advertisingOptions().length > 0
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
        this.process.flow.currentStep() === 'encuesta' &&
        (this.activeSection() === 'identidad' || this.activeSection() === 'reglamento');
      if (this.regulationAcceptanceRequested || !nearRegulation) return;
      this.regulationAcceptanceRequested = true;
      this.loadStudentRegulationAcceptance();
    });
  }

  private loadStudentRegulationAcceptance(): void {
    this.inscriptions
      .getStudentRegulationAcceptance()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (acceptance: InscripcionStudentRegulationAcceptance) => {
          const accepted = acceptance.aceptoReglamentoEstudiantil === true;
          this.hasAcceptedStudentRegulation.set(accepted);
          if (!accepted) return;
          this.submittedAcceptanceDate.set(parseDate(acceptance.fechaAceptacion));
          this.regulationForm.controls.aceptaReglamento.setValue(true);
          this.patchProgress('reglamento', { completed: true });
        },
        error: () => this.hasAcceptedStudentRegulation.set(false),
      });
  }

  private reapplyBackendSurvey(): void {
    const state = this.appliedSurveyState;
    const encuesta = state?.response.encuesta;
    if (state && encuesta) {
      this.applyBackendSurvey(encuesta, state.response, state.includeAcademicSelection);
    }
  }

  private applyBackendSurvey(
    survey: InscripcionInitialSurvey,
    response: InscripcionInitialSurveyResponse,
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
