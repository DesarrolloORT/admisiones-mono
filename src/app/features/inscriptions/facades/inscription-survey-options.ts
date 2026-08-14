import { computed, DestroyRef, effect, inject, type Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import type {
  BaccalaureateYearGroup,
  InitialSurveyCatalogs,
  LocationCountry,
} from '../../catalogs/models/catalog.interface';
import { Catalogs } from '../../catalogs/services/catalogs';
import type { OpcionInscripcion } from '../models/inscription-flow';
import { toCatalogOptions } from '../models/inscription-flow-options';
import { InscripcionFormsStore } from '../store/inscription-forms';

const URUGUAY_COUNTRY_CODE = 1;

export interface SurveyOptionsCallbacks {
  /** El paso de encuesta está activo: recién ahí se cargan sus catálogos. */
  isSurveyStepActive: Signal<boolean>;
  /** Cambió algún catálogo de opciones: revalidar los campos condicionales. */
  onOptionsChanged(): void;
  /** Llegaron los catálogos de la encuesta inicial: re-aplicar la encuesta del backend. */
  onInitialCatalogsApplied(): void;
}

/**
 * Catálogos y opciones de la encuesta inicial (educación, decisión, experiencia,
 * laboral). La fachada principal registra callbacks vía `initialize()` en vez de
 * inyectarse mutuamente, evitando el ciclo de DI.
 */
export class InscripcionSurveyOptionsFacade {
  private readonly catalogs = inject(Catalogs);
  private readonly destroyRef = inject(DestroyRef);
  private readonly educationForm = inject(InscripcionFormsStore).educationForm;

  private uruguayCountryCode: number | null = null;
  private callbacks: SurveyOptionsCallbacks = {
    isSurveyStepActive: signal(false),
    onOptionsChanged: () => undefined,
    onInitialCatalogsApplied: () => undefined,
  };
  private readonly context = signal<SurveyOptionsCallbacks | null>(null);
  private catalogsRequested = false;
  private readonly baccalaureateYears = signal<readonly BaccalaureateYearGroup[]>([]);

  public readonly ratingLabels = signal<Record<number, string>>({
    1: '1 estrella: Malo',
    2: '2 estrellas: Regular',
    3: '3 estrellas: Bueno',
    4: '4 estrellas: Muy bueno',
    5: '5 estrellas: Excelente',
  });

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

  public readonly catalogError = signal<string | null>(null);
  public readonly loadingInitialSurveyCatalogs = signal(false);
  public readonly initialized = signal(false);

  constructor() {
    effect(() => {
      const context = this.context();
      if (!context || this.catalogsRequested || !context.isSurveyStepActive()) return;
      this.catalogsRequested = true;
      this.loadInitialSurveyCatalogs();
      this.loadDepartmentOptions();
    });
  }

  public initialize(callbacks: SurveyOptionsCallbacks): void {
    this.callbacks = callbacks;
    this.observeDependentControls();
    this.context.set(callbacks);
  }

  public refreshOrientationOptions(): void {
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

  private observeDependentControls(): void {
    this.educationForm.controls.anioSecundaria.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.refreshOrientationOptions();
        this.callbacks.onOptionsChanged();
      });
    this.educationForm.controls.departamento.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadInstitutionsForSelectedDepartment());
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
          this.callbacks.onInitialCatalogsApplied();
        },
        error: () => {
          this.catalogError.set('No se pudieron cargar los catálogos de encuesta inicial.');
          this.applyInitialSurveyCatalogs({
            education: {
              lastSecondaryYearLocations: [],
              highSchoolYears: [],
              previousHigherEducationOptions: [],
              universities: [],
              guardianEducationLevels: [],
            },
            academicDecision: {
              upperSecondaryYears: [],
              decisionSupports: [],
              decisionLevels: [],
              universities: [],
              ortChoiceReasons: [],
            },
            ortExperience: { ratings: [], ortAdvertisements: [] },
          });
        },
      });
  }

  private applyInitialSurveyCatalogs(catalogs: InitialSurveyCatalogs): void {
    const education = catalogs.education;
    const decision = catalogs.academicDecision;
    const experience = catalogs.ortExperience;

    this.previousCareerOptions.set(toCatalogOptions(education.previousHigherEducationOptions));
    this.educationLevelOptions.set(toCatalogOptions(education.guardianEducationLevels));
    this.schoolPlaceOptions.set(toCatalogOptions(education.lastSecondaryYearLocations));
    this.supportOptions.set(toCatalogOptions(decision.decisionSupports));
    this.decisionYearOptions.set(toCatalogOptions(decision.upperSecondaryYears));
    this.decisionLevelOptions.set(toCatalogOptions(decision.decisionLevels));
    this.motivesOptions.set(toCatalogOptions(decision.ortChoiceReasons));
    this.universityOptions.set(toCatalogOptions(decision.universities));
    this.higherEducationUniversityOptions.set(toCatalogOptions(education.universities));
    this.advertisingOptions.set(toCatalogOptions(experience.ortAdvertisements));
    this.baccalaureateYears.set(education.highSchoolYears);
    if (experience.ratings.length > 0) {
      this.ratingLabels.set(
        Object.fromEntries(
          experience.ratings.map(({ id, label }) => [
            Number(id),
            `${id} ${Number(id) === 1 ? 'estrella' : 'estrellas'}: ${label}`,
          ])
        )
      );
    }
    this.refreshOrientationOptions();
    this.callbacks.onOptionsChanged();
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
    const uruguay = countries.find(country => country.countryCode === URUGUAY_COUNTRY_CODE) ?? null;
    this.uruguayCountryCode = uruguay?.countryCode ?? null;
    this.departmentOptions.set(
      (uruguay?.states ?? []).map(state => ({
        value: state.stateCode.toString(),
        label: state.name,
      }))
    );
    this.callbacks.onOptionsChanged();
  }

  private loadInstitutionsForSelectedDepartment(): void {
    const departmentValue = this.educationForm.controls.departamento.value;
    const codigoEstado = departmentValue ? Number(departmentValue) : null;
    if (this.uruguayCountryCode === null || codigoEstado === null) {
      this.institutionOptions.set([]);
      this.callbacks.onOptionsChanged();
      return;
    }
    this.catalogs
      .getInstitutions(this.uruguayCountryCode, codigoEstado)
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
          this.callbacks.onOptionsChanged();
        },
        error: () => {
          this.institutionOptions.set([]);
          this.callbacks.onOptionsChanged();
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
