import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup } from '@angular/forms';
import { of } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';

import {
  Career,
  CatalogItem,
  Comienzo,
  InitialSurveyCatalogs,
  Turno,
} from '../../catalogs/models/catalog.interface';
import { Catalogs } from '../../catalogs/services/catalogs';
import {
  AcademicForm,
  INSCRIPCION_STEP_METADATA,
  InscripcionOption,
  InscripcionStep,
  InscripcionSummaryItem,
  PaymentForm,
  PersonalInfoForm,
} from '../models/inscripcion-flow';
import {
  COORDINATORS,
  PAYMENT_OPTIONS,
  SECONDARY_PLACE_OPTIONS,
  SECONDARY_STATUS_OPTIONS,
  SUBJECTS,
  WORK_STATUS_OPTIONS,
} from '../models/inscripcion-static-data';

type PersonalInfoStringControlName = Exclude<keyof PersonalInfoForm, 'acceptedRules'>;

@Injectable({
  providedIn: 'root',
})
export class InscripcionFlowFacade {
  private readonly catalogs = inject(Catalogs);
  private readonly destroyRef = inject(DestroyRef);

  public readonly secondaryStatusOptions = SECONDARY_STATUS_OPTIONS;
  public readonly secondaryPlaceOptions = SECONDARY_PLACE_OPTIONS;
  public readonly workStatusOptions = WORK_STATUS_OPTIONS;
  public readonly paymentOptions = PAYMENT_OPTIONS;
  public readonly coordinators = COORDINATORS;
  public readonly paymentDeadline = '04/02/2025';
  public readonly studentNumber = '397654';
  public readonly inscriptionAmount = '$ 15.500';

  public readonly academicForm = new FormGroup<AcademicForm>({
    proposalType: new FormControl('', { nonNullable: true }),
    career: new FormControl('', { nonNullable: true }),
    start: new FormControl('', { nonNullable: true }),
    turno: new FormControl('', { nonNullable: true }),
  });

  public readonly personalForm = new FormGroup<PersonalInfoForm>({
    secondaryStatus: new FormControl('', { nonNullable: true }),
    secondaryPlace: new FormControl('', { nonNullable: true }),
    previousCareer: new FormControl('', { nonNullable: true }),
    motherEducation: new FormControl('', { nonNullable: true }),
    fatherEducation: new FormControl('', { nonNullable: true }),
    academicDecision: new FormControl('', { nonNullable: true }),
    ortExperience: new FormControl('', { nonNullable: true }),
    workStatus: new FormControl('', { nonNullable: true }),
    identityVerification: new FormControl('documento-validado', { nonNullable: true }),
    acceptedRules: new FormControl(false, { nonNullable: true }),
  });

  public readonly paymentForm = new FormGroup<PaymentForm>({
    paymentMethod: new FormControl('', { nonNullable: true }),
  });

  private readonly careers = signal<readonly Career[]>([]);
  private readonly proposalTypeValue = toSignal(
    this.academicForm.controls.proposalType.valueChanges,
    {
      initialValue: this.academicForm.controls.proposalType.value,
    }
  );

  public readonly startOptions = signal<readonly InscripcionOption[]>([]);
  public readonly turnoOptions = signal<readonly InscripcionOption[]>([]);
  public readonly previousCareerOptions = signal<readonly InscripcionOption[]>([]);
  public readonly educationLevelOptions = signal<readonly InscripcionOption[]>([]);
  public readonly academicDecisionOptions = signal<readonly InscripcionOption[]>([]);
  public readonly ortExperienceOptions = signal<readonly InscripcionOption[]>([]);
  public readonly catalogError = signal<string | null>(null);
  public readonly step = signal<InscripcionStep>('propuesta');
  public readonly showAllSubjects = signal(false);

  public readonly proposalOptions = computed<readonly InscripcionOption[]>(() => {
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
  public readonly careerOptions = computed<readonly InscripcionOption[]>(() => {
    const proposalType = this.proposalTypeValue();

    return this.careers()
      .filter(career => !proposalType || career.idNivelProducto.toString() === proposalType)
      .map(career => ({
        value: career.idProducto.toString(),
        label: career.nombreProducto,
      }));
  });
  public readonly isFinished = computed(() => this.step() === 'finalizada');
  public readonly canGoBack = computed(
    () => this.step() === 'personal' || this.step() === 'confirmacion'
  );
  public readonly stepMetadata = computed(() => {
    const currentStep = this.step();

    return currentStep === 'finalizada' ? null : INSCRIPCION_STEP_METADATA[currentStep];
  });
  public readonly stepNumber = computed(() => this.stepMetadata()?.number ?? 3);
  public readonly stepSupportLabel = computed(() => this.stepMetadata()?.supportLabel ?? '');
  public readonly summaryItems = computed<InscripcionSummaryItem[]>(() => [
    {
      icon: 'school',
      label: 'Carrera',
      value: this.getOptionLabel(
        this.careerOptions(),
        this.academicForm.controls.career.value,
        'Sin seleccionar'
      ),
    },
    {
      icon: 'calendar_today',
      label: 'Comienzo',
      value: this.getOptionLabel(
        this.startOptions(),
        this.academicForm.controls.start.value,
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

  constructor() {
    this.loadCareers();
    this.loadInitialSurveyCatalogs();
    this.resetAcademicSelectionOnProposalChange();
    this.loadStartsOnCareerChange();
    this.loadTurnosOnStartChange();
  }

  public selectProposal(value: string): void {
    this.academicForm.controls.proposalType.setValue(value);
  }

  public selectPersonalOption(control: PersonalInfoStringControlName, value: string): void {
    this.personalForm.controls[control].setValue(value);
  }

  public selectPaymentMethod(value: string): void {
    this.paymentForm.controls.paymentMethod.setValue(value);
  }

  public continue(): void {
    switch (this.step()) {
      case 'propuesta':
        this.step.set('personal');
        break;
      case 'personal':
        this.step.set('confirmacion');
        break;
      case 'confirmacion':
        this.step.set('finalizada');
        break;
      case 'finalizada':
        break;
    }
  }

  public back(): void {
    switch (this.step()) {
      case 'personal':
        this.step.set('propuesta');
        break;
      case 'confirmacion':
        this.step.set('personal');
        break;
      case 'propuesta':
      case 'finalizada':
        break;
    }
  }

  public toggleSubjects(): void {
    this.showAllSubjects.update(showAll => !showAll);
  }

  private loadCareers(): void {
    this.catalogs
      .getCareers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: careers => {
          this.careers.set(careers);
        },
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
        next: catalogs => {
          this.applyInitialSurveyCatalogs(catalogs);
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
    this.academicForm.controls.proposalType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.academicForm.patchValue({ career: '', start: '', turno: '' }, { emitEvent: false });
        this.startOptions.set([]);
        this.turnoOptions.set([]);
      });
  }

  private loadStartsOnCareerChange(): void {
    this.academicForm.controls.career.valueChanges
      .pipe(
        tap(() => {
          this.academicForm.patchValue({ start: '', turno: '' }, { emitEvent: false });
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
      .subscribe(starts => {
        this.startOptions.set(starts.map(start => this.toStartOption(start)));
      });
  }

  private loadTurnosOnStartChange(): void {
    this.academicForm.controls.start.valueChanges
      .pipe(
        tap(() => {
          this.academicForm.controls.turno.setValue('', { emitEvent: false });
          this.turnoOptions.set([]);
        }),
        switchMap(value => {
          const careerId = this.toNullableNumber(this.academicForm.controls.career.value);
          const startId = this.toNullableNumber(value);

          return careerId === null || startId === null
            ? of<Turno[]>([])
            : this.catalogs.getTurnos(careerId, startId).pipe(catchError(() => of<Turno[]>([])));
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(turnos => {
        this.turnoOptions.set(turnos.map(turno => this.toTurnoOption(turno)));
      });
  }

  private applyInitialSurveyCatalogs(catalogs: InitialSurveyCatalogs): void {
    this.previousCareerOptions.set(this.toCatalogOptions(catalogs.estadoEducacionSuperior));
    this.educationLevelOptions.set(this.toCatalogOptions(catalogs.formacionTutores));
    this.academicDecisionOptions.set(this.toCatalogOptions(catalogs.decisionCarrera));
    this.ortExperienceOptions.set(this.toCatalogOptions(catalogs.decisionUniversidad));
  }

  private toCatalogOptions(items: CatalogItem[]): InscripcionOption[] {
    return items.map(item => ({
      value: item.id.toString(),
      label: item.label,
    }));
  }

  private toStartOption(start: Comienzo): InscripcionOption {
    return {
      value: start.idProceso.toString(),
      label: start.nombreProceso,
    };
  }

  private toTurnoOption(turno: Turno): InscripcionOption {
    return {
      value: turno.idOferta.toString(),
      label: turno.horarioReferencia
        ? `${turno.nombreTurno} (${turno.horarioReferencia})`
        : turno.nombreTurno,
    };
  }

  private toNullableNumber(value: string): number | null {
    if (!value) {
      return null;
    }

    const parsed = Number(value);

    return Number.isFinite(parsed) ? parsed : null;
  }

  private getProposalIcon(label: string): string {
    const normalized = label
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase();

    if (normalized.includes('tecn')) {
      return 'list_alt';
    }

    if (normalized.includes('actualizacion')) {
      return 'how_to_reg';
    }

    return 'school';
  }

  private getOptionLabel(
    options: readonly InscripcionOption[],
    value: string,
    fallback: string
  ): string {
    return options.find(option => option.value === value)?.label ?? fallback;
  }
}
