import { computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import type {
  OrtErrorItem,
  OrtFileUploaderChange,
  OrtPreloadedFile,
} from '@desarrolloort/components';
import { merge, Observable, of } from 'rxjs';
import { catchError, finalize, switchMap, tap } from 'rxjs/operators';

import {
  Career,
  Comienzo,
  InitialSurveyCatalogs,
  Turno,
} from '../../catalogs/models/catalog.interface';
import { Catalogs } from '../../catalogs/services/catalogs';
import {
  ArchivosIdentidad,
  EnvioInscripcion,
  EstadoEncuestaInicial,
  EstadoSeccionEncuesta,
  InscripcionBackendSurvey,
  InscripcionInitialSurveyResponse,
  InscripcionPreEnrollmentResponse,
  MetodoPago,
  OpcionInscripcion,
  PantallaInscripcion,
  SeccionEncuestaId,
} from '../models/inscripcion-flow';
import {
  buildFormErrors,
  createInscripcionForms,
  createSectionConfig,
  type IdentityFileTarget,
  type IdentityPreloadedFileMap,
} from '../models/inscripcion-flow-forms';
import {
  buildConfirmPreEnrollmentPayload,
  buildInitialSurveyPayload,
  getSurveyValues,
  isBackendSurveyComplete,
  parseDate,
  patchBackendSurveyForms,
  resolveBackendSection,
  serializeDate,
  toNullableNumber,
} from '../models/inscripcion-flow-mappers';
import {
  getAvailableProposalOptions,
  getCareerOptions,
  getOptionLabel,
  getProposalLevelIds,
  getProposalOptionByLevel,
  toCatalogOptions,
  toStartOption,
  toTurnoOption,
} from '../models/inscripcion-flow-options';
import {
  findFirstIncompleteSection,
  getPreviousScreen,
  getResultadoPago,
  getSeccionesVisibles,
  parseResultadoForzado,
} from '../models/inscripcion-flow-policy';
import {
  buildStepperSteps,
  buildSummaryItems,
  formatInscriptionAmount,
  formatPaymentDeadline,
  getReservationInstructions,
  getStepNumber,
  getStepSupportLabel,
} from '../models/inscripcion-flow-view';
import {
  COORDINATORS,
  PAYMENT_OPTIONS,
  SUBJECTS,
  WORK_STATUS_OPTIONS,
} from '../models/inscripcion-static-data';
import type { InscripcionInitialSurveyResolved } from '../resolvers/inscripcion-initial-survey.resolver';
import { Inscripciones, type InscripcionIdentityPreload } from '../services/inscripciones';

export class InscripcionFlowFacade {
  private readonly catalogs = inject(Catalogs);
  private readonly inscripciones = inject(Inscripciones);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private processingTimer: ReturnType<typeof setTimeout> | null = null;
  private initialSurveyResponse: InscripcionInitialSurveyResponse | null = null;
  private identityPreloadRequested = false;
  private readonly identityFileTouched = new Set<IdentityFileTarget>();
  private readonly preEnrollmentResponse = signal<InscripcionPreEnrollmentResponse | null>(null);

  public readonly paymentOptions = PAYMENT_OPTIONS;
  public readonly coordinators = COORDINATORS;
  public readonly studentNumber = '397654';
  public readonly acceptedImageTypes = ['image/jpeg', 'image/png'];
  public readonly workStatusOptions = WORK_STATUS_OPTIONS;

  private readonly forms = createInscripcionForms();

  public readonly academicForm = this.forms.academicForm;
  public readonly educationForm = this.forms.educationForm;
  public readonly academicDecisionForm = this.forms.academicDecisionForm;
  public readonly ortExperienceForm = this.forms.ortExperienceForm;
  public readonly workForm = this.forms.workForm;
  public readonly identityForm = this.forms.identityForm;
  public readonly regulationForm = this.forms.regulationForm;
  public readonly paymentForm = this.forms.paymentForm;

  private readonly sectionConfig = createSectionConfig(this.forms);

  private readonly careers = signal<readonly Career[]>([]);
  private readonly proposalTypeValue = signal(this.academicForm.controls.tipoPropuesta.value);
  private readonly completedSections = signal<readonly SeccionEncuestaId[]>([]);
  private readonly submittedSections = signal<readonly SeccionEncuestaId[]>([]);
  private readonly submittedAcademic = signal(false);
  private readonly submittedPayment = signal(false);
  private readonly productInterestError = signal<string | null>(null);

  public readonly forcedResult = parseResultadoForzado(
    this.route.snapshot.queryParamMap.get('resultado')
  );
  public readonly screen = signal<PantallaInscripcion>('propuesta');
  public readonly activeSection = signal<SeccionEncuestaId>('educacion');
  public readonly surveyState = signal<EstadoEncuestaInicial>('no-iniciada');
  public readonly hasInitialSurveyRight = signal(true);
  public readonly identityFiles = signal<ArchivosIdentidad>({
    frente: null,
    dorso: null,
    selfie: null,
  });
  private readonly preloadedIdentityFiles = signal<IdentityPreloadedFileMap>({
    frente: null,
    dorso: null,
    selfie: null,
  });
  public readonly initialIdentityFiles = computed(() => {
    const files = this.preloadedIdentityFiles();
    return {
      frente: files.frente ? [files.frente] : [],
      dorso: files.dorso ? [files.dorso] : [],
      selfie: files.selfie ? [files.selfie] : [],
    };
  });
  public readonly exitConfirmationOpen = signal(false);
  public readonly showAllSubjects = signal(false);
  public readonly selectedPaymentMethod = signal<MetodoPago | null>(null);

  public readonly startOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly turnoOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly previousCareerOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly educationLevelOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly supportOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly careerDecisionOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly motivesOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly catalogError = signal<string | null>(null);
  public readonly surveyLoadError = signal<string | null>(null);
  public readonly surveySaveError = signal<string | null>(null);
  public readonly preEnrollmentError = signal<string | null>(null);
  public readonly loadingCareers = signal(false);
  public readonly loadingInitialSurveyCatalogs = signal(false);
  public readonly loadingSurveyState = signal(false);
  public readonly loadingStarts = signal(false);
  public readonly loadingTurnos = signal(false);
  public readonly registeringProductInterest = signal(false);
  public readonly savingSurvey = signal(false);
  public readonly finalizingPreEnrollment = signal(false);

  public readonly visibleSections = computed(() =>
    getSeccionesVisibles(
      !this.hasInitialSurveyRight() || this.surveyState() === 'completa'
        ? 'encuesta-completa'
        : 'primera-vez'
    )
  );
  public readonly sectionItems = computed(() =>
    this.visibleSections().map(section => ({
      id: section,
      label: this.sectionConfig[section].label,
      icon: this.sectionConfig[section].icon,
      state: this.getSectionState(section),
    }))
  );
  public readonly proposalOptions = computed<readonly OpcionInscripcion[]>(() =>
    getAvailableProposalOptions(this.careers())
  );
  public readonly careerOptions = computed<readonly OpcionInscripcion[]>(() =>
    getCareerOptions(this.careers(), this.proposalTypeValue())
  );
  public readonly careersLoadingMessage = computed(() =>
    this.loadingCareers() ? 'Estamos cargando las carreras.' : ''
  );
  public readonly startsLoadingMessage = computed(() => {
    if (!this.loadingStarts()) return '';

    const career = getOptionLabel(
      this.careerOptions(),
      this.academicForm.controls.carrera.value,
      'la carrera seleccionada'
    );
    return `Estamos cargando los comienzos para "${career}".`;
  });
  public readonly turnosLoadingMessage = computed(() => {
    if (!this.loadingTurnos()) return '';

    const start = getOptionLabel(
      this.startOptions(),
      this.academicForm.controls.comienzo.value,
      'el comienzo seleccionado'
    );
    return `Estamos cargando los turnos para "${start}".`;
  });
  public readonly isTerminal = computed(() =>
    ['reserva', 'inscripcion-confirmada', 'inscripcion-en-proceso'].includes(this.screen())
  );
  public readonly showStepper = computed(
    () =>
      !this.loadingSurveyState() &&
      !this.surveyLoadError() &&
      ['propuesta', 'encuesta', 'lector-reglamento', 'pago', 'confirmacion-pago'].includes(
        this.screen()
      )
  );
  public readonly canGoBack = computed(
    () =>
      getPreviousScreen(this.screen(), this.activeSection(), this.visibleSections()) !== null &&
      this.screen() !== 'confirmacion-pago'
  );
  public readonly stepNumber = computed<1 | 2 | 3>(() => getStepNumber(this.screen()));
  public readonly stepSupportLabel = computed(() => getStepSupportLabel(this.stepNumber()));
  public readonly stepperSteps = computed(() => buildStepperSteps(this.stepNumber()));
  public readonly summaryItems = computed(() =>
    buildSummaryItems({
      response: this.preEnrollmentResponse(),
      selectedCareer: this.academicForm.controls.carrera.value,
      selectedStart: this.academicForm.controls.comienzo.value,
      selectedTurno: this.academicForm.controls.turno.value,
      careerOptions: this.careerOptions(),
      startOptions: this.startOptions(),
      turnoOptions: this.turnoOptions(),
    })
  );
  public readonly paymentDeadline = computed(() =>
    formatPaymentDeadline(this.preEnrollmentResponse()?.fechaVencimientoPago)
  );
  public readonly inscriptionAmount = computed(() =>
    formatInscriptionAmount(this.preEnrollmentResponse()?.seniaInscripcion)
  );
  public readonly visibleSubjects = computed(() =>
    this.showAllSubjects() ? SUBJECTS : SUBJECTS.slice(0, 4)
  );
  public readonly subjectsToggleLabel = computed(() =>
    this.showAllSubjects() ? 'Ver menos materias' : 'Ver todas las materias'
  );
  public readonly reservationInstructions = computed(() =>
    getReservationInstructions(this.selectedPaymentMethod())
  );
  public readonly academicErrors = computed<OrtErrorItem[]>(() => {
    if (!this.submittedAcademic()) return [];

    const formErrors = buildFormErrors(this.academicForm, [
      { controlName: 'tipoPropuesta', fieldId: '', label: 'Propuesta académica' },
      { controlName: 'carrera', fieldId: '', label: 'Carrera' },
      { controlName: 'comienzo', fieldId: '', label: 'Comienzo' },
      { controlName: 'turno', fieldId: '', label: 'Turno' },
    ]);
    const productInterestError = this.productInterestError();

    return productInterestError ? [...formErrors, { message: productInterestError }] : formErrors;
  });
  public readonly activeSectionErrors = computed<OrtErrorItem[]>(() => {
    const section = this.activeSection();
    if (!this.submittedSections().includes(section)) return [];
    const config = this.sectionConfig[section];

    const formErrors = buildFormErrors(config.form, config.errorFields);

    if (section !== 'identidad') return formErrors;

    const files = this.identityFiles();
    return [
      ...formErrors,
      ...(!files.frente ? [{ message: 'Frente del documento es obligatorio.' }] : []),
      ...(!files.dorso ? [{ message: 'Dorso del documento es obligatorio.' }] : []),
      ...(!files.selfie ? [{ message: 'Foto del rostro es obligatoria.' }] : []),
    ];
  });
  public readonly paymentErrors = computed<OrtErrorItem[]>(() =>
    this.submittedPayment()
      ? buildFormErrors(this.paymentForm, [
          { controlName: 'metodoPago', fieldId: '', label: 'Medio de pago' },
        ])
      : []
  );

  constructor() {
    this.loadCareers();
    this.loadInitialSurveyCatalogs();
    this.resetAcademicSelectionOnProposalChange();
    this.loadStartsOnCareerChange();
    this.loadTurnosOnStartChange();
    this.observeForms();
    this.loadIdentityPreloadOnIdentitySection();
    this.applyResolvedInitialSurveyState();
    this.destroyRef.onDestroy(() => {
      if (this.processingTimer) clearTimeout(this.processingTimer);
    });
  }

  public continue(): void {
    switch (this.screen()) {
      case 'propuesta':
        this.submitAcademic();
        break;
      case 'encuesta':
        this.submitActiveSection();
        break;
      case 'pago':
        this.requestPaymentConfirmation();
        break;
      default:
        break;
    }
  }

  public back(): void {
    const currentScreen = this.screen();
    const previousScreen = getPreviousScreen(
      currentScreen,
      this.activeSection(),
      this.visibleSections()
    );

    if (!previousScreen) return;

    if (currentScreen === 'encuesta' && previousScreen === 'encuesta') {
      const currentIndex = this.visibleSections().indexOf(this.activeSection());
      this.activeSection.set(this.visibleSections()[currentIndex - 1]);
    } else {
      this.screen.set(previousScreen);
    }
  }

  public openSection(section: SeccionEncuestaId): void {
    if (!this.visibleSections().includes(section)) return;
    this.activeSection.set(section);
  }

  public canSelectCareer(): boolean {
    return (
      !this.loadingCareers() &&
      getProposalLevelIds(this.academicForm.controls.tipoPropuesta.value).length > 0 &&
      this.careerOptions().length > 0
    );
  }

  public canSelectStart(): boolean {
    return (
      !!this.academicForm.controls.carrera.value &&
      !this.loadingStarts() &&
      this.startOptions().length > 0
    );
  }

  public canSelectTurno(): boolean {
    return (
      !!this.academicForm.controls.comienzo.value &&
      !this.loadingTurnos() &&
      this.turnoOptions().length > 0
    );
  }

  public getSectionState(section: SeccionEncuestaId): EstadoSeccionEncuesta {
    if (this.isSectionValid(section)) return 'completa';
    if (this.activeSection() === section) return 'activa';
    return this.completedSections().includes(section) ? 'completa' : 'pendiente';
  }

  public updateIdentityFile(target: IdentityFileTarget, event: OrtFileUploaderChange): void {
    const selectedFile = event.value.find(file => file.isValid)?.file ?? null;
    this.identityFileTouched.add(target);
    this.preloadedIdentityFiles.update(files => ({ ...files, [target]: null }));
    this.identityFiles.update(files => ({ ...files, [target]: selectedFile }));
    this.syncSectionCompletion('identidad');
  }

  public openRegulationReader(): void {
    this.screen.set('lector-reglamento');
  }

  public acceptRegulation(): void {
    this.regulationForm.controls.aceptaReglamento.setValue(true);
    this.completeSection('reglamento');
    this.screen.set('encuesta');
    this.activeSection.set('reglamento');
  }

  public requestPaymentConfirmation(): void {
    this.submittedPayment.set(true);
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    this.screen.set('confirmacion-pago');
  }

  public cancelPaymentConfirmation(): void {
    if (this.screen() !== 'confirmacion-pago') return;

    this.screen.set('pago');
  }

  public confirmPayment(): void {
    const method = this.paymentForm.controls.metodoPago.value;
    if (!method) return;

    const submission = this.buildSubmission();
    this.selectedPaymentMethod.set(method);
    this.logSubmission(submission);

    const result = getResultadoPago(method, this.forcedResult);
    if (result === 'reservada') {
      this.finishAt('reserva');
      return;
    }

    if (result === 'en-proceso') {
      this.finishAt('inscripcion-en-proceso');
      return;
    }

    this.screen.set('procesando');
    this.processingTimer = setTimeout(() => {
      this.processingTimer = null;
      this.finishAt('inscripcion-confirmada');
    }, 1000);
  }

  public requestExit(): void {
    this.surveySaveError.set(null);
    this.exitConfirmationOpen.set(true);
  }

  public cancelExit(): void {
    this.surveySaveError.set(null);
    this.exitConfirmationOpen.set(false);
  }

  public confirmExit(): void {
    if (this.savingSurvey()) return;

    if (!this.hasInitialSurveyRight()) {
      this.exitConfirmationOpen.set(false);
      void this.router.navigateByUrl('/inicio');
      return;
    }

    this.surveySaveError.set(null);
    this.savingSurvey.set(true);
    this.inscripciones
      .saveInitialSurvey(this.getInitialSurveyPayload())
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

  public retryInitialSurvey(): void {
    this.loadInitialSurveyState();
  }

  public toggleSubjects(): void {
    this.showAllSubjects.update(showAll => !showAll);
  }

  public buildSubmission(): EnvioInscripcion {
    const method = this.paymentForm.controls.metodoPago.value;
    if (!method) {
      throw new Error('No se puede crear una inscripción sin medio de pago.');
    }

    const files = this.identityFiles();
    const surveyState = this.surveyState();
    return {
      escenario:
        surveyState === 'completa'
          ? 'encuesta-completa'
          : surveyState === 'en-progreso'
            ? 'parcial'
            : 'primera-vez',
      estadoEncuestaInicial: surveyState,
      propuesta: this.academicForm.getRawValue(),
      encuesta: surveyState === 'completa' ? null : getSurveyValues(this.forms),
      identidad: {
        vencimientoDocumento: serializeDate(this.identityForm.controls.vencimientoDocumento.value),
        frenteAdjunto: files.frente !== null,
        dorsoAdjunto: files.dorso !== null,
        selfieAdjunta: files.selfie !== null,
      },
      reglamentoAceptado: this.regulationForm.controls.aceptaReglamento.value,
      metodoPago: method,
    };
  }

  private submitAcademic(): void {
    if (this.registeringProductInterest()) return;

    this.submittedAcademic.set(true);
    this.productInterestError.set(null);
    if (this.academicForm.invalid) {
      this.academicForm.markAllAsTouched();
      return;
    }

    const payload = this.buildProductInterestPayload();
    if (!payload) {
      this.productInterestError.set('Seleccioná una propuesta válida para continuar.');
      return;
    }

    this.registeringProductInterest.set(true);
    this.inscripciones
      .registerProductInterest(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: registered => {
          this.registeringProductInterest.set(false);
          if (!registered) {
            this.productInterestError.set(
              'No se pudo registrar el interés por la propuesta seleccionada.'
            );
            return;
          }

          this.advanceToSurvey();
        },
        error: () => {
          this.registeringProductInterest.set(false);
          this.productInterestError.set(
            'No se pudo registrar el interés por la propuesta seleccionada.'
          );
        },
      });
  }

  private advanceToSurvey(): void {
    this.screen.set('encuesta');
    this.activeSection.set(
      findFirstIncompleteSection(this.visibleSections(), this.completedSections())
    );
  }

  private submitActiveSection(): void {
    if (this.finalizingPreEnrollment()) return;

    const section = this.activeSection();
    this.submittedSections.update(sections =>
      sections.includes(section) ? sections : [...sections, section]
    );

    if (!this.isSectionValid(section)) {
      this.sectionConfig[section].form.markAllAsTouched();
      return;
    }

    this.completeSection(section);
    const sections = this.visibleSections();
    const currentIndex = sections.indexOf(section);
    const nextSection = sections[currentIndex + 1];

    if (nextSection) {
      this.activeSection.set(nextSection);
    } else {
      this.finishSurveyStep();
    }
  }

  private finishSurveyStep(): void {
    const confirmPayload = buildConfirmPreEnrollmentPayload(this.forms);
    if (!confirmPayload) {
      this.preEnrollmentError.set(
        'No se pudo confirmar la preinscripción con la oferta seleccionada.'
      );
      return;
    }

    this.preEnrollmentError.set(null);
    this.finalizingPreEnrollment.set(true);
    this.saveFinalSurvey()
      .pipe(
        switchMap(saved => {
          if (!saved) throw new Error('No se pudo guardar la encuesta inicial final.');
          return this.inscripciones.confirmPreEnrollment(confirmPayload);
        }),
        finalize(() => this.finalizingPreEnrollment.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: response => {
          if (response.confirmada === false) {
            this.preEnrollmentError.set(
              'No se pudo confirmar la preinscripción. Intentá nuevamente.'
            );
            return;
          }

          this.preEnrollmentResponse.set(response);
          this.surveyState.set('completa');
          this.screen.set('pago');
        },
        error: () =>
          this.preEnrollmentError.set(
            'No se pudo guardar y confirmar la preinscripción. Intentá nuevamente.'
          ),
      });
  }

  private saveFinalSurvey(): Observable<boolean> {
    if (!this.hasInitialSurveyRight()) return of(true);

    return this.inscripciones.saveInitialSurvey(this.getInitialSurveyPayload());
  }

  private completeSection(section: SeccionEncuestaId): void {
    this.completedSections.update(sections =>
      sections.includes(section) ? sections : [...sections, section]
    );
  }

  private syncSectionCompletion(section: SeccionEncuestaId): void {
    if (this.isSectionValid(section)) {
      this.completeSection(section);
      return;
    }

    this.completedSections.update(sections => sections.filter(item => item !== section));
  }

  private isSectionValid(section: SeccionEncuestaId): boolean {
    if (section !== 'identidad') return this.sectionConfig[section].form.valid;
    const files = this.identityFiles();
    return (
      this.identityForm.valid &&
      files.frente !== null &&
      files.dorso !== null &&
      files.selfie !== null
    );
  }

  private observeForms(): void {
    merge(
      this.academicForm.valueChanges,
      this.educationForm.valueChanges,
      this.academicDecisionForm.valueChanges,
      this.ortExperienceForm.valueChanges,
      this.workForm.valueChanges,
      this.identityForm.valueChanges,
      this.regulationForm.valueChanges,
      this.paymentForm.valueChanges
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.productInterestError.set(null);
        this.preEnrollmentError.set(null);
        for (const section of this.visibleSections()) {
          this.syncSectionCompletion(section);
        }
      });
  }

  private loadIdentityPreloadOnIdentitySection(): void {
    effect(() => {
      if (
        this.identityPreloadRequested ||
        this.surveyLoadError() ||
        this.screen() !== 'encuesta' ||
        this.activeSection() !== 'identidad'
      ) {
        return;
      }

      this.identityPreloadRequested = true;
      this.inscripciones
        .getIdentityPreload()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: preload => this.applyIdentityPreload(preload),
          error: () => undefined,
        });
    });
  }

  private applyIdentityPreload(preload: InscripcionIdentityPreload): void {
    this.applyPreloadedIdentityFile('frente', preload.frente);
    this.applyPreloadedIdentityFile('dorso', preload.dorso);
    this.applyPreloadedIdentityFile('selfie', preload.selfie);

    const expiration = parseDate(preload.fechaVencimiento);
    const expirationControl = this.identityForm.controls.vencimientoDocumento;
    if (expiration && !expirationControl.value && !expirationControl.dirty) {
      expirationControl.setValue(expiration);
    }

    this.syncSectionCompletion('identidad');
  }

  private applyPreloadedIdentityFile(target: IdentityFileTarget, file: File | null): void {
    if (!file || this.identityFileTouched.has(target) || this.identityFiles()[target]) return;

    this.identityFiles.update(files => ({ ...files, [target]: file }));
    void file
      .arrayBuffer()
      .then(src => {
        if (this.identityFileTouched.has(target) || this.identityFiles()[target] !== file) return;

        this.preloadedIdentityFiles.update(files => ({
          ...files,
          [target]: this.toIdentityPreloadedFile(target, file, src),
        }));
      })
      .catch(() => undefined);
  }

  private toIdentityPreloadedFile(
    target: IdentityFileTarget,
    file: File,
    src: ArrayBuffer
  ): OrtPreloadedFile {
    return {
      id: `identity-preload-${target}`,
      name: file.name,
      size: file.size,
      type: file.type,
      src,
    };
  }
  private loadInitialSurveyState(): void {
    if (this.loadingSurveyState()) return;

    this.surveyLoadError.set(null);
    this.loadingSurveyState.set(true);
    this.inscripciones
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

    this.applyInitialSurvey(resolved.initialSurvey ?? { tieneDerechoEncuesta: true });
  }

  private initializeEmptySurvey(): void {
    this.hasInitialSurveyRight.set(true);
    this.surveyState.set('no-iniciada');
    this.completedSections.set([]);
    this.activeSection.set('educacion');
    this.screen.set('propuesta');
  }

  private initializeIdentityOnlySurvey(): void {
    this.hasInitialSurveyRight.set(false);
    this.surveyState.set('completa');
    this.completedSections.set([]);
    this.activeSection.set('identidad');
    this.screen.set('propuesta');
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

    const isComplete = isBackendSurveyComplete(survey);
    const state: EstadoEncuestaInicial = isComplete ? 'completa' : 'en-progreso';
    this.surveyState.set(state);
    this.applyBackendSurvey(survey, response);

    const activeSection = isComplete
      ? 'identidad'
      : (resolveBackendSection(survey.estadoEncuestaIniAdmision) ?? 'educacion');
    const visibleSections = getSeccionesVisibles(isComplete ? 'encuesta-completa' : 'primera-vez');
    const activeIndex = visibleSections.indexOf(activeSection);

    this.completedSections.set(activeIndex > 0 ? visibleSections.slice(0, activeIndex) : []);
    this.activeSection.set(activeSection);
    this.screen.set('encuesta');
    this.loadAcademicOptionsForSurvey(survey);
  }

  private applyBackendSurvey(
    survey: InscripcionBackendSurvey,
    response: InscripcionInitialSurveyResponse
  ): void {
    const proposalType = patchBackendSurveyForms(survey, response, {
      forms: this.forms,
      careers: this.careers(),
      previousCareerOptions: this.previousCareerOptions(),
    });
    this.proposalTypeValue.set(proposalType);
  }
  private loadAcademicOptionsForSurvey(survey: InscripcionBackendSurvey): void {
    const careerId = survey.idProducto ?? null;
    const processId = survey.idProceso ?? null;
    if (careerId === null) return;

    this.catalogs
      .getComienzos(careerId)
      .pipe(
        catchError(() => of<Comienzo[]>([])),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(starts => this.startOptions.set(starts.map(start => toStartOption(start))));

    if (processId === null) return;
    this.catalogs
      .getTurnos(careerId, processId)
      .pipe(
        catchError(() => of<Turno[]>([])),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(turnos => {
        this.turnoOptions.set(turnos.map(turno => toTurnoOption(turno)));
        const selectedTurno = turnos.find(turno => turno.idTurno === survey.idTurno);
        this.academicForm.controls.turno.setValue(selectedTurno?.idOferta.toString() ?? '', {
          emitEvent: false,
        });
      });
  }

  private getInitialSurveyPayload() {
    return buildInitialSurveyPayload({
      forms: this.forms,
      previousCareerOptions: this.previousCareerOptions(),
      motivesOptions: this.motivesOptions(),
    });
  }
  private finishAt(
    screen: Extract<
      PantallaInscripcion,
      'reserva' | 'inscripcion-confirmada' | 'inscripcion-en-proceso'
    >
  ): void {
    this.screen.set(screen);
  }

  private logSubmission(submission: EnvioInscripcion): void {
    const safeLog = {
      escenario: submission.escenario,
      estadoEncuestaInicial: submission.estadoEncuestaInicial,
      propuesta: submission.propuesta,
      encuestaIncluida: submission.encuesta !== null,
      identidad: {
        frenteAdjunto: submission.identidad.frenteAdjunto,
        dorsoAdjunto: submission.identidad.dorsoAdjunto,
        selfieAdjunta: submission.identidad.selfieAdjunta,
      },
      reglamentoAceptado: submission.reglamentoAceptado,
      metodoPago: submission.metodoPago,
    };

    console.log('[Inscripciones] envío simulado', safeLog);
  }

  private loadCareers(): void {
    this.loadingCareers.set(true);
    this.catalogs
      .getCareers()
      .pipe(
        finalize(() => this.loadingCareers.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: careers => {
          this.careers.set(careers);
          const selectedCareer = careers.find(
            career => career.idProducto.toString() === this.academicForm.controls.carrera.value
          );
          if (selectedCareer && !this.academicForm.controls.tipoPropuesta.value) {
            const proposalType = getProposalOptionByLevel(selectedCareer.idNivelProducto)?.value;
            if (proposalType) {
              this.academicForm.controls.tipoPropuesta.setValue(proposalType, { emitEvent: false });
              this.proposalTypeValue.set(proposalType);
            }
          }
        },
        error: () => {
          this.catalogError.set('No se pudieron cargar las carreras.');
          this.careers.set([]);
        },
      });
  }

  private loadInitialSurveyCatalogs(): void {
    this.loadingInitialSurveyCatalogs.set(true);
    this.catalogs
      .getInitialSurveyCatalogs()
      .pipe(
        finalize(() => this.loadingInitialSurveyCatalogs.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: catalogs => {
          this.applyInitialSurveyCatalogs(catalogs);
          const response = this.initialSurveyResponse;
          if (response?.encuesta) this.applyBackendSurvey(response.encuesta, response);
        },
        error: () => {
          this.catalogError.set('No se pudieron cargar los catálogos de encuesta inicial.');
          this.applyInitialSurveyCatalogs({
            aniosAprobadosEducacionSuperior: [],
            compartidoCon: [],
            decisionCarrera: [],
            decisionUniversidad: [],
            estadoEducacionSuperior: [],
            formacionTutores: [],
            nivelConocimiento: [],
          });
        },
      });
  }

  private resetAcademicSelectionOnProposalChange(): void {
    this.academicForm.controls.tipoPropuesta.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(value => {
        this.proposalTypeValue.set(value);
        this.academicForm.controls.carrera.setValue('');
        this.startOptions.set([]);
        this.turnoOptions.set([]);
      });
  }

  private loadStartsOnCareerChange(): void {
    this.academicForm.controls.carrera.valueChanges
      .pipe(
        tap(() => {
          this.academicForm.controls.comienzo.setValue('');
          this.startOptions.set([]);
          this.turnoOptions.set([]);
        }),
        switchMap(value => {
          const careerId = toNullableNumber(value);
          if (careerId === null) {
            this.loadingStarts.set(false);
            return of<Comienzo[]>([]);
          }

          this.loadingStarts.set(true);
          return this.catalogs.getComienzos(careerId).pipe(
            catchError(() => of<Comienzo[]>([])),
            finalize(() => this.loadingStarts.set(false))
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(starts => this.startOptions.set(starts.map(start => toStartOption(start))));
  }

  private loadTurnosOnStartChange(): void {
    this.academicForm.controls.comienzo.valueChanges
      .pipe(
        tap(() => {
          this.academicForm.controls.turno.setValue('', { emitEvent: false });
          this.turnoOptions.set([]);
        }),
        switchMap(value => {
          const careerId = toNullableNumber(this.academicForm.controls.carrera.value);
          const startId = toNullableNumber(value);

          if (careerId === null || startId === null) {
            this.loadingTurnos.set(false);
            return of<Turno[]>([]);
          }

          this.loadingTurnos.set(true);
          return this.catalogs.getTurnos(careerId, startId).pipe(
            catchError(() => of<Turno[]>([])),
            finalize(() => this.loadingTurnos.set(false))
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(turnos => this.turnoOptions.set(turnos.map(turno => toTurnoOption(turno))));
  }

  private applyInitialSurveyCatalogs(catalogs: InitialSurveyCatalogs): void {
    this.previousCareerOptions.set(toCatalogOptions(catalogs.estadoEducacionSuperior));
    this.educationLevelOptions.set(toCatalogOptions(catalogs.formacionTutores));
    this.supportOptions.set(toCatalogOptions(catalogs.compartidoCon));
    this.careerDecisionOptions.set(toCatalogOptions(catalogs.decisionCarrera));
    this.motivesOptions.set(toCatalogOptions(catalogs.decisionUniversidad));
  }

  private buildProductInterestPayload(): {
    idOferta: number;
    idProcesoSeleccionado: number;
    idProducto: number;
  } | null {
    const idProducto = toNullableNumber(this.academicForm.controls.carrera.value);
    const idProcesoSeleccionado = toNullableNumber(this.academicForm.controls.comienzo.value);
    const idOferta = toNullableNumber(this.academicForm.controls.turno.value);

    return idProducto === null || idProcesoSeleccionado === null || idOferta === null
      ? null
      : { idOferta, idProcesoSeleccionado, idProducto };
  }
}
function isNotFoundError(error: unknown): error is { status: number } {
  return typeof error === 'object' && error !== null && 'status' in error && error.status === 404;
}
