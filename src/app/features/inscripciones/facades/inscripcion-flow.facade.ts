import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import type { OrtErrorItem, OrtFileUploaderChange } from '@desarrolloort/components';
import { merge, of } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import {
  buildFormErrorSummary,
  type FormErrorField,
  ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED,
} from 'src/app/shared/forms/form-error-summary';

import {
  Career,
  CatalogItem,
  Comienzo,
  InitialSurveyCatalogs,
  Turno,
} from '../../catalogs/models/catalog.interface';
import { Catalogs } from '../../catalogs/services/catalogs';
import {
  ArchivosIdentidad,
  BorradorInscripcion,
  EnvioInscripcion,
  EstadoEncuestaInicial,
  EstadoSeccionEncuesta,
  FormularioDecisionAcademica,
  FormularioEducacion,
  FormularioExperienciaOrt,
  FormularioIdentidad,
  FormularioPago,
  FormularioPropuesta,
  FormularioReglamento,
  FormularioSituacionLaboral,
  ItemResumenInscripcion,
  METADATOS_PASOS_INSCRIPCION,
  MetodoPago,
  OpcionInscripcion,
  PantallaInscripcion,
  PasoInscripcion,
  SeccionEncuestaId,
  ValoresEncuesta,
  ValoresPropuesta,
} from '../models/inscripcion-flow';
import {
  findFirstIncompleteSection,
  getPreviousScreen,
  getResultadoPago,
  getSeccionesVisibles,
  parseEscenario,
  parseResultadoForzado,
} from '../models/inscripcion-flow-policy';
import {
  CERTAINTY_OPTIONS,
  COORDINATORS,
  DECISION_YEAR_OPTIONS,
  PAYMENT_OPTIONS,
  RESERVATION_INSTRUCTIONS,
  SECONDARY_PLACE_OPTIONS,
  SECONDARY_STATUS_OPTIONS,
  SUBJECTS,
  WORK_STATUS_OPTIONS,
  YES_NO_OPTIONS,
} from '../models/inscripcion-static-data';
import { InscripcionDraft } from '../services/inscripcion-draft';

interface SectionConfig {
  label: string;
  icon: string;
  form: FormGroup;
  errorFields: FormErrorField[];
}

export class InscripcionFlowFacade {
  private readonly catalogs = inject(Catalogs);
  private readonly draftStorage = inject(InscripcionDraft);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private processingTimer: ReturnType<typeof setTimeout> | null = null;
  private restoring = true;

  public readonly secondaryStatusOptions = SECONDARY_STATUS_OPTIONS;
  public readonly secondaryPlaceOptions = SECONDARY_PLACE_OPTIONS;
  public readonly workStatusOptions = WORK_STATUS_OPTIONS;
  public readonly yesNoOptions = YES_NO_OPTIONS;
  public readonly decisionYearOptions = DECISION_YEAR_OPTIONS;
  public readonly certaintyOptions = CERTAINTY_OPTIONS;
  public readonly paymentOptions = PAYMENT_OPTIONS;
  public readonly coordinators = COORDINATORS;
  public readonly paymentDeadline = '04/03/2027';
  public readonly studentNumber = '397654';
  public readonly inscriptionAmount = '$ 15.500';
  public readonly acceptedImageTypes = ['image/jpeg', 'image/png'];

  public readonly academicForm = new FormGroup<FormularioPropuesta>({
    tipoPropuesta: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    carrera: new FormControl('', { nonNullable: true, validators: Validators.required }),
    comienzo: new FormControl('', { nonNullable: true, validators: Validators.required }),
    turno: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });

  public readonly educationForm = new FormGroup<FormularioEducacion>({
    cursaSecundaria: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    lugarSecundaria: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    estadoEducacionSuperior: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    formacionMadre: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    formacionPadre: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
  });

  public readonly academicDecisionForm = new FormGroup<FormularioDecisionAcademica>({
    anioDecisionCarrera: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    apoyoDecision: new FormControl('', { nonNullable: true, validators: Validators.required }),
    anioDecisionOrt: new FormControl('', { nonNullable: true, validators: Validators.required }),
    otrasUniversidades: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    certezaDecision: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    motivosOrt: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });

  public readonly ortExperienceForm = new FormGroup<FormularioExperienciaOrt>({
    reunionAsesoramiento: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    visitoWeb: new FormControl('', { nonNullable: true, validators: Validators.required }),
    visitoSede: new FormControl('', { nonNullable: true, validators: Validators.required }),
    recuerdaPublicidad: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
  });

  public readonly workForm = new FormGroup<FormularioSituacionLaboral>({
    situacionLaboral: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
  });

  public readonly identityForm = new FormGroup<FormularioIdentidad>({
    vencimientoDocumento: new FormControl<Date | null>(null, Validators.required),
  });

  public readonly regulationForm = new FormGroup<FormularioReglamento>({
    aceptaReglamento: new FormControl(false, {
      nonNullable: true,
      validators: Validators.requiredTrue,
    }),
  });

  public readonly paymentForm = new FormGroup<FormularioPago>({
    metodoPago: new FormControl<MetodoPago | ''>('', {
      nonNullable: true,
      validators: Validators.required,
    }),
  });

  private readonly sectionConfig: Record<SeccionEncuestaId, SectionConfig> = {
    educacion: {
      label: 'Educación',
      icon: 'menu_book',
      form: this.educationForm,
      errorFields: [
        { controlName: 'cursaSecundaria', fieldId: '', label: 'Situación de secundaria' },
        { controlName: 'lugarSecundaria', fieldId: '', label: 'Lugar de secundaria' },
        {
          controlName: 'estadoEducacionSuperior',
          fieldId: '',
          label: 'Estado de educación superior',
        },
        { controlName: 'formacionMadre', fieldId: '', label: 'Formación de madre o tutor' },
        { controlName: 'formacionPadre', fieldId: '', label: 'Formación de padre o tutor' },
      ],
    },
    'decision-academica': {
      label: 'Decisión académica',
      icon: 'schema',
      form: this.academicDecisionForm,
      errorFields: [
        { controlName: 'anioDecisionCarrera', fieldId: '', label: 'Año de decisión de carrera' },
        { controlName: 'apoyoDecision', fieldId: '', label: 'Apoyo en la decisión' },
        { controlName: 'anioDecisionOrt', fieldId: '', label: 'Año de decisión de ORT' },
        { controlName: 'otrasUniversidades', fieldId: '', label: 'Otras universidades' },
        { controlName: 'certezaDecision', fieldId: '', label: 'Certeza de la decisión' },
        { controlName: 'motivosOrt', fieldId: '', label: 'Motivos para elegir ORT' },
      ],
    },
    'experiencia-ort': {
      label: 'Experiencia con ORT',
      icon: 'domain',
      form: this.ortExperienceForm,
      errorFields: [
        {
          controlName: 'reunionAsesoramiento',
          fieldId: '',
          label: 'Reunión de asesoramiento',
        },
        { controlName: 'visitoWeb', fieldId: '', label: 'Visita al sitio web' },
        { controlName: 'visitoSede', fieldId: '', label: 'Visita a instalaciones' },
        { controlName: 'recuerdaPublicidad', fieldId: '', label: 'Publicidad de ORT' },
      ],
    },
    'situacion-laboral': {
      label: 'Situación laboral',
      icon: 'business_center',
      form: this.workForm,
      errorFields: [{ controlName: 'situacionLaboral', fieldId: '', label: 'Situación laboral' }],
    },
    identidad: {
      label: 'Verificación de identidad',
      icon: 'verified',
      form: this.identityForm,
      errorFields: [
        {
          controlName: 'vencimientoDocumento',
          fieldId: '',
          label: 'Vencimiento del documento',
        },
      ],
    },
    reglamento: {
      label: 'Reglamento estudiantil',
      icon: 'article',
      form: this.regulationForm,
      errorFields: [
        { controlName: 'aceptaReglamento', fieldId: '', label: 'Aceptación del reglamento' },
      ],
    },
  };

  private readonly careers = signal<readonly Career[]>([]);
  private readonly proposalTypeValue = toSignal(
    this.academicForm.controls.tipoPropuesta.valueChanges,
    { initialValue: this.academicForm.controls.tipoPropuesta.value }
  );
  private readonly completedSections = signal<readonly SeccionEncuestaId[]>([]);
  private readonly submittedSections = signal<readonly SeccionEncuestaId[]>([]);
  private readonly submittedAcademic = signal(false);
  private readonly submittedPayment = signal(false);

  public readonly scenario = parseEscenario(this.route.snapshot.queryParamMap.get('escenario'));
  public readonly forcedResult = parseResultadoForzado(
    this.route.snapshot.queryParamMap.get('resultado')
  );
  public readonly screen = signal<PantallaInscripcion>('propuesta');
  public readonly activeSection = signal<SeccionEncuestaId>(
    this.scenario === 'encuesta-completa' ? 'identidad' : 'educacion'
  );
  public readonly identityFiles = signal<ArchivosIdentidad>({
    frente: null,
    dorso: null,
    selfie: null,
  });
  public readonly exitConfirmationOpen = signal(false);
  public readonly showAllSubjects = signal(false);
  public readonly selectedPaymentMethod = signal<MetodoPago | null>(null);

  public readonly startOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly turnoOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly previousCareerOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly educationLevelOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly supportOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly motivesOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly knowledgeOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly catalogError = signal<string | null>(null);

  public readonly visibleSections = computed(() => getSeccionesVisibles(this.scenario));
  public readonly surveyState = computed<EstadoEncuestaInicial>(() => {
    if (this.scenario === 'encuesta-completa') {
      return 'completa';
    }

    return this.completedSections().length === 0 ? 'no-iniciada' : 'en-progreso';
  });
  public readonly sectionItems = computed(() =>
    this.visibleSections().map(section => ({
      id: section,
      label: this.sectionConfig[section].label,
      icon: this.sectionConfig[section].icon,
      state: this.getSectionState(section),
    }))
  );
  public readonly proposalOptions = computed<readonly OpcionInscripcion[]>(() => {
    const levels = new Map<number, string>();

    for (const career of this.careers()) {
      if (career.idNivelProducto && career.nombreNivelProducto) {
        levels.set(career.idNivelProducto, career.nombreNivelProducto);
      }
    }

    return Array.from(levels, ([value, label]) => ({
      value: value.toString(),
      label,
      icon: this.getProposalIcon(label),
    }));
  });
  public readonly careerOptions = computed<readonly OpcionInscripcion[]>(() => {
    const proposalType = this.proposalTypeValue();

    return this.careers()
      .filter(career => !proposalType || career.idNivelProducto.toString() === proposalType)
      .map(career => ({
        value: career.idProducto.toString(),
        label: career.nombreProducto,
      }));
  });
  public readonly isTerminal = computed(() =>
    ['reserva', 'inscripcion-confirmada', 'inscripcion-en-proceso'].includes(this.screen())
  );
  public readonly showStepper = computed(() =>
    ['propuesta', 'encuesta', 'lector-reglamento', 'pago', 'confirmacion-pago'].includes(
      this.screen()
    )
  );
  public readonly canGoBack = computed(
    () =>
      getPreviousScreen(this.screen(), this.activeSection(), this.visibleSections()) !== null &&
      this.screen() !== 'confirmacion-pago'
  );
  public readonly stepNumber = computed<1 | 2 | 3>(() => {
    const screen = this.screen();
    if (screen === 'propuesta') return 1;
    if (screen === 'encuesta' || screen === 'lector-reglamento') return 2;
    return 3;
  });
  public readonly stepSupportLabel = computed(() => {
    const number = this.stepNumber();
    if (number === 1) return METADATOS_PASOS_INSCRIPCION.propuesta.supportLabel;
    if (number === 2) return METADATOS_PASOS_INSCRIPCION.encuesta.supportLabel;
    return METADATOS_PASOS_INSCRIPCION.pago.supportLabel;
  });
  public readonly stepperSteps = computed<PasoInscripcion[]>(() => [
    {
      id: 'propuesta',
      title: 'Propuesta académica',
      overline: 'Paso 1',
      status: this.stepNumber() > 1 ? 'completo' : 'actual',
    },
    {
      id: 'encuesta',
      title: 'Información personal',
      overline: 'Paso 2',
      status: this.stepNumber() > 2 ? 'completo' : this.stepNumber() === 2 ? 'actual' : 'pendiente',
    },
    {
      id: 'pago',
      title: 'Confirmación',
      overline: 'Paso 3',
      status: this.stepNumber() === 3 ? 'actual' : 'pendiente',
    },
  ]);
  public readonly summaryItems = computed<ItemResumenInscripcion[]>(() => [
    {
      icon: 'school',
      label: 'Carrera',
      value: this.getOptionLabel(
        this.careerOptions(),
        this.academicForm.controls.carrera.value,
        'Sin seleccionar'
      ),
    },
    {
      icon: 'calendar_today',
      label: 'Comienzo',
      value: this.getOptionLabel(
        this.startOptions(),
        this.academicForm.controls.comienzo.value,
        'Sin seleccionar'
      ),
    },
    {
      icon: 'schedule',
      label: 'Turno',
      value: this.getOptionLabel(
        this.turnoOptions(),
        this.academicForm.controls.turno.value,
        'Sin seleccionar'
      ),
    },
  ]);
  public readonly visibleSubjects = computed(() =>
    this.showAllSubjects() ? SUBJECTS : SUBJECTS.slice(0, 4)
  );
  public readonly subjectsToggleLabel = computed(() =>
    this.showAllSubjects() ? 'Ver menos materias' : 'Ver todas las materias'
  );
  public readonly reservationInstructions = computed(() => {
    const method = this.selectedPaymentMethod();
    return method === 'paganza' || method === 'banred' || method === 'abitab'
      ? RESERVATION_INSTRUCTIONS[method]
      : RESERVATION_INSTRUCTIONS.abitab;
  });
  public readonly academicErrors = computed<OrtErrorItem[]>(() =>
    this.submittedAcademic()
      ? buildFormErrorSummary(
          this.academicForm,
          [
            { controlName: 'tipoPropuesta', fieldId: '', label: 'Propuesta académica' },
            { controlName: 'carrera', fieldId: '', label: 'Carrera' },
            { controlName: 'comienzo', fieldId: '', label: 'Comienzo' },
            { controlName: 'turno', fieldId: '', label: 'Turno' },
          ],
          ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED
        )
      : []
  );
  public readonly activeSectionErrors = computed<OrtErrorItem[]>(() => {
    const section = this.activeSection();
    if (!this.submittedSections().includes(section)) return [];
    const config = this.sectionConfig[section];

    const formErrors = buildFormErrorSummary(
      config.form,
      config.errorFields,
      ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED
    );

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
      ? buildFormErrorSummary(
          this.paymentForm,
          [{ controlName: 'metodoPago', fieldId: '', label: 'Medio de pago' }],
          ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED
        )
      : []
  );

  constructor() {
    this.loadCareers();
    this.loadInitialSurveyCatalogs();
    this.resetAcademicSelectionOnProposalChange();
    this.loadStartsOnCareerChange();
    this.loadTurnosOnStartChange();
    this.restoreDraft();
    this.observeForms();
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

    this.saveDraft();
  }

  public openSection(section: SeccionEncuestaId): void {
    if (!this.visibleSections().includes(section)) return;
    this.activeSection.set(section);
    this.saveDraft();
  }

  public getSectionState(section: SeccionEncuestaId): EstadoSeccionEncuesta {
    if (this.activeSection() === section) return 'activa';
    return this.completedSections().includes(section) ? 'completa' : 'pendiente';
  }

  public updateIdentityFile(target: keyof ArchivosIdentidad, event: OrtFileUploaderChange): void {
    const selectedFile = event.value.find(file => file.isValid)?.file ?? null;
    this.identityFiles.update(files => ({ ...files, [target]: selectedFile }));
    this.invalidateSectionIfNeeded('identidad');
    this.saveDraft();
  }

  public openRegulationReader(): void {
    this.screen.set('lector-reglamento');
    this.saveDraft();
  }

  public acceptRegulation(): void {
    this.regulationForm.controls.aceptaReglamento.setValue(true);
    this.completeSection('reglamento');
    this.screen.set('encuesta');
    this.activeSection.set('reglamento');
    this.saveDraft();
  }

  public requestPaymentConfirmation(): void {
    this.submittedPayment.set(true);
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    this.screen.set('confirmacion-pago');
    this.saveDraft();
  }

  public cancelPaymentConfirmation(): void {
    if (this.screen() !== 'confirmacion-pago') return;

    this.screen.set('pago');
    this.saveDraft();
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
    this.exitConfirmationOpen.set(true);
  }

  public cancelExit(): void {
    this.exitConfirmationOpen.set(false);
  }

  public confirmExit(): void {
    this.saveDraft();
    this.exitConfirmationOpen.set(false);
    void this.router.navigateByUrl('/inicio');
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
    return {
      escenario: this.scenario,
      estadoEncuestaInicial: this.surveyState(),
      propuesta: this.academicForm.getRawValue(),
      encuesta: this.scenario === 'encuesta-completa' ? null : this.getSurveyValues(),
      identidad: {
        vencimientoDocumento: this.serializeDate(
          this.identityForm.controls.vencimientoDocumento.value
        ),
        frenteAdjunto: files.frente !== null,
        dorsoAdjunto: files.dorso !== null,
        selfieAdjunta: files.selfie !== null,
      },
      reglamentoAceptado: this.regulationForm.controls.aceptaReglamento.value,
      metodoPago: method,
    };
  }

  private submitAcademic(): void {
    this.submittedAcademic.set(true);
    if (this.academicForm.invalid) {
      this.academicForm.markAllAsTouched();
      return;
    }

    this.screen.set('encuesta');
    this.activeSection.set(
      findFirstIncompleteSection(this.visibleSections(), this.completedSections())
    );
    this.saveDraft();
  }

  private submitActiveSection(): void {
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
      this.screen.set('pago');
    }

    this.saveDraft();
  }

  private completeSection(section: SeccionEncuestaId): void {
    this.completedSections.update(sections =>
      sections.includes(section) ? sections : [...sections, section]
    );
  }

  private invalidateSectionIfNeeded(section: SeccionEncuestaId): void {
    if (this.isSectionValid(section)) return;
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
        for (const section of this.visibleSections()) {
          this.invalidateSectionIfNeeded(section);
        }
        this.saveDraft();
      });
  }

  private restoreDraft(): void {
    const draft = this.draftStorage.load(this.scenario);

    if (draft) {
      this.applyDraft(draft);
    } else if (this.scenario === 'parcial') {
      this.applyDraft(this.getPartialScenarioDraft());
    }

    this.restoring = false;
  }

  private applyDraft(draft: BorradorInscripcion): void {
    this.academicForm.patchValue(draft.propuesta, { emitEvent: false });
    this.educationForm.patchValue(draft.encuesta.educacion, { emitEvent: false });
    this.academicDecisionForm.patchValue(draft.encuesta.decisionAcademica, {
      emitEvent: false,
    });
    this.ortExperienceForm.patchValue(draft.encuesta.experienciaOrt, { emitEvent: false });
    this.workForm.patchValue(draft.encuesta.situacionLaboral, { emitEvent: false });
    this.identityForm.patchValue(
      {
        vencimientoDocumento: this.parseDate(draft.identidad.vencimientoDocumento),
      },
      { emitEvent: false }
    );
    this.regulationForm.patchValue(draft.reglamento, { emitEvent: false });
    this.paymentForm.patchValue(draft.pago, { emitEvent: false });
    const completedSections = draft.seccionesCompletas.filter(
      section => this.visibleSections().includes(section) && section !== 'identidad'
    );
    const requiresIdentityReattachment =
      draft.seccionesCompletas.includes('identidad') &&
      (draft.pantalla === 'lector-reglamento' ||
        draft.pantalla === 'pago' ||
        draft.pantalla === 'confirmacion-pago' ||
        draft.seccionActiva === 'reglamento');

    this.completedSections.set(completedSections);
    this.screen.set(requiresIdentityReattachment ? 'encuesta' : draft.pantalla);
    this.activeSection.set(
      requiresIdentityReattachment
        ? 'identidad'
        : this.visibleSections().includes(draft.seccionActiva)
          ? draft.seccionActiva
          : findFirstIncompleteSection(this.visibleSections(), completedSections)
    );
    this.loadAcademicOptionsForDraft(draft.propuesta);
  }

  private getPartialScenarioDraft(): BorradorInscripcion {
    return {
      version: 1,
      escenario: 'parcial',
      pantalla: 'encuesta',
      seccionActiva: 'decision-academica',
      seccionesCompletas: ['educacion'],
      propuesta: {
        tipoPropuesta: '1',
        carrera: '20',
        comienzo: '200',
        turno: '300',
      },
      encuesta: {
        educacion: {
          cursaSecundaria: 'cursando',
          lugarSecundaria: 'uruguay',
          estadoEducacionSuperior: '3',
          formacionMadre: '4',
          formacionPadre: '4',
        },
        decisionAcademica: {
          anioDecisionCarrera: '',
          apoyoDecision: '',
          anioDecisionOrt: '',
          otrasUniversidades: '',
          certezaDecision: '',
          motivosOrt: '',
        },
        experienciaOrt: {
          reunionAsesoramiento: '',
          visitoWeb: '',
          visitoSede: '',
          recuerdaPublicidad: '',
        },
        situacionLaboral: { situacionLaboral: '' },
      },
      identidad: { vencimientoDocumento: '' },
      reglamento: { aceptaReglamento: false },
      pago: { metodoPago: '' },
    };
  }

  private saveDraft(): void {
    if (this.restoring || this.isTerminal() || this.screen() === 'procesando') return;

    const currentScreen = this.screen();
    const draftScreen: BorradorInscripcion['pantalla'] =
      currentScreen === 'lector-reglamento' ||
      currentScreen === 'confirmacion-pago' ||
      currentScreen === 'pago' ||
      currentScreen === 'encuesta'
        ? currentScreen
        : 'propuesta';

    this.draftStorage.save({
      version: 1,
      escenario: this.scenario,
      pantalla: draftScreen,
      seccionActiva: this.activeSection(),
      seccionesCompletas: [...this.completedSections()],
      propuesta: this.academicForm.getRawValue(),
      encuesta: this.getSurveyValues(),
      identidad: {
        vencimientoDocumento: this.serializeDate(
          this.identityForm.controls.vencimientoDocumento.value
        ),
      },
      reglamento: this.regulationForm.getRawValue(),
      pago: this.paymentForm.getRawValue(),
    });
  }

  private getSurveyValues(): ValoresEncuesta {
    return {
      educacion: this.educationForm.getRawValue(),
      decisionAcademica: this.academicDecisionForm.getRawValue(),
      experienciaOrt: this.ortExperienceForm.getRawValue(),
      situacionLaboral: this.workForm.getRawValue(),
    };
  }

  private serializeDate(value: Date | null): string {
    if (!value) return '';

    const year = value.getFullYear();
    const month = `${value.getMonth() + 1}`.padStart(2, '0');
    const day = `${value.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private parseDate(value: string): Date | null {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
    if (!match) return null;

    const year = Number(match[1]);
    const month = Number(match[2]) - 1;
    const day = Number(match[3]);
    const date = new Date(year, month, day);

    return date.getFullYear() === year && date.getMonth() === month && date.getDate() === day
      ? date
      : null;
  }

  private finishAt(
    screen: Extract<
      PantallaInscripcion,
      'reserva' | 'inscripcion-confirmada' | 'inscripcion-en-proceso'
    >
  ): void {
    this.screen.set(screen);
    this.draftStorage.clear(this.scenario);
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
    this.catalogs
      .getCareers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: careers => this.careers.set(careers),
        error: () => {
          this.catalogError.set('No se pudieron cargar las carreras.');
          this.careers.set([]);
        },
      });
  }

  private loadInitialSurveyCatalogs(): void {
    this.catalogs
      .getInitialSurveyCatalogs()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: catalogs => this.applyInitialSurveyCatalogs(catalogs),
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
      .subscribe(() => {
        this.academicForm.patchValue(
          { carrera: '', comienzo: '', turno: '' },
          { emitEvent: false }
        );
        this.startOptions.set([]);
        this.turnoOptions.set([]);
      });
  }

  private loadStartsOnCareerChange(): void {
    this.academicForm.controls.carrera.valueChanges
      .pipe(
        tap(() => {
          this.academicForm.patchValue({ comienzo: '', turno: '' }, { emitEvent: false });
          this.startOptions.set([]);
          this.turnoOptions.set([]);
        }),
        switchMap(value => {
          const careerId = this.toNullableNumber(value);
          return careerId === null
            ? of<Comienzo[]>([])
            : this.catalogs.getComienzos(careerId).pipe(catchError(() => of<Comienzo[]>([])));
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(starts => this.startOptions.set(starts.map(start => this.toStartOption(start))));
  }

  private loadTurnosOnStartChange(): void {
    this.academicForm.controls.comienzo.valueChanges
      .pipe(
        tap(() => {
          this.academicForm.controls.turno.setValue('', { emitEvent: false });
          this.turnoOptions.set([]);
        }),
        switchMap(value => {
          const careerId = this.toNullableNumber(this.academicForm.controls.carrera.value);
          const startId = this.toNullableNumber(value);

          return careerId === null || startId === null
            ? of<Turno[]>([])
            : this.catalogs.getTurnos(careerId, startId).pipe(catchError(() => of<Turno[]>([])));
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(turnos => this.turnoOptions.set(turnos.map(turno => this.toTurnoOption(turno))));
  }

  private loadAcademicOptionsForDraft(values: ValoresPropuesta): void {
    const careerId = this.toNullableNumber(values.carrera);
    const startId = this.toNullableNumber(values.comienzo);
    if (careerId === null) return;

    this.catalogs
      .getComienzos(careerId)
      .pipe(
        catchError(() => of<Comienzo[]>([])),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(starts => this.startOptions.set(starts.map(start => this.toStartOption(start))));

    if (startId === null) return;
    this.catalogs
      .getTurnos(careerId, startId)
      .pipe(
        catchError(() => of<Turno[]>([])),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(turnos => this.turnoOptions.set(turnos.map(turno => this.toTurnoOption(turno))));
  }

  private applyInitialSurveyCatalogs(catalogs: InitialSurveyCatalogs): void {
    this.previousCareerOptions.set(this.toCatalogOptions(catalogs.estadoEducacionSuperior));
    this.educationLevelOptions.set(this.toCatalogOptions(catalogs.formacionTutores));
    this.supportOptions.set(this.toCatalogOptions(catalogs.compartidoCon));
    this.motivesOptions.set(this.toCatalogOptions(catalogs.decisionUniversidad));
    this.knowledgeOptions.set(this.toCatalogOptions(catalogs.nivelConocimiento));
  }

  private toCatalogOptions(items: CatalogItem[]): OpcionInscripcion[] {
    return items.map(item => ({ value: item.id.toString(), label: item.label }));
  }

  private toStartOption(start: Comienzo): OpcionInscripcion {
    return { value: start.idProceso.toString(), label: start.nombreProceso };
  }

  private toTurnoOption(turno: Turno): OpcionInscripcion {
    return {
      value: turno.idOferta.toString(),
      label: turno.horarioReferencia
        ? `${turno.nombreTurno} (${turno.horarioReferencia})`
        : turno.nombreTurno,
    };
  }

  private toNullableNumber(value: string): number | null {
    if (!value) return null;
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  private getProposalIcon(label: string): string {
    const normalized = label
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase();

    if (normalized.includes('tecn')) return 'list_alt';
    if (normalized.includes('actualizacion')) return 'how_to_reg';
    return 'school';
  }

  private getOptionLabel(
    options: readonly OpcionInscripcion[],
    value: string,
    fallback: string
  ): string {
    return options.find(option => option.value === value)?.label ?? fallback;
  }
}
