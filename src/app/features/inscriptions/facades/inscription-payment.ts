import { computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs/operators';
import type { ErrorAlertState } from 'src/app/shared/ui/error-alert/error-alert';

import { Catalogs } from '../../catalogs/services/catalogs';
import { FALLBACK_BANK_OPTIONS, toBankOptions } from '../models/inscription-bank-logo';
import type {
  InscripcionConfirmedDetail,
  InscripcionCoordinador,
  InscripcionDetail,
} from '../models/inscription-detail';
import type {
  ContactoCoordinador,
  InscripcionPaymentResponse,
  InscripcionReservationData,
  ItemSeminarioResumen,
  MetodoPago,
  OpcionInscripcion,
  ResultadoPago,
} from '../models/inscription-flow';
import { parseResultadoForzado } from '../models/inscription-flow-policy';
import {
  buildReservationInstructions,
  buildSeminariosSummary,
  buildSummaryItems,
  formatInscriptionAmount,
  formatPaymentDeadline,
} from '../models/inscription-flow-view';
import type { InscripcionOutcome, InscripcionPaymentView } from '../models/inscription-process';
import {
  PAYMENT_OPTIONS,
  type PaymentOption,
  SANTANDER_ACCOUNT_URL,
  STUDENT_SERVICE_LINKS,
} from '../models/inscription-static-data';
import { ExternalPaymentSubmitter } from '../services/external-payment-submitter';
import { InscriptionResumeContextStore } from '../services/inscription-resume-context';
import { Inscripciones } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionProposalFacade } from './inscription-proposal';

export class InscripcionPaymentFacade {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly catalogs = inject(Catalogs);
  private readonly inscriptions = inject(Inscripciones);
  private readonly resumeContext = inject(InscriptionResumeContextStore);
  private readonly externalPaymentSubmitter = inject(ExternalPaymentSubmitter);
  private readonly formsStore = inject(InscripcionFormsStore);
  private readonly process = inject(InscripcionProcessStore);
  private readonly proposal = inject(InscripcionProposalFacade);

  public readonly paymentForm = this.formsStore.paymentForm;
  public readonly paymentOptions = computed<readonly PaymentOption[]>(() =>
    PAYMENT_OPTIONS.flatMap(option => this.resolvePaymentOption(option))
  );
  public readonly santanderAccountUrl = SANTANDER_ACCOUNT_URL;
  public readonly studentServices = STUDENT_SERVICE_LINKS;

  // Detalle de la inscripción confirmada (número de estudiante, coordinación y
  // materias). Lo setea applyResumeContext al retomar desde el panel, o se carga
  // vía getDetail al confirmarse el pago. Si la carga falla queda null y la
  // pantalla de éxito oculta esas secciones.
  public readonly confirmedDetail = signal<InscripcionConfirmedDetail | null>(null);
  // Datos para pagar la reserva (Abitab/Paganza): cédula y número de estudiante.
  // Lo setea applyPaymentInit al retomar, o se carga vía getDetail al quedar en
  // reserva desde el flujo fresco. Si falla queda null y la pantalla muestra solo
  // el monto.
  public readonly reservationData = signal<InscripcionReservationData | null>(null);
  public readonly studentNumber = computed(() => this.confirmedDetail()?.numeroEstudiante ?? null);
  public readonly coordinators = computed<readonly ContactoCoordinador[]>(() => {
    const detail = this.confirmedDetail();
    return [
      toCoordinatorContact('Coordinador(a) Académico:', detail?.coordinadorAcademico),
      toCoordinatorContact('Coordinador(a) de Cursos:', detail?.coordinadorCursos),
    ].filter((contact): contact is ContactoCoordinador => contact !== null);
  });
  // En Actualización profesional cada seminario confirmado trae sus propias materias:
  // se listan todas juntas, sin repetir las que comparten varios seminarios.
  private readonly subjects = computed<readonly string[]>(() => [
    ...new Set(
      (this.confirmedDetail()?.inscripciones ?? [])
        .flatMap(inscripcion => inscripcion.materiasPrimerSemestre)
        .map(materia => materia.nombre?.trim())
        .filter((nombre): nombre is string => !!nombre)
    ),
  ]);

  public readonly bankOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly loadingBanks = signal(false);
  private banksRequested = false;

  private readonly submitted = signal(false);
  private readonly paymentApiError = signal<string | null>(null);
  public readonly view = signal<InscripcionPaymentView>('editing');
  public readonly outcome = signal<InscripcionOutcome | null>(null);
  public readonly selectedPaymentMethod = signal<MetodoPago | null>(null);
  public readonly showAllSubjects = signal(false);

  private readonly forcedResult = parseResultadoForzado(
    this.route.snapshot.queryParamMap.get('resultado')
  );

  public readonly paymentErrorAlert = computed<ErrorAlertState | null>(() => {
    if (!this.submitted()) return null;
    if (this.paymentForm.controls.metodoPago.hasError('required')) {
      return {
        title: 'Medio de pago requerido',
        message: 'Elegí un medio de pago para poder continuar.',
      };
    }
    if (this.paymentForm.controls.banco.hasError('required')) {
      return {
        title: 'Banco requerido',
        message: 'Seleccioná tu banco para poder continuar.',
      };
    }

    const apiError = this.paymentApiError();
    return apiError
      ? {
          title: 'No pudimos procesar el pago',
          message: apiError,
        }
      : null;
  });
  public readonly isProfessionalUpdate = computed(() =>
    this.proposal.selection.isProfessionalUpdate()
  );
  private readonly seminariosSeleccionados = computed(() =>
    this.isProfessionalUpdate() ? this.buildProfessionalUpdateSummary() : []
  );
  public readonly summaryItems = computed(() =>
    buildSummaryItems({
      response: this.process.preEnrollmentResponse(),
      selectedCareer: this.proposal.academicForm.controls.degreeProgram.value,
      selectedStart: this.proposal.academicForm.controls.intake.value,
      selectedTurno: this.proposal.academicForm.controls.shift.value,
      careerOptions: this.proposal.careerOptions(),
      startOptions: this.proposal.startOptions(),
      turnoOptions: this.proposal.turnoOptions(),
      isProfessionalUpdate: this.isProfessionalUpdate(),
      seminarios: this.seminariosSeleccionados(),
    })
  );
  // Con un solo seminario el comienzo ya va como fila del resumen: no se lista el bloque.
  public readonly seminariosResumen = computed(() => {
    const seminarios = this.seminariosSeleccionados();
    return seminarios.length > 1 ? seminarios : [];
  });
  public readonly paymentDeadline = computed(() =>
    formatPaymentDeadline(this.process.preEnrollmentResponse()?.fechaVencimientoPago)
  );
  public readonly inscriptionAmount = computed(() =>
    formatInscriptionAmount(this.process.preEnrollmentResponse()?.seniaInscripcion)
  );
  public readonly visibleSubjects = computed(() =>
    this.showAllSubjects() ? this.subjects() : this.subjects().slice(0, 4)
  );
  public readonly canToggleSubjects = computed(() => this.subjects().length > 4);
  public readonly subjectsToggleLabel = computed(() =>
    this.showAllSubjects() ? 'Ver menos materias' : 'Ver todas las materias'
  );
  public readonly reservationInstructions = computed(() =>
    buildReservationInstructions(
      this.selectedPaymentMethod(),
      this.process.preEnrollmentResponse(),
      this.reservationData()
    )
  );

  constructor() {
    effect(() => {
      if (
        this.process.flow.currentStep() === 'pago' &&
        this.view() === 'editing' &&
        this.outcome() === null
      ) {
        this.loadBanks();
      }
    });
    this.configureBankValidator();
  }

  private loadBanks(): void {
    if (this.banksRequested) return;
    this.banksRequested = true;
    this.loadingBanks.set(true);
    this.catalogs
      .getBanks()
      .pipe(
        finalize(() => this.loadingBanks.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: banks => this.bankOptions.set(toBankOptions(banks)),
        error: () => this.bankOptions.set(FALLBACK_BANK_OPTIONS),
      });
  }

  private configureBankValidator(): void {
    this.paymentForm.controls.metodoPago.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(method => this.syncBankValidator(method));
    this.syncBankValidator(this.paymentForm.controls.metodoPago.value);
  }

  private syncBankValidator(method: MetodoPago | ''): void {
    const bankControl = this.paymentForm.controls.banco;
    bankControl.setValidators(method === 'cuenta-bancaria' ? Validators.required : null);
    if (method !== 'cuenta-bancaria') bankControl.setValue('', { emitEvent: false });
    bankControl.updateValueAndValidity({ emitEvent: false });
  }

  public requestConfirmation(): void {
    this.submitted.set(true);
    this.paymentApiError.set(null);
    if (!this.isSelectedPaymentMethodAvailable()) {
      this.paymentForm.controls.metodoPago.setValue('');
      this.paymentForm.markAllAsTouched();
      return;
    }
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }
    this.view.set('confirming');
  }

  public cancelConfirmation(): void {
    if (this.view() === 'confirming') this.view.set('editing');
  }

  public confirm(): void {
    if (this.view() === 'processing') return;
    const method = this.paymentForm.controls.metodoPago.value;
    const idsInscripcion = this.paymentInscriptionIds();
    if (!method) return;
    if (idsInscripcion.length === 0) {
      this.paymentApiError.set('No pudimos identificar la inscripción pendiente.');
      this.view.set('editing');
      return;
    }

    this.selectedPaymentMethod.set(method);
    this.paymentApiError.set(null);
    this.view.set('processing');
    this.inscriptions
      .pay({
        idsInscripcion,
        metodoPago: method,
        idBancoSistarbanc:
          method === 'cuenta-bancaria' ? this.paymentForm.controls.banco.value : null,
      })
      .pipe(
        finalize(() => {
          if (this.view() === 'processing' && !this.outcome()) this.view.set('editing');
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: response => this.resolvePaymentResponse(method, response),
        error: () => {
          this.paymentApiError.set('Intentá nuevamente en unos minutos.');
        },
      });
  }

  public toggleSubjects(): void {
    this.showAllSubjects.update(showAll => !showAll);
  }

  // La respuesta del backend manda. Al retomar, sessionStorage conserva los ids de la
  // tarjeta como fallback si ConfirmarPreInscripcion no devuelve `inscripciones`.
  private paymentInscriptionIds(): number[] {
    const response = this.process.preEnrollmentResponse();
    const responseIds = [
      ...(response?.seminarios ?? []).map(seminario => seminario.idInscripcion),
      response?.idInscripcion,
    ].filter(isPositiveInteger);
    if (responseIds.length) return [...new Set(responseIds)];

    const idProducto = toPositiveInteger(this.route.snapshot.queryParamMap.get('idProducto'));
    const idProceso = toPositiveInteger(this.route.snapshot.queryParamMap.get('idProceso'));
    if (!idProducto || !idProceso) return [];

    return this.resumeContext.read(idProducto, idProceso)?.idInscripciones ?? [];
  }

  private buildProfessionalUpdateSummary(): ItemSeminarioResumen[] {
    const responseSummary = buildSeminariosSummary(this.process.preEnrollmentResponse());
    if (responseSummary.length) return responseSummary;

    const selectedOffers = new Set(
      this.proposal.academicForm.controls.seminars.value.map(Number).filter(isPositiveInteger)
    );
    return this.proposal.selection
      .seminars()
      .filter(seminario => selectedOffers.has(seminario.offeringId))
      .map(seminario => ({
        idInscripcion: null,
        nombre: seminario.name,
        comienzo: formatPaymentDeadline(seminario.startDate),
        turno: 'No informado',
      }));
  }

  private resolvePaymentResponse(method: MetodoPago, response: InscripcionPaymentResponse): void {
    if (!response.success) {
      this.paymentApiError.set(getPaymentErrorMessage(response));
      return;
    }

    // Si el backend confirmó el pago en línea ya trae el detalle (número de
    // estudiante, coordinación y materias): lo usamos directo y evitamos el
    // getDetail posterior.
    if (response.confirmada) this.confirmedDetail.set(response.confirmada);

    const result = this.forcedResult ?? normalizePaymentResult(response.resultado);
    if (result === 'confirmada') {
      this.finishAt('inscription-confirmada');
      return;
    }
    if (result === 'en-proceso') {
      this.finishAt('inscription-en-proceso');
      return;
    }
    if (method === 'abitab' || method === 'paganza') {
      this.finishAt('reserva');
      return;
    }
    if (isExternalPaymentMethod(method)) {
      if (!this.submitExternalPayment(response)) return;
      this.finishAt('pago-pendiente-externo');
      return;
    }
    if (result === 'reservada') {
      this.finishAt('reserva');
      return;
    }

    this.finishAt('inscription-confirmada');
  }

  private submitExternalPayment(response: InscripcionPaymentResponse): boolean {
    if (!hasText(response.urlPago) || !hasText(response.parametrosEncriptados)) {
      this.paymentApiError.set(
        'La pasarela no devolvió los datos necesarios para iniciar el pago.'
      );
      return false;
    }
    if (
      !this.externalPaymentSubmitter.submit({
        urlPago: response.urlPago,
        parametrosEncriptados: response.parametrosEncriptados,
      })
    ) {
      this.paymentApiError.set('La pasarela devolvió una URL inválida.');
      return false;
    }

    return true;
  }

  private resolvePaymentOption(option: PaymentOption): readonly PaymentOption[] {
    if (option.value !== 'cuenta-personal') return [option];

    const amount = this.process.preEnrollmentResponse()?.seniaInscripcion;
    const availableAmount = this.process.preEnrollmentResponse()?.saldoCuenta;
    if (!isPositiveAmount(amount)) return [];

    return [
      {
        ...option,
        hint: `Monto disponible ${formatInscriptionAmount(availableAmount)}`,
        availableAmount: availableAmount ?? undefined,
        disabled: amount > (availableAmount ?? 0),
      },
    ];
  }

  private isSelectedPaymentMethodAvailable(): boolean {
    const selected = this.paymentForm.controls.metodoPago.value;
    if (!selected) return true;
    return this.paymentOptions().some(option => option.value === selected && !option.disabled);
  }

  private finishAt(outcome: InscripcionOutcome): void {
    this.outcome.set(outcome);
    if (outcome === 'inscription-confirmada' && !this.confirmedDetail()) {
      this.loadDetail(detail => this.confirmedDetail.set(detail.confirmada));
    }
    if (outcome === 'reserva' && !this.reservationData()) {
      this.loadDetail(detail => this.applyReservationDetail(detail));
    }
  }

  private applyReservationDetail(detail: InscripcionDetail): void {
    const senia = detail.seniaMinima;
    if (!senia) return;
    this.reservationData.set({ cedula: senia.cedula, codigoPersona: senia.codigoPersona });
  }

  // Carga el Detalle y aplica lo que necesite cada pantalla terminal. Los ids
  // salen de la propuesta académica en el flujo completo, o de los query params
  // al retomar el pago desde el panel. Si no hay ids o falla, no rompe: la
  // pantalla degrada a lo que ya tenga.
  private loadDetail(apply: (detail: InscripcionDetail) => void): void {
    const idProducto = this.resolveCatalogId(
      this.proposal.academicForm.controls.degreeProgram.value,
      'idProducto'
    );
    const idProceso = this.resolveCatalogId(
      this.proposal.academicForm.controls.intake.value,
      'idProceso'
    );
    if (idProducto === null || idProceso === null) return;

    const estado = this.route.snapshot.queryParamMap.get('estado');

    this.inscriptions
      .getDetail(idProducto, idProceso, estado)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: detail => apply(detail),
        error: () => undefined,
      });
  }

  // En el flujo completo los ids salen de la propuesta académica; al retomar el
  // pago desde el panel esa sección está vacía y los ids vienen por query params
  // (los mismos que usa inscriptionDetailResolver).
  private resolveCatalogId(formValue: string, queryParam: string): number | null {
    return (
      toPositiveInteger(formValue) ??
      toPositiveInteger(this.route.snapshot.queryParamMap.get(queryParam))
    );
  }
}

function toPositiveInteger(value: string | null): number | null {
  if (!value || !/^\d+$/.test(value)) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : null;
}

function toCoordinatorContact(
  role: string,
  coordinator: InscripcionCoordinador | null | undefined
): ContactoCoordinador | null {
  if (!coordinator?.nombre || !coordinator.email) return null;
  return { role, name: coordinator.nombre, email: coordinator.email };
}

function isPositiveAmount(value: number | null | undefined): value is number {
  return typeof value === 'number' && Number.isFinite(value) && value > 0;
}

function isPositiveInteger(value: number | null | undefined): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function hasText(value: string | null): value is string {
  return typeof value === 'string' && value.trim().length > 0;
}

function isExternalPaymentMethod(method: MetodoPago): boolean {
  return method === 'cuenta-bancaria' || method === 'banred' || method === 'geopay';
}

function normalizePaymentResult(value: string | null): ResultadoPago | null {
  const normalized = (value ?? '').toLowerCase();
  if (normalized.includes('confirm')) return 'confirmada';
  if (normalized.includes('reserv')) return 'reservada';
  if (normalized.includes('proceso')) return 'en-proceso';
  return null;
}

function getPaymentErrorMessage(response: InscripcionPaymentResponse): string {
  return (
    response.message ??
    response.mensajes.find(message => message.valor?.trim())?.valor ??
    'Intentá nuevamente en unos minutos.'
  );
}
