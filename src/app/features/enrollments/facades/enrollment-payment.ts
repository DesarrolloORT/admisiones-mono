import { computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs/operators';
import type { ErrorAlertState } from 'src/app/shared/ui/error-alert/error-alert';

import { Catalogs } from '../../catalogs/services/catalogs';
import { FALLBACK_BANK_OPTIONS, toBankOptions } from '../models/enrollment-bank-logo';
import type {
  EnrollmentConfirmedDetail,
  EnrollmentCoordinator,
  EnrollmentDetail,
} from '../models/enrollment-detail';
import type {
  CoordinatorContact,
  EnrollmentOption,
  EnrollmentPaymentResponse,
  EnrollmentReservationData,
  PaymentMethod,
  PaymentResult,
  SeminarSummaryItem,
} from '../models/enrollment-flow';
import { parseForcedResult } from '../models/enrollment-flow-policy';
import {
  buildReservationInstructions,
  buildSeminarsSummary,
  buildSummaryItems,
  formatEnrollmentAmount,
  formatPaymentDeadline,
} from '../models/enrollment-flow-view';
import type { EnrollmentOutcome, EnrollmentPaymentView } from '../models/enrollment-process';
import {
  PAYMENT_OPTIONS,
  type PaymentOption,
  SANTANDER_ACCOUNT_URL,
  STUDENT_SERVICE_LINKS,
} from '../models/enrollment-static-data';
import { EnrollmentResumeContextStore } from '../services/enrollment-resume-context';
import { Enrollments } from '../services/enrollments';
import { ExternalPaymentSubmitter } from '../services/external-payment-submitter';
import { EnrollmentFormsStore } from '../store/enrollment-forms';
import { EnrollmentProcessStore } from '../store/enrollment-process';
import { EnrollmentProposalFacade } from './enrollment-proposal';

export class EnrollmentPaymentFacade {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly catalogs = inject(Catalogs);
  private readonly enrollments = inject(Enrollments);
  private readonly resumeContext = inject(EnrollmentResumeContextStore);
  private readonly externalPaymentSubmitter = inject(ExternalPaymentSubmitter);
  private readonly formsStore = inject(EnrollmentFormsStore);
  private readonly process = inject(EnrollmentProcessStore);
  private readonly proposal = inject(EnrollmentProposalFacade);

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
  public readonly confirmedDetail = signal<EnrollmentConfirmedDetail | null>(null);
  // Datos para pagar la reserva (Abitab/Paganza): cédula y número de estudiante.
  // Lo setea applyPaymentInit al retomar, o se carga vía getDetail al quedar en
  // reserva desde el flujo fresco. Si falla queda null y la pantalla muestra solo
  // el monto.
  public readonly reservationData = signal<EnrollmentReservationData | null>(null);
  public readonly studentNumber = computed(() => this.confirmedDetail()?.studentNumber ?? null);
  public readonly coordinators = computed<readonly CoordinatorContact[]>(() => {
    const detail = this.confirmedDetail();
    return [
      toCoordinatorContact('Coordinador(a) Académico:', detail?.academicCoordinator),
      toCoordinatorContact('Coordinador(a) de Cursos:', detail?.courseCoordinator),
    ].filter((contact): contact is CoordinatorContact => contact !== null);
  });
  // En Actualización profesional cada seminario confirmado trae sus propias materias:
  // se listan todas juntas, sin repetir las que comparten varios seminarios.
  private readonly subjects = computed<readonly string[]>(() => [
    ...new Set(
      (this.confirmedDetail()?.enrollments ?? [])
        .flatMap(enrollment => enrollment.firstSemesterSubjects)
        .map(subject => subject.name?.trim())
        .filter((name): name is string => !!name)
    ),
  ]);

  public readonly bankOptions = signal<readonly EnrollmentOption[]>([]);
  public readonly loadingBanks = signal(false);
  private banksRequested = false;

  private readonly submitted = signal(false);
  private readonly paymentApiError = signal<string | null>(null);
  public readonly view = signal<EnrollmentPaymentView>('editing');
  public readonly outcome = signal<EnrollmentOutcome | null>(null);
  public readonly selectedPaymentMethod = signal<PaymentMethod | null>(null);
  public readonly showAllSubjects = signal(false);

  private readonly forcedResult = parseForcedResult(
    this.route.snapshot.queryParamMap.get('resultado')
  );

  public readonly paymentErrorAlert = computed<ErrorAlertState | null>(() => {
    if (!this.submitted()) return null;
    if (this.paymentForm.controls.paymentMethod.hasError('required')) {
      return {
        title: 'Medio de pago requerido',
        message: 'Elegí un medio de pago para poder continuar.',
      };
    }
    if (this.paymentForm.controls.bank.hasError('required')) {
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
  private readonly selectedSeminars = computed(() =>
    this.isProfessionalUpdate() ? this.buildProfessionalUpdateSummary() : []
  );
  public readonly summaryItems = computed(() =>
    buildSummaryItems({
      response: this.process.preEnrollmentResponse(),
      selectedCareer: this.proposal.academicForm.controls.degreeProgram.value,
      selectedStart: this.proposal.academicForm.controls.intake.value,
      selectedShift: this.proposal.academicForm.controls.shift.value,
      careerOptions: this.proposal.careerOptions(),
      startOptions: this.proposal.startOptions(),
      shiftOptions: this.proposal.shiftOptions(),
      isProfessionalUpdate: this.isProfessionalUpdate(),
      seminars: this.selectedSeminars(),
    })
  );
  // Con un solo seminario el comienzo ya va como fila del resumen: no se lista el bloque.
  public readonly seminarsSummary = computed(() => {
    const seminars = this.selectedSeminars();
    return seminars.length > 1 ? seminars : [];
  });
  public readonly paymentDeadline = computed(() =>
    formatPaymentDeadline(this.process.preEnrollmentResponse()?.paymentDueDate)
  );
  public readonly enrollmentAmount = computed(() =>
    formatEnrollmentAmount(this.process.preEnrollmentResponse()?.enrollmentDeposit)
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
        this.process.flow.currentStep() === 'payment' &&
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
    this.paymentForm.controls.paymentMethod.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(method => this.syncBankValidator(method));
    this.syncBankValidator(this.paymentForm.controls.paymentMethod.value);
  }

  private syncBankValidator(method: PaymentMethod | ''): void {
    const bankControl = this.paymentForm.controls.bank;
    bankControl.setValidators(method === 'bank-account' ? Validators.required : null);
    if (method !== 'bank-account') bankControl.setValue('', { emitEvent: false });
    bankControl.updateValueAndValidity({ emitEvent: false });
  }

  public requestConfirmation(): void {
    this.submitted.set(true);
    this.paymentApiError.set(null);
    if (!this.isSelectedPaymentMethodAvailable()) {
      this.paymentForm.controls.paymentMethod.setValue('');
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
    const method = this.paymentForm.controls.paymentMethod.value;
    const enrollmentIds = this.paymentEnrollmentIds();
    if (!method) return;
    if (enrollmentIds.length === 0) {
      this.paymentApiError.set('No pudimos identificar la inscripción pendiente.');
      this.view.set('editing');
      return;
    }

    this.selectedPaymentMethod.set(method);
    this.paymentApiError.set(null);
    this.view.set('processing');
    this.enrollments
      .pay({
        enrollmentIds,
        paymentMethod: method,
        sistarbancBankId: method === 'bank-account' ? this.paymentForm.controls.bank.value : null,
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
  // tarjeta como fallback si ConfirmarPreEnrollment no devuelve `inscripciones`.
  private paymentEnrollmentIds(): number[] {
    const response = this.process.preEnrollmentResponse();
    const responseIds = [
      ...(response?.seminars ?? []).map(seminar => seminar.idEnrollment),
      response?.idEnrollment,
    ].filter(isPositiveInteger);
    if (responseIds.length) return [...new Set(responseIds)];

    const productId = toPositiveInteger(this.route.snapshot.queryParamMap.get('idProducto'));
    const admissionProcessId = toPositiveInteger(
      this.route.snapshot.queryParamMap.get('idProceso')
    );
    if (!productId || !admissionProcessId) return [];

    return this.resumeContext.read(productId, admissionProcessId)?.enrollmentIds ?? [];
  }

  private buildProfessionalUpdateSummary(): SeminarSummaryItem[] {
    const responseSummary = buildSeminarsSummary(this.process.preEnrollmentResponse());
    if (responseSummary.length) return responseSummary;

    const selectedOffers = new Set(
      this.proposal.academicForm.controls.seminars.value.map(Number).filter(isPositiveInteger)
    );
    return this.proposal.selection
      .seminars()
      .filter(seminar => selectedOffers.has(seminar.offeringId))
      .map(seminar => ({
        idEnrollment: null,
        name: seminar.name,
        intake: formatPaymentDeadline(seminar.startDate),
        shift: 'No informado',
      }));
  }

  private resolvePaymentResponse(method: PaymentMethod, response: EnrollmentPaymentResponse): void {
    if (!response.success) {
      this.paymentApiError.set(getPaymentErrorMessage(response));
      return;
    }

    // Si el backend confirmó el pago en línea ya trae el detalle (número de
    // estudiante, coordinación y materias): lo usamos directo y evitamos el
    // getDetail posterior.
    if (response.confirmed) this.confirmedDetail.set(response.confirmed);

    const result = this.forcedResult ?? normalizePaymentResult(response.result);
    if (result === 'confirmed') {
      this.finishAt('enrollment-confirmed');
      return;
    }
    if (result === 'in-progress') {
      this.finishAt('enrollment-in-progress');
      return;
    }
    if (method === 'abitab' || method === 'paganza') {
      this.finishAt('reservation');
      return;
    }
    if (isExternalPaymentMethod(method)) {
      if (!this.submitExternalPayment(response)) return;
      this.finishAt('external-payment-pending');
      return;
    }
    if (result === 'reserved') {
      this.finishAt('reservation');
      return;
    }

    this.finishAt('enrollment-confirmed');
  }

  private submitExternalPayment(response: EnrollmentPaymentResponse): boolean {
    if (!hasText(response.paymentUrl) || !hasText(response.encryptedParameters)) {
      this.paymentApiError.set(
        'La pasarela no devolvió los datos necesarios para iniciar el pago.'
      );
      return false;
    }
    if (
      !this.externalPaymentSubmitter.submit({
        paymentUrl: response.paymentUrl,
        encryptedParameters: response.encryptedParameters,
      })
    ) {
      this.paymentApiError.set('La pasarela devolvió una URL inválida.');
      return false;
    }

    return true;
  }

  private resolvePaymentOption(option: PaymentOption): readonly PaymentOption[] {
    if (option.value !== 'personal-account') return [option];

    const amount = this.process.preEnrollmentResponse()?.enrollmentDeposit;
    const availableAmount = this.process.preEnrollmentResponse()?.accountBalance;
    if (!isPositiveAmount(amount)) return [];

    return [
      {
        ...option,
        hint: `Monto disponible ${formatEnrollmentAmount(availableAmount)}`,
        availableAmount: availableAmount ?? undefined,
        disabled: amount > (availableAmount ?? 0),
      },
    ];
  }

  private isSelectedPaymentMethodAvailable(): boolean {
    const selected = this.paymentForm.controls.paymentMethod.value;
    if (!selected) return true;
    return this.paymentOptions().some(option => option.value === selected && !option.disabled);
  }

  private finishAt(outcome: EnrollmentOutcome): void {
    this.outcome.set(outcome);
    if (outcome === 'enrollment-confirmed' && !this.confirmedDetail()) {
      this.loadDetail(detail => this.confirmedDetail.set(detail.confirmed));
    }
    if (outcome === 'reservation' && !this.reservationData()) {
      this.loadDetail(detail => this.applyReservationDetail(detail));
    }
  }

  private applyReservationDetail(detail: EnrollmentDetail): void {
    const deposit = detail.minimumDeposit;
    if (!deposit) return;
    this.reservationData.set({
      documentNumber: deposit.documentNumber,
      personCode: deposit.personCode,
    });
  }

  // Carga el Detalle y aplica lo que necesite cada pantalla terminal. Los ids
  // salen de la propuesta académica en el flujo completo, o de los query params
  // al retomar el pago desde el panel. Si no hay ids o falla, no rompe: la
  // pantalla degrada a lo que ya tenga.
  private loadDetail(apply: (detail: EnrollmentDetail) => void): void {
    const productId = this.resolveCatalogId(
      this.proposal.academicForm.controls.degreeProgram.value,
      'idProducto'
    );
    const admissionProcessId = this.resolveCatalogId(
      this.proposal.academicForm.controls.intake.value,
      'idProceso'
    );
    if (productId === null || admissionProcessId === null) return;

    const status = this.route.snapshot.queryParamMap.get('estado');

    this.enrollments
      .getDetail(productId, admissionProcessId, status)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: detail => apply(detail),
        error: () => undefined,
      });
  }

  // En el flujo completo los ids salen de la propuesta académica; al retomar el
  // pago desde el panel esa sección está vacía y los ids vienen por query params
  // (los mismos que usa enrollmentDetailResolver).
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
  coordinator: EnrollmentCoordinator | null | undefined
): CoordinatorContact | null {
  if (!coordinator?.name || !coordinator.email) return null;
  return { role, name: coordinator.name, email: coordinator.email };
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

function isExternalPaymentMethod(method: PaymentMethod): boolean {
  return method === 'bank-account' || method === 'banred' || method === 'geopay';
}

function normalizePaymentResult(value: string | null): PaymentResult | null {
  const normalized = (value ?? '').toLowerCase();
  if (normalized.includes('confirm')) return 'confirmed';
  if (normalized.includes('reserv')) return 'reserved';
  if (normalized.includes('proceso')) return 'in-progress';
  return null;
}

function getPaymentErrorMessage(response: EnrollmentPaymentResponse): string {
  return (
    response.message ??
    response.messages.find(message => message.value?.trim())?.value ??
    'Intentá nuevamente en unos minutos.'
  );
}
