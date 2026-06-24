import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { OrtErrorItem } from '@desarrolloort/components';
import { of } from 'rxjs';
import { catchError, finalize, switchMap, tap } from 'rxjs/operators';

import type { Career, Comienzo, Turno } from '../../catalogs/models/catalog.interface';
import { Catalogs } from '../../catalogs/services/catalogs';
import type { InscripcionBackendSurvey, OpcionInscripcion } from '../models/inscripcion-flow';
import { buildFormErrors } from '../models/inscripcion-flow-forms';
import {
  getAvailableProposalOptions,
  getCareerOptions,
  getOptionLabel,
  getProposalLevelIds,
  getProposalOptionByLevel,
  toStartOption,
  toTurnoOption,
} from '../models/inscripcion-flow-options';
import { Inscripciones } from '../services/inscripciones';
import { InscripcionFormsStore } from '../store/inscripcion-forms';
import { InscripcionProcessStore } from '../store/inscripcion-process';

export class InscripcionProposalFacade {
  private readonly catalogs = inject(Catalogs);
  private readonly inscripciones = inject(Inscripciones);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formsStore = inject(InscripcionFormsStore);
  private readonly process = inject(InscripcionProcessStore);

  public readonly academicForm = this.formsStore.academicForm;

  private readonly careersState = signal<readonly Career[]>([]);
  private readonly proposalTypeValue = signal(this.academicForm.controls.tipoPropuesta.value);
  private readonly submitted = signal(false);
  private readonly productInterestError = signal<string | null>(null);

  public readonly startOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly turnoOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly catalogError = signal<string | null>(null);
  public readonly loadingCareers = signal(false);
  public readonly loadingStarts = signal(false);
  public readonly loadingTurnos = signal(false);
  public readonly registeringProductInterest = signal(false);
  public readonly initialized = signal(false);

  public readonly proposalOptions = computed<readonly OpcionInscripcion[]>(() =>
    getAvailableProposalOptions(this.careersState())
  );
  public readonly careerOptions = computed<readonly OpcionInscripcion[]>(() =>
    getCareerOptions(this.careersState(), this.proposalTypeValue())
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
  public readonly academicErrors = computed<OrtErrorItem[]>(() => {
    if (!this.submitted()) return [];

    const formErrors = buildFormErrors(this.academicForm, [
      { controlName: 'tipoPropuesta', fieldId: '', label: 'Propuesta académica' },
      { controlName: 'carrera', fieldId: '', label: 'Carrera' },
      { controlName: 'comienzo', fieldId: '', label: 'Comienzo' },
      { controlName: 'turno', fieldId: '', label: 'Turno' },
    ]);
    const interestError = this.productInterestError();
    return interestError ? [...formErrors, { message: interestError }] : formErrors;
  });

  constructor() {
    this.loadCareers();
    this.resetAcademicSelectionOnProposalChange();
    this.loadStartsOnCareerChange();
    this.loadTurnosOnStartChange();
  }

  public continue(): void {
    if (this.registeringProductInterest()) return;

    this.submitted.set(true);
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
      .pipe(
        finalize(() => this.registeringProductInterest.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: registered => {
          if (!registered) {
            this.productInterestError.set(
              'No se pudo registrar el interés por la propuesta seleccionada.'
            );
            return;
          }
          this.process.flow.next();
          this.process.markCheckpoint();
        },
        error: () =>
          this.productInterestError.set(
            'No se pudo registrar el interés por la propuesta seleccionada.'
          ),
      });
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

  public careers(): readonly Career[] {
    return this.careersState();
  }

  public setProposalType(value: string): void {
    this.proposalTypeValue.set(value);
  }

  public loadAcademicOptionsForSurvey(survey: InscripcionBackendSurvey): void {
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

  private loadCareers(): void {
    this.loadingCareers.set(true);
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
          this.careersState.set([]);
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

function toNullableNumber(value: string | number | null | undefined): number | null {
  if (value === null || value === undefined || value === '') return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}
