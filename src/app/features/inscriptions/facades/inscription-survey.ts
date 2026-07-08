import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import type { OrtErrorItem } from '@desarrolloort/components';
import { merge, Observable, of } from 'rxjs';
import { catchError, finalize, switchMap } from 'rxjs/operators';
import {
  DEFAULT_ERROR_ALERT,
  type ErrorAlertState,
} from 'src/app/shared/ui/error-alert/error-alert';

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
import type { InscripcionInitialSurveyResolved } from '../resolvers/inscription-initial-survey.resolver';
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
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formsStore = inject(InscripcionFormsStore);
  private readonly process = inject(InscripcionProcessStore);
  private readonly payment = inject(InscripcionPaymentFacade);
  private readonly proposal = inject(InscripcionProposalFacade);

  public readonly options = inject(InscripcionSurveyOptionsFacade);
  public readonly identity = inject(InscripcionSurveyIdentityFacade);

  private initialSurveyResponse: InscripcionInitialSurveyResponse | null = null;

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
  public readonly visibleSections = computed(() => getSeccionesVisibles(this.scenario()));
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

  constructor() {
    this.options.initialize({
      onOptionsChanged: () => this.updateConditionalValidators(),
      onInitialCatalogsApplied: () => this.reapplyBackendSurvey(),
    });
    this.identity.initialize({
      isIdentitySectionActive: computed(
        () => this.process.flow.currentStep() === 'encuesta' && this.activeSection() === 'identidad'
      ),
      surveyLoadError: this.surveyLoadError,
      onIdentityChanged: () => this.syncSectionCompletion('identidad'),
    });
    this.configureConditionalValidators();
    this.observeForms();
    this.observeIdentityConfirmation();
    this.loadStudentRegulationAcceptance();
    this.applyResolvedInitialSurveyState();
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
    if (
      this.progressOf(section).completed ||
      (section !== 'identidad' && this.isSectionValid(section))
    ) {
      return 'completa';
    }
    if (this.activeSection() === section) return 'activa';
    return 'pendiente';
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

  public retryInitialSurvey(): void {
    this.loadInitialSurveyState();
  }

  public savePartial(): Observable<boolean> {
    if (!this.hasInitialSurveyRight()) return of(true);
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

    const confirmPayload = buildConfirmPreEnrollmentPayload(this.formsStore.forms);
    if (!confirmPayload) {
      this.preEnrollmentError.set(
        'No se pudo confirmar la preinscripción con la oferta seleccionada.'
      );
      return;
    }

    this.preEnrollmentError.set(null);
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
    if (section !== 'identidad' && this.isSectionValid(section)) {
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
      this.ortExperienceForm.controls.recuerdaPublicidad.valueChanges,
      this.workForm.controls.situacionLaboral.valueChanges
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

    this.setRequired(
      work.tipoJornadaLaboral,
      work.situacionLaboral.value === 'trabaja' && this.options.workScheduleOptions().length > 0
    );
  }

  private setRequired(
    control: AbstractControl,
    required: boolean,
    validators: ValidatorFn | ValidatorFn[] = Validators.required
  ): void {
    control.setValidators(required ? validators : null);
    control.updateValueAndValidity({ emitEvent: false });
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

  private loadInitialSurveyState(): void {
    if (this.loadingSurveyState()) return;
    this.surveyLoadError.set(null);
    this.loadingSurveyState.set(true);
    this.inscriptions
      .getInitialSurvey()
      .pipe(
        finalize(() => this.loadingSurveyState.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: response => this.applyInitialSurvey(response),
        error: error => {
          if (isNotFoundError(error)) {
            this.initializeEmptySurvey();
            return;
          }
          this.surveyLoadError.set(
            'No se pudo consultar el estado de tu encuesta. Intentá nuevamente.'
          );
        },
      });
  }

  private applyResolvedInitialSurveyState(): void {
    const resolved = this.route.snapshot.data['initialSurvey'] as
      | InscripcionInitialSurveyResolved
      | undefined;
    if (!resolved) {
      this.loadInitialSurveyState();
      return;
    }
    if (resolved.loadFailed) {
      this.surveyLoadError.set(
        'No se pudo consultar el estado de tu encuesta. Intentá nuevamente.'
      );
      return;
    }
    this.applyInitialSurvey(
      resolved.initialSurvey ?? {
        tieneDerechoEncuesta: true,
        encuesta: null,
        universidadesConsideradas: [],
        universidadesConsideradasOtros: [],
        universidadesEducacionSuperior: [],
        universidadesEducacionSuperiorOtros: [],
        opcionesMotivosSeleccionados: [],
        opcionesPublicidadSeleccionadas: [],
      }
    );
  }

  private initializeEmptySurvey(): void {
    this.hasInitialSurveyRight.set(true);
    this.surveyState.set('no-iniciada');
    this.sectionProgress.set({});
    this.activeSection.set('educacion');
    this.process.flow.reset();
  }

  private initializeIdentityOnlySurvey(): void {
    this.hasInitialSurveyRight.set(false);
    this.surveyState.set('completa');
    this.sectionProgress.set({});
    this.activeSection.set('identidad');
    this.process.flow.reset();
  }

  private applyInitialSurvey(response: InscripcionInitialSurveyResponse): void {
    this.initialSurveyResponse = response;
    if (response.tieneDerechoEncuesta === false) {
      this.initializeIdentityOnlySurvey();
      return;
    }

    this.hasInitialSurveyRight.set(true);
    const survey = response.encuesta;
    if (!survey) {
      this.initializeEmptySurvey();
      return;
    }

    const isComplete = survey.completa;
    this.surveyState.set(isComplete ? 'completa' : 'en-progreso');
    this.applyBackendSurvey(survey, response);

    const activeSection = isComplete ? 'identidad' : (survey.seccionActiva ?? 'educacion');
    const visibleSections = getSeccionesVisibles(isComplete ? 'encuesta-completa' : 'primera-vez');
    const activeIndex = visibleSections.indexOf(activeSection);
    const completedSections = activeIndex > 0 ? visibleSections.slice(0, activeIndex) : [];
    this.sectionProgress.set(
      Object.fromEntries(
        completedSections.map(section => [section, { completed: true, submitted: false }])
      )
    );
    this.activeSection.set(activeSection);
    this.process.flow.goTo('encuesta');
    this.proposal.loadAcademicOptionsForSurvey(survey);
  }

  private reapplyBackendSurvey(): void {
    const response = this.initialSurveyResponse;
    if (response?.encuesta) this.applyBackendSurvey(response.encuesta, response);
  }

  private applyBackendSurvey(
    survey: InscripcionInitialSurvey,
    response: InscripcionInitialSurveyResponse
  ): void {
    const proposalType = patchBackendSurveyForms(survey, response, {
      forms: this.formsStore.forms,
      careers: this.proposal.careers(),
    });
    this.proposal.setProposalType(proposalType);
    this.options.refreshOrientationOptions();
    this.updateConditionalValidators();
  }
}

function isNotFoundError(error: unknown): error is { status: number } {
  return typeof error === 'object' && error !== null && 'status' in error && error.status === 404;
}
