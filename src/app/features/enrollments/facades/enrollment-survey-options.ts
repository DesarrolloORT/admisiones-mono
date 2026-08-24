import { computed, DestroyRef, effect, inject, type Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import { CatalogsApi } from '../../catalogs/api/catalogs.api';
import type {
  BaccalaureateYearGroup,
  InitialSurveyCatalogs,
  LocationCountry,
} from '../../catalogs/models/catalog.interface';
import type { EnrollmentOption } from '../models/enrollment-flow';
import { ENROLLMENT_FORMS } from '../models/enrollment-flow-forms';
import { toCatalogOptions } from '../models/enrollment-flow-options';

const URUGUAY_COUNTRY_CODE = 1;

export interface SurveyOptionsCallbacks {
  /** El paso de encuesta está activo: recién ahí se cargan sus catálogos. */
  isSurveyStepActive: Signal<boolean>;
  /** Cambió algún catálogo de opciones: revalidar los campos condicionales. */
  onOptionsChanged(): void;
}

/**
 * Catálogos y opciones de la encuesta inicial (educación, decisión, experiencia,
 * laboral). La fachada principal registra callbacks vía `initialize()` en vez de
 * inyectarse mutuamente, evitando el ciclo de DI.
 */
export class EnrollmentSurveyOptionsFacade {
  private readonly catalogs = inject(CatalogsApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly educationForm = inject(ENROLLMENT_FORMS).forms.educationForm;

  private uruguayCountryCode: number | null = null;
  private callbacks: SurveyOptionsCallbacks = {
    isSurveyStepActive: signal(false),
    onOptionsChanged: () => undefined,
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

  public readonly previousDegreeProgramOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly educationLevelOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly supportOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly decisionYearOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly decisionLevelOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly motivesOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly schoolYearOptions = computed<readonly EnrollmentOption[]>(() =>
    this.baccalaureateYears().map(year => ({ value: year.id.toString(), label: year.label }))
  );
  public readonly orientationOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly schoolPlaceOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly departmentOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly institutionOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly universityOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly higherEducationUniversityOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly advertisingOptions = signal<readonly EnrollmentOption[]>([]);

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
    const years = this.baccalaureateYears();
    this.orientationOptions.set(
      buildOrientationOptions(years, this.educationForm.controls.highSchoolYear.value)
    );
    if (years.length === 0) return;

    if (
      this.educationForm.controls.orientation.value &&
      !this.orientationOptions().some(
        option => option.value === this.educationForm.controls.orientation.value
      )
    ) {
      this.educationForm.controls.orientation.setValue('', { emitEvent: false });
    }
  }

  public seedEducationalInstitution(institutionId: number, name: string | null): void {
    const value = institutionId.toString();
    if (name) {
      this.institutionOptions.set([{ value, label: name }]);
      this.callbacks.onOptionsChanged();
      return;
    }

    this.catalogs
      .getInstitutions(URUGUAY_COUNTRY_CODE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: institutions => {
          const institution = institutions.find(item => item.id === institutionId);
          if (!institution) return;
          this.institutionOptions.set([{ value, label: institution.label }]);
          this.callbacks.onOptionsChanged();
        },
        error: () => undefined,
      });
  }

  private observeDependentControls(): void {
    this.educationForm.controls.highSchoolYear.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.refreshOrientationOptions();
        this.callbacks.onOptionsChanged();
      });
    this.educationForm.controls.state.valueChanges
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
        next: catalogs => this.applyInitialSurveyCatalogs(catalogs),
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

    this.previousDegreeProgramOptions.set(
      toCatalogOptions(education.previousHigherEducationOptions)
    );
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
    const departmentValue = this.educationForm.controls.state.value;
    const statusCode = departmentValue ? Number(departmentValue) : null;
    if (this.uruguayCountryCode === null || statusCode === null) {
      this.institutionOptions.set([]);
      this.callbacks.onOptionsChanged();
      return;
    }
    this.catalogs
      .getInstitutions(this.uruguayCountryCode, statusCode)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: institutions => {
          this.institutionOptions.set(
            institutions.map(institution => ({
              value: institution.id.toString(),
              label: institution.label,
            }))
          );
          const selected = this.educationForm.controls.educationalInstitution.value;
          if (selected && !this.institutionOptions().some(option => option.value === selected)) {
            this.educationForm.controls.educationalInstitution.setValue('', { emitEvent: false });
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
): readonly EnrollmentOption[] {
  const year = years.find(option => option.id.toString() === selectedYear);
  return (year?.baccalaureates ?? []).flatMap(option =>
    option.orientation ? [{ value: option.id.toString(), label: option.orientation }] : []
  );
}
