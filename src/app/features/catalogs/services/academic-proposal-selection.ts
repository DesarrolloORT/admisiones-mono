import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { FormGroup } from '@angular/forms';
import { of, Subscription } from 'rxjs';
import { catchError, finalize, switchMap, tap } from 'rxjs/operators';

import {
  type AcademicProposalForm,
  type AcademicProposalOption,
  getAcademicCareerOptions,
  getAcademicProposalLevelIds,
  getAcademicProposalTypeByLevel,
  getAvailableAcademicProposalTypes,
  toAcademicShiftOption,
  toAcademicStartOption,
} from '../models/academic-proposal';
import type { Career, Comienzo, Turno } from '../models/catalog.interface';
import { Catalogs } from './catalogs';

export class AcademicProposalSelection {
  private readonly catalogs = inject(Catalogs);
  private readonly destroyRef = inject(DestroyRef);
  private readonly careersState = signal<readonly Career[]>([]);
  private readonly proposalTypeValue = signal('');
  private form: FormGroup<AcademicProposalForm> | null = null;
  private formSubscriptions = new Subscription();

  public readonly startOptions = signal<readonly AcademicProposalOption[]>([]);
  public readonly shiftOptions = signal<readonly AcademicProposalOption[]>([]);
  public readonly catalogError = signal<string | null>(null);
  public readonly loadingCareers = signal(false);
  public readonly loadingStarts = signal(false);
  public readonly loadingShifts = signal(false);
  public readonly initialized = signal(false);

  public readonly proposalOptions = computed(() =>
    getAvailableAcademicProposalTypes(this.careersState())
  );
  public readonly careerOptions = computed(() =>
    getAcademicCareerOptions(this.careersState(), this.proposalTypeValue())
  );
  public readonly careersLoadingMessage = computed(() =>
    this.loadingCareers() ? 'Estamos cargando las carreras.' : ''
  );
  public readonly startsLoadingMessage = computed(() => {
    if (!this.loadingStarts()) return '';
    const career = getOptionLabel(
      this.careerOptions(),
      this.form?.controls.carrera.value ?? '',
      'la carrera seleccionada'
    );
    return `Estamos cargando los comienzos para "${career}".`;
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
    this.subscribeToForm(form);
    this.loadCareers(form);
  }

  public careers(): readonly Career[] {
    return this.careersState();
  }

  public setProposalType(value: string): void {
    this.proposalTypeValue.set(value);
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
      })
    );

    this.formSubscriptions.add(
      form.controls.carrera.valueChanges
        .pipe(
          tap(() => {
            form.controls.comienzo.setValue('');
            this.startOptions.set([]);
            this.shiftOptions.set([]);
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
