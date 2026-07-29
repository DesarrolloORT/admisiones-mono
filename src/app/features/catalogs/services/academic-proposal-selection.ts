import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FormGroup, Validators } from '@angular/forms';
import { of, Subscription } from 'rxjs';
import { catchError, finalize, switchMap, tap } from 'rxjs/operators';

import {
  type AcademicProposalForm,
  type AcademicProposalOption,
  getAcademicCareerOptions,
  getAcademicProposalLevelIds,
  getAcademicProposalTerminology,
  getAcademicProposalTypeByLevel,
  getAvailableAcademicProposalTypes,
  isProfessionalUpdateLevel,
  isProfessionalUpdateType,
  toAcademicSeminarOption,
  toAcademicShiftOption,
  toAcademicStartOption,
} from '../models/academic-proposal';
import type { Career, Comienzo, Seminario, Turno } from '../models/catalog.interface';
import { Catalogs } from './catalogs';

export class AcademicProposalSelection {
  private readonly catalogs = inject(Catalogs);
  private readonly destroyRef = inject(DestroyRef);
  private readonly careersState = signal<readonly Career[]>([]);
  private readonly proposalTypeValue = signal('');
  private readonly seminarsProgramId = signal<number | null>(null);
  private form: FormGroup<AcademicProposalForm> | null = null;
  private formSubscriptions = new Subscription();

  public readonly startOptions = signal<readonly AcademicProposalOption[]>([]);
  public readonly shiftOptions = signal<readonly AcademicProposalOption[]>([]);
  public readonly catalogError = signal<string | null>(null);
  public readonly loadingCareers = signal(false);
  public readonly loadingStarts = signal(false);
  public readonly loadingShifts = signal(false);
  public readonly initialized = signal(false);
  private readonly seminarsState = signal<readonly Seminario[]>([]);
  public readonly loadingSeminars = signal(false);

  public readonly proposalOptions = computed(() =>
    getAvailableAcademicProposalTypes(this.careersState())
  );
  public readonly isProfessionalUpdate = computed(() =>
    isProfessionalUpdateType(this.proposalTypeValue())
  );
  public readonly terminology = computed(() =>
    getAcademicProposalTerminology(this.proposalTypeValue())
  );
  public readonly careerOptions = computed(() =>
    getAcademicCareerOptions(this.careersState(), this.proposalTypeValue())
  );
  public readonly seminarOptions = computed(() =>
    this.seminarsState().map(toAcademicSeminarOption)
  );
  /** AP: el programa manda. Sin `tieneSeminario` se elige una sola oferta. */
  public readonly allowsMultipleSeminars = computed(
    () =>
      this.careersState().find(career => career.idProducto === this.seminarsProgramId())
        ?.tieneSeminario === true
  );
  public readonly careersLoadingMessage = computed(() =>
    this.loadingCareers() ? this.terminology().careerLoadingMessage : ''
  );
  public readonly startsLoadingMessage = computed(() => {
    if (!this.loadingStarts() && !this.loadingSeminars()) return '';
    const terminology = this.terminology();
    const career = getOptionLabel(
      this.careerOptions(),
      this.form?.controls.carrera.value ?? '',
      terminology.careerFallbackLabel
    );
    return `Estamos cargando ${terminology.startNoun} para "${career}".`;
  });
  public readonly shiftsLoadingMessage = computed(() => {
    if (!this.loadingShifts()) return '';
    const start = getOptionLabel(
      this.startOptions(),
      this.form?.controls.comienzo.value ?? '',
      'el comienzo seleccionado'
    );
    return `Estamos cargando los turnos para "${start}".`;
  });

  constructor() {
    this.destroyRef.onDestroy(() => this.formSubscriptions.unsubscribe());
  }

  public connect(form: FormGroup<AcademicProposalForm>): void {
    if (this.form === form) return;

    this.formSubscriptions.unsubscribe();
    this.formSubscriptions = new Subscription();
    this.form = form;
    this.proposalTypeValue.set(form.controls.tipoPropuesta.value);
    this.syncValidatorsForProposalType(form);
    this.subscribeToForm(form);
    this.loadCareers(form);
  }

  public careers(): readonly Career[] {
    return this.careersState();
  }

  public setProposalType(value: string): void {
    this.proposalTypeValue.set(value);
    if (this.form) this.syncValidatorsForProposalType(this.form);
  }

  public canSelectCareer(): boolean {
    return (
      !this.loadingCareers() &&
      getAcademicProposalLevelIds(this.form?.controls.tipoPropuesta.value ?? '').length > 0 &&
      this.careerOptions().length > 0
    );
  }

  public canSelectStart(): boolean {
    return (
      !!this.form?.controls.carrera.value && !this.loadingStarts() && this.startOptions().length > 0
    );
  }

  public seminars(): readonly Seminario[] {
    return this.seminarsState();
  }

  public canSelectSeminars(): boolean {
    return (
      !!this.form?.controls.carrera.value &&
      !this.loadingSeminars() &&
      this.seminarOptions().length > 0
    );
  }

  /** Carga el catálogo de seminarios de un programa (también para precarga en retomar). */
  public loadSeminars(idPrograma: number): void {
    this.seminarsProgramId.set(idPrograma);
    const idProceso = this.careersState().find(
      career => career.idProducto === idPrograma
    )?.idProceso;
    if (typeof idProceso !== 'number' || !Number.isSafeInteger(idProceso) || idProceso <= 0) {
      this.seminarsState.set([]);
      return;
    }

    this.loadingSeminars.set(true);
    this.catalogs
      .getSeminarios(idPrograma, idProceso)
      .pipe(
        catchError(() => of<Seminario[]>([])),
        finalize(() => this.loadingSeminars.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(seminars => this.seminarsState.set(seminars));
  }

  public canSelectShift(): boolean {
    return (
      !!this.form?.controls.comienzo.value &&
      !this.loadingShifts() &&
      this.shiftOptions().length > 0
    );
  }

  public loadOptions(
    careerId: number | null,
    processId: number | null,
    shiftId: number | null
  ): void {
    const form = this.requireForm();
    if (careerId === null) return;

    if (this.isProfessionalUpdateCareer(careerId)) {
      this.loadSeminars(careerId);
      return;
    }

    this.catalogs
      .getComienzos(careerId)
      .pipe(
        catchError(() => of<Comienzo[]>([])),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(starts => this.startOptions.set(starts.map(toAcademicStartOption)));

    if (processId === null) return;
    this.catalogs
      .getTurnos(careerId, processId)
      .pipe(
        catchError(() => of<Turno[]>([])),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(shifts => {
        this.shiftOptions.set(shifts.map(toAcademicShiftOption));
        const selectedShift = shifts.find(shift => shift.idTurno === shiftId);
        form.controls.turno.setValue(selectedShift?.idOferta.toString() ?? '', {
          emitEvent: false,
        });
      });
  }

  private loadCareers(form: FormGroup<AcademicProposalForm>): void {
    this.loadingCareers.set(true);
    this.catalogError.set(null);
    this.catalogs
      .getCareers()
      .pipe(
        finalize(() => {
          this.loadingCareers.set(false);
          this.initialized.set(true);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: careers => {
          this.careersState.set(careers);
          const selectedCareer = careers.find(
            career => career.idProducto.toString() === form.controls.carrera.value
          );
          if (selectedCareer && !form.controls.tipoPropuesta.value) {
            const proposalType = getAcademicProposalTypeByLevel(
              selectedCareer.idNivelProducto
            )?.value;
            if (proposalType) {
              form.controls.tipoPropuesta.setValue(proposalType, { emitEvent: false });
              this.proposalTypeValue.set(proposalType);
              this.syncValidatorsForProposalType(form);
            }
          }
        },
        error: () => {
          this.catalogError.set('No se pudieron cargar las carreras.');
          this.careersState.set([]);
        },
      });
  }

  private subscribeToForm(form: FormGroup<AcademicProposalForm>): void {
    this.formSubscriptions.add(
      form.controls.tipoPropuesta.valueChanges.subscribe(value => {
        this.proposalTypeValue.set(value);
        form.controls.carrera.setValue('');
        this.startOptions.set([]);
        this.shiftOptions.set([]);
        this.seminarsState.set([]);
        this.seminarsProgramId.set(null);
        this.syncValidatorsForProposalType(form);
      })
    );

    this.formSubscriptions.add(
      form.controls.carrera.valueChanges
        .pipe(
          tap(() => {
            form.controls.comienzo.setValue('');
            form.controls.seminarios.setValue([]);
            this.startOptions.set([]);
            this.shiftOptions.set([]);
            this.seminarsState.set([]);
            this.seminarsProgramId.set(null);
          }),
          switchMap(value => {
            const careerId = toNullableNumber(value);
            if (careerId === null) {
              this.loadingStarts.set(false);
              return of<Comienzo[]>([]);
            }

            if (this.isProfessionalUpdateCareer(careerId)) {
              this.loadSeminars(careerId);
              return of<Comienzo[]>([]);
            }

            this.loadingStarts.set(true);
            return this.catalogs.getComienzos(careerId).pipe(
              catchError(() => of<Comienzo[]>([])),
              finalize(() => this.loadingStarts.set(false))
            );
          })
        )
        .subscribe(starts => this.startOptions.set(starts.map(toAcademicStartOption)))
    );

    this.formSubscriptions.add(
      form.controls.comienzo.valueChanges
        .pipe(
          tap(() => {
            form.controls.turno.setValue('', { emitEvent: false });
            this.shiftOptions.set([]);
          }),
          switchMap(value => {
            const careerId = toNullableNumber(form.controls.carrera.value);
            const startId = toNullableNumber(value);
            if (careerId === null || startId === null) {
              this.loadingShifts.set(false);
              return of<Turno[]>([]);
            }

            this.loadingShifts.set(true);
            return this.catalogs.getTurnos(careerId, startId).pipe(
              catchError(() => of<Turno[]>([])),
              finalize(() => this.loadingShifts.set(false))
            );
          })
        )
        .subscribe(shifts => this.shiftOptions.set(shifts.map(toAcademicShiftOption)))
    );

    // Sin seminarios el select es simple y emite un string; el form siempre guarda string[].
    this.formSubscriptions.add(
      form.controls.seminarios.valueChanges.subscribe(value => {
        const selected: unknown = value;
        if (typeof selected === 'string')
          form.controls.seminarios.setValue(selected ? [selected] : [], { emitEvent: false });
      })
    );
  }

  // AP no usa comienzo/turno (los seminarios traen la oferta); el resto conserva
  // la validación actual. Se sincroniza al conectar y al cambiar el tipo.
  private syncValidatorsForProposalType(form: FormGroup<AcademicProposalForm>): void {
    const professionalUpdate = isProfessionalUpdateType(form.controls.tipoPropuesta.value);
    form.controls.comienzo.setValidators(professionalUpdate ? null : Validators.required);
    form.controls.turno.setValidators(professionalUpdate ? null : Validators.required);
    form.controls.seminarios.setValidators(professionalUpdate ? Validators.required : null);
    form.controls.comienzo.updateValueAndValidity({ emitEvent: false });
    form.controls.turno.updateValueAndValidity({ emitEvent: false });
    form.controls.seminarios.updateValueAndValidity({ emitEvent: false });
  }

  // El nivel del producto manda: un producto AP nunca debe disparar getComienzos,
  // aunque el tipo de propuesta del form quede desincronizado (p. ej. re-aplicación
  // de una encuesta previa de otro flujo).
  private isProfessionalUpdateCareer(careerId: number): boolean {
    const career = this.careersState().find(item => item.idProducto === careerId);
    return career ? isProfessionalUpdateLevel(career.idNivelProducto) : this.isProfessionalUpdate();
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

function toNullableNumber(value: string | number | null | undefined): number | null {
  if (value === null || value === undefined || value === '') return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}
