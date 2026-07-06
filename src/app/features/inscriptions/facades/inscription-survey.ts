import { computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import type {
  OrtErrorItem,
  OrtFileUploaderChange,
  OrtPreloadedFile,
} from '@desarrolloort/components';
import { forkJoin, merge, Observable, of } from 'rxjs';
import { catchError, finalize, map, switchMap } from 'rxjs/operators';
import {
  DEFAULT_ERROR_ALERT,
  type ErrorAlertState,
} from 'src/app/shared/ui/error-alert/error-alert';

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
  InscripcionInitialSurvey,
  InscripcionInitialSurveyResponse,
  InscripcionStudentRegulationAcceptance,
  OpcionInscripcion,
  SeccionEncuestaId,
} from '../models/inscription-flow';
import {
  buildFormErrors,
  type IdentityFileTarget,
  type IdentityPreloadedFileMap,
} from '../models/inscription-flow-forms';
import {
  buildConfirmPreEnrollmentPayload,
  buildInitialSurveyPayload,
  hasCompleteUniversityEducation,
  parseDate,
  patchBackendSurveyForms,
  serializeDate,
} from '../models/inscription-flow-mappers';
import { toCatalogOptions } from '../models/inscription-flow-options';
import { getSeccionesVisibles } from '../models/inscription-flow-policy';
import type { InscripcionInitialSurveyResolved } from '../resolvers/inscription-initial-survey.resolver';
import { Inscripciones, type InscripcionIdentityPreload } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionProposalFacade } from './inscription-proposal';

const URUGUAY_COUNTRY_CODE = 1;
const IDENTITY_SAVE_ERROR = 'identity-save';

export class InscripcionSurveyFacade {
  private readonly catalogs = inject(Catalogs);
  private readonly inscriptions = inject(Inscripciones);
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
  public readonly ratingLabels = signal<Record<number, string>>({
    1: '1 estrella: Malo',
    2: '2 estrellas: Regular',
    3: '3 estrellas: Bueno',
    4: '4 estrellas: Muy bueno',
    5: '5 estrellas: Excelente',
  });

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
  public readonly requiresIdentityConfirmation = signal(false);
  public readonly hasAcceptedStudentRegulation = signal(false);
  public readonly submittedAcceptanceDate = signal<Date | null>(null);

  public readonly previousCareerOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly educationLevelOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly supportOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly decisionYearOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly decisionLevelOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly motivesOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly schoolYearOptions = computed<readonly OpcionInscripcion[]>(() =>
    this.baccalaureateYears().map(year => ({ value: year.id.toString(), label: year.label }))
  );
  public readonly orientationOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly schoolPlaceOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly departmentOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly institutionOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly universityOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly higherEducationUniversityOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly advertisingOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly workScheduleOptions = signal<readonly OpcionInscripcion[]>([]);

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
  public readonly activeSectionErrorAlert = computed<ErrorAlertState | null>(() =>
    this.activeSectionErrors().length > 0 ? DEFAULT_ERROR_ALERT : null
  );

  constructor() {
    this.loadInitialSurveyCatalogs();
    this.loadDepartmentOptions();
    this.configureConditionalValidators();
    this.configureDependentCatalogs();
    this.observeForms();
    this.observeIdentityConfirmation();
    this.loadStudentRegulationAcceptance();
    this.loadIdentityPreloadOnIdentitySection();
    this.applyResolvedInitialSurveyState();
  }

  private configureDependentCatalogs(): void {
    this.educationForm.controls.anioSecundaria.valueChanges
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
    const nextSection = this.findNextInvalidSection(section);
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
    const selected =
      event.value.find(file => file.isValid && !file.isPreloaded) ??
      event.value.find(file => file.isValid) ??
      null;
    const selectedFile = selected?.file ?? null;
    const currentFile = this.identityFiles()[target];

    if (
      selected?.isPreloaded &&
      currentFile &&
      selectedFile &&
      currentFile.name === selectedFile.name &&
      currentFile.size === selectedFile.size &&
      currentFile.type === selectedFile.type
    ) {
      return;
    }

    this.identityFileTouched.add(target);
    this.preloadedIdentityFiles.update(files => ({ ...files, [target]: null }));
    this.identityFiles.update(files => ({ ...files, [target]: selectedFile }));
    this.syncSectionCompletion('identidad');
  }

  public isIdentityFileMissing(target: IdentityFileTarget): boolean {
    return this.submittedSections().includes('identidad') && !this.identityFiles()[target];
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
      this.orientationOptions().length > 0
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
    return this.inscriptions.saveInitialSurvey(this.getInitialSurveyPayload());
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
    this.saveIdentityChanges()
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
        error: error => {
          const identitySaveFailed =
            error instanceof Error && error.message === IDENTITY_SAVE_ERROR;
          if (identitySaveFailed) {
            this.completedSections.update(sections =>
              sections.filter(section => section !== 'identidad')
            );
            this.markSectionSubmitted('identidad');
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

    this.markSectionSubmitted(invalidSection);
    this.activeSection.set(invalidSection);
    this.sectionConfig[invalidSection].form.markAllAsTouched();
    this.preEnrollmentError.set(
      'Completá la información pendiente antes de confirmar la preinscripción.'
    );
    return false;
  }

  private markSectionSubmitted(section: SeccionEncuestaId): void {
    this.submittedSections.update(sections =>
      sections.includes(section) ? sections : [...sections, section]
    );
  }

  private saveIdentityChanges(): Observable<boolean> {
    const files = this.identityFiles();
    const expiration = serializeDate(this.identityForm.controls.vencimientoDocumento.value);
    const uploads: Observable<boolean>[] = [];

    if (
      expiration &&
      files.frente &&
      files.dorso &&
      (this.identityFileTouched.has('frente') ||
        this.identityFileTouched.has('dorso') ||
        this.identityForm.controls.vencimientoDocumento.dirty)
    ) {
      uploads.push(
        this.inscriptions.uploadIdentityDocument({
          fecha: expiration,
          frente: files.frente,
          dorso: files.dorso,
        })
      );
    }

    if (files.selfie && this.identityFileTouched.has('selfie')) {
      uploads.push(this.inscriptions.uploadIdentityPhoto(files.selfie));
    }

    return uploads.length === 0
      ? of(true)
      : forkJoin(uploads).pipe(map(results => results.every(Boolean)));
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

  private observeIdentityConfirmation(): void {
    this.identityForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      const confirmed = this.identityForm.controls.identidadCorrecta.value;
      if (!confirmed || !this.requiresIdentityConfirmation() || !this.isSectionValid('identidad')) {
        return;
      }

      this.completeSection('identidad');
      if (this.activeSection() !== 'identidad') return;

      const sections = this.visibleSections();
      const nextSection = sections[sections.indexOf('identidad') + 1];
      if (nextSection) this.activeSection.set(nextSection);
      this.process.markCheckpoint();
    });
  }

  private configureConditionalValidators(): void {
    merge(
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

    this.setRequired(
      education.anioSecundaria,
      currentlyInSchool && this.schoolYearOptions().length > 0
    );
    this.setRequired(education.orientacion, this.shouldAskBaccalaureateOrientation());
    this.setRequired(education.vecesRecursaAnioBachillerato, this.shouldAskRecursaCount(), [
      Validators.required,
      Validators.min(1),
    ]);
    this.setRequired(
      education.departamento,
      this.isNationalSchoolPlace() && this.departmentOptions().length > 0
    );
    this.setRequired(
      education.institucionEducativa,
      (this.isNationalSchoolPlace() && this.institutionOptions().length > 0) ||
        this.isForeignSchoolPlace()
    );
    this.setRequired(
      education.universidadesEducacionSuperior,
      this.shouldAskHigherEducationUniversities() &&
        this.higherEducationUniversityOptions().length > 0
    );
    this.setRequired(
      education.universidadEducacionSuperiorOtro,
      this.shouldAskHigherEducationOtherUniversity()
    );
    this.setRequired(education.tituloOrtMadre, this.shouldAskMotherOrtDegree());
    this.setRequired(education.tituloOrtPadre, this.shouldAskFatherOrtDegree());

    this.setRequired(
      decision.universidadesInformadas,
      decision.otrasUniversidades.value === 'si' && this.universityOptions().length > 0
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
      experience.recuerdaPublicidad.value === 'si' && this.advertisingOptions().length > 0
    );

    this.setRequired(
      work.tipoJornadaLaboral,
      work.situacionLaboral.value === 'trabaja' && this.workScheduleOptions().length > 0
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
      this.inscriptions
        .getIdentityPreload()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: preload => this.applyIdentityPreload(preload),
          error: () => undefined,
        });
    });
  }

  private setIdentityConfirmationRequired(required: boolean): void {
    this.requiresIdentityConfirmation.set(required);
    const control = this.identityForm.controls.identidadCorrecta;
    control.setValidators(required ? Validators.requiredTrue : null);
    if (!required) control.setValue(false, { emitEvent: false });
    control.updateValueAndValidity({ emitEvent: false });
  }

  private applyIdentityPreload(preload: InscripcionIdentityPreload): void {
    const expiration = parseDate(preload.fechaVencimiento);
    this.setIdentityConfirmationRequired(
      !!preload.frente && !!preload.dorso && !!preload.selfie && !!expiration
    );

    this.applyPreloadedIdentityFile('frente', preload.frente);
    this.applyPreloadedIdentityFile('dorso', preload.dorso);
    this.applyPreloadedIdentityFile('selfie', preload.selfie);

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

    const isComplete = survey.completa;
    this.surveyState.set(isComplete ? 'completa' : 'en-progreso');
    this.applyBackendSurvey(survey, response);

    const activeSection = isComplete ? 'identidad' : (survey.seccionActiva ?? 'educacion');
    const visibleSections = getSeccionesVisibles(isComplete ? 'encuesta-completa' : 'primera-vez');
    const activeIndex = visibleSections.indexOf(activeSection);
    this.completedSections.set(activeIndex > 0 ? visibleSections.slice(0, activeIndex) : []);
    this.activeSection.set(activeSection);
    this.process.flow.goTo('encuesta');
    this.proposal.loadAcademicOptionsForSurvey(survey);
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
    this.refreshOrientationOptions();
    this.updateConditionalValidators();
  }

  private getInitialSurveyPayload() {
    return buildInitialSurveyPayload(this.formsStore.forms);
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
            educacion: {
              ubicacionesUltimoAnioSecundaria: [],
              aniosBachillerato: [],
              estadosEducacionSuperiorPrevia: [],
              universidades: [],
              nivelesFormacionTutores: [],
            },
            decisionAcademica: {
              aniosEducacionMediaSuperior: [],
              apoyosDecision: [],
              nivelesDecision: [],
              universidades: [],
              motivosEleccionOrt: [],
            },
            experienciaOrt: { valoraciones: [], publicidadesOrt: [] },
            situacionLaboral: { tiposJornada: [] },
          });
        },
      });
  }

  private applyInitialSurveyCatalogs(catalogs: InitialSurveyCatalogs): void {
    const education = catalogs.educacion;
    const decision = catalogs.decisionAcademica;
    const experience = catalogs.experienciaOrt;

    this.previousCareerOptions.set(toCatalogOptions(education.estadosEducacionSuperiorPrevia));
    this.educationLevelOptions.set(toCatalogOptions(education.nivelesFormacionTutores));
    this.schoolPlaceOptions.set(toCatalogOptions(education.ubicacionesUltimoAnioSecundaria));
    this.supportOptions.set(toCatalogOptions(decision.apoyosDecision));
    this.decisionYearOptions.set(toCatalogOptions(decision.aniosEducacionMediaSuperior));
    this.decisionLevelOptions.set(toCatalogOptions(decision.nivelesDecision));
    this.motivesOptions.set(toCatalogOptions(decision.motivosEleccionOrt));
    this.universityOptions.set(toCatalogOptions(decision.universidades));
    this.higherEducationUniversityOptions.set(toCatalogOptions(education.universidades));
    this.advertisingOptions.set(toCatalogOptions(experience.publicidadesOrt));
    this.workScheduleOptions.set(toCatalogOptions(catalogs.situacionLaboral.tiposJornada));
    this.baccalaureateYears.set(education.aniosBachillerato);
    if (experience.valoraciones.length > 0) {
      this.ratingLabels.set(
        Object.fromEntries(
          experience.valoraciones.map(({ id, label }) => [
            Number(id),
            `${id} ${Number(id) === 1 ? 'estrella' : 'estrellas'}: ${label}`,
          ])
        )
      );
    }
    this.refreshOrientationOptions();
    this.updateConditionalValidators();
  }

  private refreshOrientationOptions(): void {
    this.orientationOptions.set(
      buildOrientationOptions(
        this.baccalaureateYears(),
        this.educationForm.controls.anioSecundaria.value
      )
    );
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
    const uruguay = countries.find(country => country.codigoPais === URUGUAY_COUNTRY_CODE) ?? null;
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

function buildOrientationOptions(
  years: readonly BaccalaureateYearGroup[],
  selectedYear: string
): readonly OpcionInscripcion[] {
  const year = years.find(option => option.id.toString() === selectedYear);
  return (year?.baccalaureates ?? []).flatMap(option =>
    option.orientation ? [{ value: option.id.toString(), label: option.orientation }] : []
  );
}

function isNotFoundError(error: unknown): error is { status: number } {
  return typeof error === 'object' && error !== null && 'status' in error && error.status === 404;
}
