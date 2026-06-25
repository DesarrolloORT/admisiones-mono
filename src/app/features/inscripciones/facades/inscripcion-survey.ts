import { computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import type {
  OrtErrorItem,
  OrtFileUploaderChange,
  OrtPreloadedFile,
} from '@desarrolloort/components';
import { merge, Observable, of } from 'rxjs';
import { finalize, switchMap } from 'rxjs/operators';

import type {
  BaccalaureateYearGroup,
  InitialSurveyCatalogs,
  LocationCountry,
} from '../../catalogs/models/catalog.interface';
import { Catalogs } from '../../catalogs/services/catalogs';
import type {
  ArchivosIdentidad,
  EstadoEncuestaInicial,
  EstadoSeccionEncuesta,
  InscripcionBackendSurvey,
  InscripcionInitialSurveyResponse,
  InscripcionStudentRegulationAcceptance,
  OpcionInscripcion,
  SeccionEncuestaId,
} from '../models/inscripcion-flow';
import {
  buildFormErrors,
  type IdentityFileTarget,
  type IdentityPreloadedFileMap,
} from '../models/inscripcion-flow-forms';
import {
  buildConfirmPreEnrollmentPayload,
  buildInitialSurveyPayload,
  isBackendSurveyComplete,
  parseDate,
  patchBackendSurveyForms,
  resolveBackendSection,
} from '../models/inscripcion-flow-mappers';
import { toCatalogOptions } from '../models/inscripcion-flow-options';
import { getSeccionesVisibles } from '../models/inscripcion-flow-policy';
import { WORK_STATUS_OPTIONS } from '../models/inscripcion-static-data';
import type { InscripcionInitialSurveyResolved } from '../resolvers/inscripcion-initial-survey.resolver';
import { Inscripciones, type InscripcionIdentityPreload } from '../services/inscripciones';
import { InscripcionFormsStore } from '../store/inscripcion-forms';
import { InscripcionProcessStore } from '../store/inscripcion-process';
import { InscripcionProposalFacade } from './inscripcion-proposal';

export class InscripcionSurveyFacade {
  private readonly catalogs = inject(Catalogs);
  private readonly inscripciones = inject(Inscripciones);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formsStore = inject(InscripcionFormsStore);
  private readonly process = inject(InscripcionProcessStore);
  private readonly proposal = inject(InscripcionProposalFacade);
  private initialSurveyResponse: InscripcionInitialSurveyResponse | null = null;
  private identityPreloadRequested = false;
  private uruguayCountryCode: number | null = null;
  private readonly identityFileTouched = new Set<IdentityFileTarget>();
  private readonly baccalaureateYears = signal<readonly BaccalaureateYearGroup[]>([]);

  public readonly educationForm = this.formsStore.educationForm;
  public readonly academicDecisionForm = this.formsStore.academicDecisionForm;
  public readonly ortExperienceForm = this.formsStore.ortExperienceForm;
  public readonly workForm = this.formsStore.workForm;
  public readonly identityForm = this.formsStore.identityForm;
  public readonly regulationForm = this.formsStore.regulationForm;
  private readonly sectionConfig = this.formsStore.sectionConfig;

  public readonly acceptedImageTypes = ['image/jpeg', 'image/png'];
  public readonly workStatusOptions = WORK_STATUS_OPTIONS;
  public readonly ratingLabels = {
    1: '1 estrella: Malo',
    2: '2 estrellas: Regular',
    3: '3 estrellas: Bueno',
    4: '4 estrellas: Muy bueno',
    5: '5 estrellas: Excelente',
  };

  private readonly completedSections = signal<readonly SeccionEncuestaId[]>([]);
  private readonly submittedSections = signal<readonly SeccionEncuestaId[]>([]);
  public readonly activeSection = signal<SeccionEncuestaId>('educacion');
  public readonly readerOpen = signal(false);
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
  public readonly hasAcceptedStudentRegulation = signal(false);
  public readonly submittedAcceptanceDate = signal<Date | null>(null);

  public readonly previousCareerOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly educationLevelOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly supportOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly careerDecisionOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly motivesOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly baccalaureateOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly orientationOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly departmentOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly institutionOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly universityOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly advertisingOptions = signal<readonly OpcionInscripcion[]>([]);

  public readonly catalogError = signal<string | null>(null);
  public readonly surveyLoadError = signal<string | null>(null);
  public readonly preEnrollmentError = signal<string | null>(null);
  public readonly loadingInitialSurveyCatalogs = signal(false);
  public readonly loadingSurveyState = signal(false);
  public readonly finalizingPreEnrollment = signal(false);
  public readonly initialized = signal(false);

  public readonly initialIdentityFiles = computed(() => {
    const files = this.preloadedIdentityFiles();
    return {
      frente: files.frente ? [files.frente] : [],
      dorso: files.dorso ? [files.dorso] : [],
      selfie: files.selfie ? [files.selfie] : [],
    };
  });
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
    if (!this.submittedSections().includes(section)) return [];
    const formErrors = buildFormErrors(
      this.sectionConfig[section].form,
      this.sectionConfig[section].errorFields
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

  constructor() {
    this.loadInitialSurveyCatalogs();
    this.loadDepartmentOptions();
    this.configureConditionalValidators();
    this.configureDependentCatalogs();
    this.observeForms();
    this.loadStudentRegulationAcceptance();
    this.loadIdentityPreloadOnIdentitySection();
    this.applyResolvedInitialSurveyState();
  }

  private configureDependentCatalogs(): void {
    this.educationForm.controls.tipoBachillerato.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.refreshOrientationOptions();
        this.updateConditionalValidators();
      });
    this.educationForm.controls.departamento.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadInstitutionsForSelectedDepartment());
  }

  public continue(): void {
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
    const nextSection = sections[sections.indexOf(section) + 1];
    if (nextSection) {
      this.activeSection.set(nextSection);
      this.process.markCheckpoint();
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
      this.completedSections().includes(section) ||
      (section !== 'identidad' && this.isSectionValid(section))
    ) {
      return 'completa';
    }
    if (this.activeSection() === section) return 'activa';
    return 'pendiente';
  }

  public isSectionPending(section: SeccionEncuestaId): boolean {
    return this.submittedSections().includes(section) && !this.isSectionValid(section);
  }

  public updateIdentityFile(target: IdentityFileTarget, event: OrtFileUploaderChange): void {
    const selectedFile = event.value.find(file => file.isValid)?.file ?? null;
    this.identityFileTouched.add(target);
    this.preloadedIdentityFiles.update(files => ({ ...files, [target]: null }));
    this.identityFiles.update(files => ({ ...files, [target]: selectedFile }));
    this.syncSectionCompletion('identidad');
  }

  public openRegulationReader(): void {
    this.readerOpen.set(true);
  }

  public acceptRegulation(): void {
    this.regulationForm.controls.aceptaReglamento.setValue(true);
    this.completeSection('reglamento');
    this.activeSection.set('reglamento');
    this.readerOpen.set(false);
    this.process.markCheckpoint();
  }

  public retryInitialSurvey(): void {
    this.loadInitialSurveyState();
  }

  public savePartial(): Observable<boolean> {
    if (!this.hasInitialSurveyRight()) return of(true);
    return this.inscripciones.saveInitialSurvey(this.getInitialSurveyPayload());
  }

  public restoreSectionState(
    activeSection: SeccionEncuestaId,
    completedSections: readonly SeccionEncuestaId[]
  ): void {
    const visible = this.visibleSections();
    this.activeSection.set(visible.includes(activeSection) ? activeSection : visible[0]);
    this.completedSections.set(
      completedSections.filter(section => section !== 'identidad' && visible.includes(section))
    );
  }

  public completedSectionIds(): readonly SeccionEncuestaId[] {
    return this.completedSections();
  }

  private finishSurveyStep(): void {
    const confirmPayload = buildConfirmPreEnrollmentPayload(this.formsStore.forms);
    if (!confirmPayload) {
      this.preEnrollmentError.set(
        'No se pudo confirmar la preinscripción con la oferta seleccionada.'
      );
      return;
    }

    this.preEnrollmentError.set(null);
    this.finalizingPreEnrollment.set(true);
    this.savePartial()
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
          this.process.preEnrollmentResponse.set(response);
          this.surveyState.set('completa');
          this.process.flow.next();
          this.process.markCheckpoint();
        },
        error: () =>
          this.preEnrollmentError.set(
            'No se pudo guardar y confirmar la preinscripción. Intentá nuevamente.'
          ),
      });
  }

  private completeSection(section: SeccionEncuestaId): void {
    this.completedSections.update(sections =>
      sections.includes(section) ? sections : [...sections, section]
    );
  }

  private syncSectionCompletion(section: SeccionEncuestaId): void {
    if (section !== 'identidad' && this.isSectionValid(section)) {
      this.completeSection(section);
      return;
    }
    if (!this.isSectionValid(section)) {
      this.completedSections.update(sections => sections.filter(item => item !== section));
    }
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

  private configureConditionalValidators(): void {
    merge(
      this.educationForm.controls.cursaSecundaria.valueChanges,
      this.educationForm.controls.lugarSecundaria.valueChanges,
      this.academicDecisionForm.controls.otrasUniversidades.valueChanges,
      this.ortExperienceForm.controls.reunionAsesoramiento.valueChanges,
      this.ortExperienceForm.controls.visitoWeb.valueChanges,
      this.ortExperienceForm.controls.recuerdaPublicidad.valueChanges
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateConditionalValidators());
    this.updateConditionalValidators();
  }

  private updateConditionalValidators(): void {
    const currentlyInSecondarySchool =
      this.educationForm.controls.cursaSecundaria.value === 'cursando';
    this.setRequired(
      [this.educationForm.controls.anioSecundaria],
      currentlyInSecondarySchool && this.careerDecisionOptions().length > 0
    );
    this.setRequired(
      [this.educationForm.controls.tipoBachillerato],
      currentlyInSecondarySchool && this.baccalaureateOptions().length > 0
    );
    this.setRequired(
      [this.educationForm.controls.orientacion],
      currentlyInSecondarySchool && this.orientationOptions().length > 0
    );
    this.setRequired(
      [this.educationForm.controls.departamento, this.educationForm.controls.institucionEducativa],
      this.educationForm.controls.lugarSecundaria.value === 'uruguay' &&
        this.departmentOptions().length > 0 &&
        this.institutionOptions().length > 0
    );
    this.setRequired(
      [this.academicDecisionForm.controls.universidadesInformadas],
      this.academicDecisionForm.controls.otrasUniversidades.value === 'si' &&
        this.universityOptions().length > 0
    );
    this.setRequired(
      [this.ortExperienceForm.controls.calificacionAsesoramiento],
      this.ortExperienceForm.controls.reunionAsesoramiento.value === 'si'
    );
    this.setRequired(
      [this.ortExperienceForm.controls.calificacionWeb],
      this.ortExperienceForm.controls.visitoWeb.value === 'si'
    );
    this.setRequired(
      [this.ortExperienceForm.controls.mediosPublicidad],
      this.ortExperienceForm.controls.recuerdaPublicidad.value === 'si' &&
        this.advertisingOptions().length > 0
    );
  }

  private setRequired(controls: readonly AbstractControl[], required: boolean): void {
    for (const control of controls) {
      control.setValidators(required ? Validators.required : null);
      control.updateValueAndValidity({ emitEvent: false });
    }
  }

  private loadStudentRegulationAcceptance(): void {
    this.inscripciones
      .getStudentRegulationAcceptance()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (acceptance: InscripcionStudentRegulationAcceptance) => {
          const accepted = acceptance.aceptoReglamentoEstudiantil === true;
          this.hasAcceptedStudentRegulation.set(accepted);
          if (!accepted) return;
          this.submittedAcceptanceDate.set(parseDate(acceptance.fechaAceptacion));
          this.regulationForm.controls.aceptaReglamento.setValue(true);
          this.completeSection('reglamento');
        },
        error: () => this.hasAcceptedStudentRegulation.set(false),
      });
  }

  private loadIdentityPreloadOnIdentitySection(): void {
    effect(() => {
      if (
        this.identityPreloadRequested ||
        this.surveyLoadError() ||
        this.process.flow.currentStep() !== 'encuesta' ||
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
    this.applyInitialSurvey(
      resolved.initialSurvey ?? {
        tieneDerechoEncuesta: true,
        encuesta: null,
        opcionesMotivosSeleccionados: null,
      }
    );
  }

  private initializeEmptySurvey(): void {
    this.hasInitialSurveyRight.set(true);
    this.surveyState.set('no-iniciada');
    this.completedSections.set([]);
    this.activeSection.set('educacion');
    this.process.flow.reset();
  }

  private initializeIdentityOnlySurvey(): void {
    this.hasInitialSurveyRight.set(false);
    this.surveyState.set('completa');
    this.completedSections.set([]);
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

    const isComplete = isBackendSurveyComplete(survey);
    this.surveyState.set(isComplete ? 'completa' : 'en-progreso');
    this.applyBackendSurvey(survey, response);

    const activeSection = isComplete
      ? 'identidad'
      : (resolveBackendSection(survey.estadoEncuestaIniAdmision) ?? 'educacion');
    const visibleSections = getSeccionesVisibles(isComplete ? 'encuesta-completa' : 'primera-vez');
    const activeIndex = visibleSections.indexOf(activeSection);
    this.completedSections.set(activeIndex > 0 ? visibleSections.slice(0, activeIndex) : []);
    this.activeSection.set(activeSection);
    this.process.flow.goTo('encuesta');
    this.proposal.loadAcademicOptionsForSurvey(survey);
  }

  private applyBackendSurvey(
    survey: InscripcionBackendSurvey,
    response: InscripcionInitialSurveyResponse
  ): void {
    const proposalType = patchBackendSurveyForms(survey, response, {
      forms: this.formsStore.forms,
      careers: this.proposal.careers(),
      previousCareerOptions: this.previousCareerOptions(),
    });
    this.proposal.setProposalType(proposalType);
    this.updateConditionalValidators();
  }

  private getInitialSurveyPayload() {
    return buildInitialSurveyPayload({
      forms: this.formsStore.forms,
      previousCareerOptions: this.previousCareerOptions(),
      motivesOptions: this.motivesOptions(),
    });
  }

  private loadInitialSurveyCatalogs(): void {
    this.loadingInitialSurveyCatalogs.set(true);
    this.catalogs
      .getInitialSurveyCatalogs()
      .pipe(
        finalize(() => {
          this.loadingInitialSurveyCatalogs.set(false);
          this.initialized.set(true);
        }),
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
            motivosEleccion: [],
            publicidadesEleccion: [],
            universidades: [],
            aniosBachiller: [],
          });
        },
      });
  }

  private applyInitialSurveyCatalogs(catalogs: InitialSurveyCatalogs): void {
    this.previousCareerOptions.set(toCatalogOptions(catalogs.estadoEducacionSuperior));
    this.educationLevelOptions.set(toCatalogOptions(catalogs.formacionTutores));
    this.supportOptions.set(toCatalogOptions(catalogs.compartidoCon));
    this.careerDecisionOptions.set(toCatalogOptions(catalogs.decisionCarrera));
    this.motivesOptions.set(toCatalogOptions(catalogs.motivosEleccion));
    this.universityOptions.set(toCatalogOptions(catalogs.universidades));
    this.advertisingOptions.set(toCatalogOptions(catalogs.publicidadesEleccion));
    this.baccalaureateYears.set(catalogs.aniosBachiller);
    this.baccalaureateOptions.set(buildBaccalaureateOptions(catalogs.aniosBachiller));
    this.refreshOrientationOptions();
    this.updateConditionalValidators();
  }

  /** Recalcula las orientaciones disponibles según el bachillerato seleccionado. */
  private refreshOrientationOptions(): void {
    const selected = this.educationForm.controls.tipoBachillerato.value;
    this.orientationOptions.set(buildOrientationOptions(this.baccalaureateYears(), selected));
    if (
      this.educationForm.controls.orientacion.value &&
      !this.orientationOptions().some(
        option => option.value === this.educationForm.controls.orientacion.value
      )
    ) {
      this.educationForm.controls.orientacion.setValue('', { emitEvent: false });
    }
  }

  private loadDepartmentOptions(): void {
    this.catalogs
      .getCountryLocations()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: countries => this.applyDepartmentOptions(countries),
        error: () => undefined,
      });
  }

  private applyDepartmentOptions(countries: readonly LocationCountry[]): void {
    const uruguay =
      countries.find(country => normalizeName(country.nombre) === 'uruguay') ??
      countries[0] ??
      null;
    this.uruguayCountryCode = uruguay?.codigoPais ?? null;
    this.departmentOptions.set(
      (uruguay?.estado ?? []).map(state => ({
        value: state.codigoEstado.toString(),
        label: state.nombre,
      }))
    );
    this.updateConditionalValidators();
  }

  private loadInstitutionsForSelectedDepartment(): void {
    const departmentValue = this.educationForm.controls.departamento.value;
    const codigoEstado = departmentValue ? Number(departmentValue) : null;
    if (this.uruguayCountryCode === null || codigoEstado === null) {
      this.institutionOptions.set([]);
      this.updateConditionalValidators();
      return;
    }
    this.catalogs
      .getInstituciones(this.uruguayCountryCode, codigoEstado)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: institutions => {
          this.institutionOptions.set(
            institutions.map(institution => ({
              value: institution.id.toString(),
              label: institution.label,
            }))
          );
          const selected = this.educationForm.controls.institucionEducativa.value;
          if (selected && !this.institutionOptions().some(option => option.value === selected)) {
            this.educationForm.controls.institucionEducativa.setValue('', { emitEvent: false });
          }
          this.updateConditionalValidators();
        },
        error: () => {
          this.institutionOptions.set([]);
          this.updateConditionalValidators();
        },
      });
  }
}

function normalizeName(value: string): string {
  return value
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .trim()
    .toLowerCase();
}

function buildBaccalaureateOptions(
  years: readonly BaccalaureateYearGroup[]
): readonly OpcionInscripcion[] {
  const seen = new Map<string, OpcionInscripcion>();
  for (const year of years) {
    for (const baccalaureate of year.baccalaureates) {
      const value = baccalaureate.id.toString();
      if (!seen.has(value)) seen.set(value, { value, label: baccalaureate.label });
    }
  }
  return [...seen.values()];
}

function buildOrientationOptions(
  years: readonly BaccalaureateYearGroup[],
  selectedBaccalaureate: string
): readonly OpcionInscripcion[] {
  if (!selectedBaccalaureate) return [];
  const seen = new Map<string, OpcionInscripcion>();
  for (const year of years) {
    for (const baccalaureate of year.baccalaureates) {
      if (baccalaureate.id.toString() !== selectedBaccalaureate || !baccalaureate.orientation) {
        continue;
      }
      const value = baccalaureate.orientation;
      if (!seen.has(value)) seen.set(value, { value, label: baccalaureate.orientation });
    }
  }
  return [...seen.values()];
}

function isNotFoundError(error: unknown): error is { status: number } {
  return typeof error === 'object' && error !== null && 'status' in error && error.status === 404;
}
