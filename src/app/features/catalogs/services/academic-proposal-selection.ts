import { computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FormGroup, Validators } from '@angular/forms';
import { of, Subscription } from 'rxjs';
import { catchError, finalize, switchMap, tap } from 'rxjs/operators';

import {
  type AcademicProposalForm,
  type AcademicProposalOption,
  getAcademicDegreeProgramOptions,
  getAcademicProposalLevelIds,
  getAcademicProposalTerminology,
  getAcademicProposalTypeId,
  getAcademicProposalTypes,
  isProfessionalUpdateLevel,
  isProfessionalUpdateType,
  toAcademicIntakeOption,
  toAcademicSeminarOption,
  toAcademicShiftOption,
} from '../models/academic-proposal';
import type { DegreeProgram, Intake, Seminar, Shift } from '../models/catalog.interface';
import { Catalogs } from './catalogs';

export class AcademicProposalSelection {
  private readonly catalogs = inject(Catalogs);
  private readonly destroyRef = inject(DestroyRef);
  private readonly degreeProgramsState = signal<readonly DegreeProgram[]>([]);
  private readonly proposalTypeValue = signal('');
  private readonly seminarsDegreeProgramId = signal<number | null>(null);
  private form: FormGroup<AcademicProposalForm> | null = null;
  private formSubscriptions = new Subscription();
  private degreeProgramsSubscription = new Subscription();

  public readonly intakeOptions = signal<readonly AcademicProposalOption[]>([]);
  public readonly shiftOptions = signal<readonly AcademicProposalOption[]>([]);
  public readonly catalogError = signal<string | null>(null);
  public readonly loadingDegreePrograms = signal(false);
  public readonly loadingIntakes = signal(false);
  public readonly loadingShifts = signal(false);
  public readonly initialized = signal(false);
  private readonly seminarsState = signal<readonly Seminar[]>([]);
  public readonly loadingSeminars = signal(false);

  public readonly proposalOptions = computed(() => getAcademicProposalTypes());
  public readonly isProfessionalUpdate = computed(() =>
    isProfessionalUpdateType(this.proposalTypeValue())
  );
  public readonly terminology = computed(() =>
    getAcademicProposalTerminology(this.proposalTypeValue())
  );
  public readonly degreeProgramOptions = computed(() =>
    getAcademicDegreeProgramOptions(this.degreeProgramsState(), this.proposalTypeValue())
  );
  public readonly seminarOptions = computed(() =>
    this.seminarsState().map(toAcademicSeminarOption)
  );
  /** AP: el programa manda. Sin `tieneSeminario` se elige una sola oferta. */
  public readonly allowsMultipleSeminars = computed(
    () =>
      this.degreeProgramsState().find(
        degreeProgram => degreeProgram.productId === this.seminarsDegreeProgramId()
      )?.hasSeminar === true
  );
  public readonly seminarLabel = computed(() =>
    this.allowsMultipleSeminars() ? this.terminology().startLabel : 'Horario'
  );
  public readonly seminarErrorText = computed(() =>
    this.allowsMultipleSeminars()
      ? this.terminology().startErrorText
      : `Seleccioná un ${this.seminarLabel().toLocaleLowerCase('es-UY')}`
  );
  public readonly degreeProgramsLoadingMessage = computed(() =>
    this.loadingDegreePrograms() ? this.terminology().degreeProgramLoadingMessage : ''
  );
  public readonly intakesLoadingMessage = computed(() => {
    if (!this.loadingIntakes() && !this.loadingSeminars()) return '';
    const terminology = this.terminology();
    const degreeProgram = getOptionLabel(
      this.degreeProgramOptions(),
      this.form?.controls.degreeProgram.value ?? '',
      terminology.degreeProgramFallbackLabel
    );
    return `Estamos cargando ${terminology.startNoun} para "${degreeProgram}".`;
  });
  public readonly shiftsLoadingMessage = computed(() => {
    if (!this.loadingShifts()) return '';
    const intake = getOptionLabel(
      this.intakeOptions(),
      this.form?.controls.intake.value ?? '',
      'el comienzo seleccionado'
    );
    return `Estamos cargando los turnos para "${intake}".`;
  });

  constructor() {
    // Una sola opción no se elige: se precarga sin pisar una selección existente.
    // Los catálogos se leen antes del guard a propósito: si el efecto corriera sin
    // form y sin leerlos, quedaría sin dependencias y no volvería a correr nunca.
    effect(() => {
      const intake = singleOptionValue(this.intakeOptions());
      const shift = singleOptionValue(this.shiftOptions());
      const seminar = singleOptionValue(this.seminarOptions());
      const controls = this.form?.controls;
      if (!controls) return;

      if (intake && !controls.intake.value) controls.intake.setValue(intake);
      if (shift && !controls.shift.value) controls.shift.setValue(shift);
      if (seminar && controls.seminars.value.length === 0) controls.seminars.setValue([seminar]);
    });

    this.destroyRef.onDestroy(() => {
      this.formSubscriptions.unsubscribe();
      this.degreeProgramsSubscription.unsubscribe();
    });
  }

  public connect(form: FormGroup<AcademicProposalForm>): void {
    if (this.form === form) return;

    this.formSubscriptions.unsubscribe();
    this.formSubscriptions = new Subscription();
    this.form = form;
    this.proposalTypeValue.set(form.controls.proposalType.value);
    this.syncValidatorsForProposalType(form);
    this.subscribeToForm(form);
    this.loadDegreePrograms(form, form.controls.proposalType.value);
  }

  public degreePrograms(): readonly DegreeProgram[] {
    return this.degreeProgramsState();
  }

  public setProposalType(value: string): void {
    this.proposalTypeValue.set(value);
    if (!this.form) return;
    this.syncValidatorsForProposalType(this.form);
    this.loadDegreePrograms(this.form, value);
  }

  public canSelectDegreeProgram(): boolean {
    return (
      !this.loadingDegreePrograms() &&
      getAcademicProposalLevelIds(this.form?.controls.proposalType.value ?? '').length > 0 &&
      this.degreeProgramOptions().length > 0
    );
  }

  public canSelectIntake(): boolean {
    return (
      !!this.form?.controls.degreeProgram.value &&
      !this.loadingIntakes() &&
      this.intakeOptions().length > 0
    );
  }

  public seminars(): readonly Seminar[] {
    return this.seminarsState();
  }

  public canSelectSeminars(): boolean {
    return (
      !!this.form?.controls.degreeProgram.value &&
      !this.loadingSeminars() &&
      this.seminarOptions().length > 0
    );
  }

  /** Carga el catálogo de seminarios de un programa (también para precarga en retomar). */
  public loadSeminars(degreeProgramId: number): void {
    this.seminarsDegreeProgramId.set(degreeProgramId);
    const admissionProcessId = this.degreeProgramsState().find(
      degreeProgram => degreeProgram.productId === degreeProgramId
    )?.admissionProcessId;
    if (
      typeof admissionProcessId !== 'number' ||
      !Number.isSafeInteger(admissionProcessId) ||
      admissionProcessId <= 0
    ) {
      this.seminarsState.set([]);
      return;
    }

    this.loadingSeminars.set(true);
    this.catalogs
      .getSeminars(degreeProgramId, admissionProcessId)
      .pipe(
        catchError(() => of<Seminar[]>([])),
        finalize(() => this.loadingSeminars.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(seminars => this.seminarsState.set(seminars));
  }

  public canSelectShift(): boolean {
    return (
      !!this.form?.controls.intake.value && !this.loadingShifts() && this.shiftOptions().length > 0
    );
  }

  public loadOptions(
    degreeProgramId: number | null,
    processId: number | null,
    shiftId: number | null
  ): void {
    const form = this.requireForm();
    if (degreeProgramId === null) return;

    if (this.isProfessionalUpdateDegreeProgram(degreeProgramId)) {
      this.loadSeminars(degreeProgramId);
      return;
    }

    this.catalogs
      .getIntakes(degreeProgramId)
      .pipe(
        catchError(() => of<Intake[]>([])),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(intakes => this.intakeOptions.set(intakes.map(toAcademicIntakeOption)));

    if (processId === null) return;
    this.catalogs
      .getShifts(degreeProgramId, processId)
      .pipe(
        catchError(() => of<Shift[]>([])),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(shifts => {
        this.shiftOptions.set(shifts.map(toAcademicShiftOption));
        const selectedShift = shifts.find(shift => shift.shiftId === shiftId);
        form.controls.shift.setValue(selectedShift?.offeringId.toString() ?? '', {
          emitEvent: false,
        });
      });
  }

  private loadDegreePrograms(form: FormGroup<AcademicProposalForm>, proposalType: string): void {
    this.degreeProgramsSubscription.unsubscribe();
    const proposalTypeId = getAcademicProposalTypeId(proposalType);
    this.catalogError.set(null);
    if (proposalTypeId === null) {
      this.degreeProgramsState.set([]);
      this.loadingDegreePrograms.set(false);
      this.initialized.set(true);
      return;
    }

    this.loadingDegreePrograms.set(true);
    this.degreeProgramsSubscription = this.catalogs
      .getDegreePrograms(proposalTypeId)
      .pipe(
        finalize(() => {
          this.loadingDegreePrograms.set(false);
          this.initialized.set(true);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: degreePrograms => {
          this.degreeProgramsState.set(degreePrograms);
          const selectedDegreeProgram = degreePrograms.find(
            degreeProgram =>
              degreeProgram.productId.toString() === form.controls.degreeProgram.value
          );
          if (
            selectedDegreeProgram &&
            this.isProfessionalUpdateDegreeProgram(selectedDegreeProgram.productId)
          ) {
            this.loadSeminars(selectedDegreeProgram.productId);
          }
        },
        error: () => {
          this.catalogError.set('No se pudieron cargar las carreras.');
          this.degreeProgramsState.set([]);
        },
      });
  }

  private subscribeToForm(form: FormGroup<AcademicProposalForm>): void {
    this.formSubscriptions.add(
      form.controls.proposalType.valueChanges.subscribe(value => {
        this.proposalTypeValue.set(value);
        form.controls.degreeProgram.setValue('');
        this.intakeOptions.set([]);
        this.shiftOptions.set([]);
        this.seminarsState.set([]);
        this.seminarsDegreeProgramId.set(null);
        this.syncValidatorsForProposalType(form);
        this.degreeProgramsState.set([]);
        this.loadDegreePrograms(form, value);
      })
    );

    this.formSubscriptions.add(
      form.controls.degreeProgram.valueChanges
        .pipe(
          tap(() => {
            form.controls.intake.setValue('');
            form.controls.seminars.setValue([]);
            this.intakeOptions.set([]);
            this.shiftOptions.set([]);
            this.seminarsState.set([]);
            this.seminarsDegreeProgramId.set(null);
          }),
          switchMap(value => {
            const degreeProgramId = toNullableNumber(value);
            if (degreeProgramId === null) {
              this.loadingIntakes.set(false);
              return of<Intake[]>([]);
            }

            if (this.isProfessionalUpdateDegreeProgram(degreeProgramId)) {
              this.loadSeminars(degreeProgramId);
              return of<Intake[]>([]);
            }

            this.loadingIntakes.set(true);
            return this.catalogs.getIntakes(degreeProgramId).pipe(
              catchError(() => of<Intake[]>([])),
              finalize(() => this.loadingIntakes.set(false))
            );
          })
        )
        .subscribe(intakes => this.intakeOptions.set(intakes.map(toAcademicIntakeOption)))
    );

    this.formSubscriptions.add(
      form.controls.intake.valueChanges
        .pipe(
          tap(() => {
            form.controls.shift.setValue('', { emitEvent: false });
            this.shiftOptions.set([]);
          }),
          switchMap(value => {
            const degreeProgramId = toNullableNumber(form.controls.degreeProgram.value);
            const intakeId = toNullableNumber(value);
            if (degreeProgramId === null || intakeId === null) {
              this.loadingShifts.set(false);
              return of<Shift[]>([]);
            }

            this.loadingShifts.set(true);
            return this.catalogs.getShifts(degreeProgramId, intakeId).pipe(
              catchError(() => of<Shift[]>([])),
              finalize(() => this.loadingShifts.set(false))
            );
          })
        )
        .subscribe(shifts => this.shiftOptions.set(shifts.map(toAcademicShiftOption)))
    );

    // Sin seminarios el select es simple y emite un string; el form siempre guarda string[].
    this.formSubscriptions.add(
      form.controls.seminars.valueChanges.subscribe(value => {
        const selected: unknown = value;
        if (typeof selected === 'string')
          form.controls.seminars.setValue(selected ? [selected] : [], { emitEvent: false });
      })
    );
  }

  // AP no usa comienzo/turno (los seminarios traen la oferta); el resto conserva
  // la validación actual. Se sincroniza al conectar y al cambiar el tipo.
  private syncValidatorsForProposalType(form: FormGroup<AcademicProposalForm>): void {
    const professionalUpdate = isProfessionalUpdateType(form.controls.proposalType.value);
    form.controls.intake.setValidators(professionalUpdate ? null : Validators.required);
    form.controls.shift.setValidators(professionalUpdate ? null : Validators.required);
    form.controls.seminars.setValidators(professionalUpdate ? Validators.required : null);
    form.controls.intake.updateValueAndValidity({ emitEvent: false });
    form.controls.shift.updateValueAndValidity({ emitEvent: false });
    form.controls.seminars.updateValueAndValidity({ emitEvent: false });
  }

  // El nivel del producto manda: un producto AP nunca debe disparar getComienzos,
  // aunque el tipo de propuesta del form quede desincronizado (p. ej. re-aplicación
  // de una encuesta previa de otro flujo).
  private isProfessionalUpdateDegreeProgram(degreeProgramId: number): boolean {
    const degreeProgram = this.degreeProgramsState().find(
      item => item.productId === degreeProgramId
    );
    return degreeProgram
      ? isProfessionalUpdateLevel(degreeProgram.productLevelId)
      : this.isProfessionalUpdate();
  }

  private requireForm(): FormGroup<AcademicProposalForm> {
    if (!this.form) throw new Error('AcademicProposalSelection must be connected to a form.');
    return this.form;
  }
}

function getOptionLabel(
  options: readonly AcademicProposalOption[],
  value: string,
  fallback: string
): string {
  return options.find(option => option.value === value)?.label ?? fallback;
}

function singleOptionValue(options: readonly AcademicProposalOption[]): string | null {
  return options.length === 1 ? (options[0]?.value ?? null) : null;
}

function toNullableNumber(value: string | number | null | undefined): number | null {
  if (value === null || value === undefined || value === '') return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}
