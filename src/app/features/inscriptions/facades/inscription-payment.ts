import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs/operators';
import type { ErrorAlertState } from 'src/app/shared/ui/error-alert/error-alert';

import { Catalogs } from '../../catalogs/services/catalogs';
import { FALLBACK_BANK_OPTIONS, toBankOptions } from '../models/inscription-bank-logo';
import type {
  InscripcionPaymentResponse,
  MetodoPago,
  OpcionInscripcion,
  ResultadoPago,
} from '../models/inscription-flow';
import { parseResultadoForzado } from '../models/inscription-flow-policy';
import {
  buildSummaryItems,
  formatInscriptionAmount,
  formatPaymentDeadline,
  getReservationInstructions,
} from '../models/inscription-flow-view';
import type { InscripcionOutcome, InscripcionPaymentView } from '../models/inscription-process';
import {
  COORDINATORS,
  PAYMENT_OPTIONS,
  type PaymentOption,
  SANTANDER_ACCOUNT_URL,
  STUDENT_SERVICE_LINKS,
  SUBJECTS,
} from '../models/inscription-static-data';
import { Inscripciones } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionProposalFacade } from './inscription-proposal';

export class InscripcionPaymentFacade {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly catalogs = inject(Catalogs);
  private readonly inscriptions = inject(Inscripciones);
  private readonly formsStore = inject(InscripcionFormsStore);
  private readonly process = inject(InscripcionProcessStore);
  private readonly proposal = inject(InscripcionProposalFacade);

  public readonly paymentForm = this.formsStore.paymentForm;
  public readonly paymentOptions = computed<readonly PaymentOption[]>(() =>
    PAYMENT_OPTIONS.flatMap(option => this.resolvePaymentOption(option))
  );
  public readonly coordinators = COORDINATORS;
  public readonly santanderAccountUrl = SANTANDER_ACCOUNT_URL;
  public readonly studentNumber = '397654';
  public readonly studentServices = STUDENT_SERVICE_LINKS;

  public readonly bankOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly loadingBanks = signal(false);

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
  public readonly summaryItems = computed(() =>
    buildSummaryItems({
      response: this.process.preEnrollmentResponse(),
      selectedCareer: this.proposal.academicForm.controls.carrera.value,
      selectedStart: this.proposal.academicForm.controls.comienzo.value,
      selectedTurno: this.proposal.academicForm.controls.turno.value,
      careerOptions: this.proposal.careerOptions(),
      startOptions: this.proposal.startOptions(),
      turnoOptions: this.proposal.turnoOptions(),
    })
  );
  public readonly paymentDeadline = computed(() =>
    formatPaymentDeadline(this.process.preEnrollmentResponse()?.fechaVencimientoPago)
  );
  public readonly inscriptionAmount = computed(() =>
    formatInscriptionAmount(this.process.preEnrollmentResponse()?.seniaInscripcion)
  );
  public readonly visibleSubjects = computed(() =>
    this.showAllSubjects() ? SUBJECTS : SUBJECTS.slice(0, 4)
  );
  public readonly subjectsToggleLabel = computed(() =>
    this.showAllSubjects() ? 'Ver menos materias' : 'Ver todas las materias'
  );
  public readonly reservationInstructions = computed(() =>
    getReservationInstructions(this.selectedPaymentMethod())
  );

  constructor() {
    this.loadBanks();
    this.configureBankValidator();
  }

  private loadBanks(): void {
    this.loadingBanks.set(true);
    this.catalogs
      .getBancos()
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
    const method = this.paymentForm.controls.metodoPago.value;
    const idInscripcion = this.process.preEnrollmentResponse()?.idInscripcion;
    if (!method) return;
    if (!isPositiveInteger(idInscripcion)) {
      this.paymentApiError.set('No pudimos identificar la inscripción pendiente.');
      this.view.set('editing');
      return;
    }

    this.selectedPaymentMethod.set(method);
    this.paymentApiError.set(null);
    this.view.set('processing');
    this.inscriptions
      .pay({
        idInscripcion,
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

  private resolvePaymentResponse(method: MetodoPago, response: InscripcionPaymentResponse): void {
    if (!response.success) {
      this.paymentApiError.set(getPaymentErrorMessage(response));
      return;
    }

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
      if (response.urlPago && !this.redirectToExternalPayment(response.urlPago)) return;
      this.finishAt('pago-pendiente-externo');
      return;
    }
    if (result === 'reservada') {
      this.finishAt('reserva');
      return;
    }

    this.finishAt('inscription-confirmada');
  }

  private redirectToExternalPayment(value: string): boolean {
    const url = toHttpUrl(value);
    if (!url) {
      this.paymentApiError.set('La pasarela devolvió una URL inválida.');
      return false;
    }

    globalThis.location.assign(url);
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
  }
}

function isPositiveAmount(value: number | null | undefined): value is number {
  return typeof value === 'number' && Number.isFinite(value) && value > 0;
}

function isPositiveInteger(value: number | null | undefined): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
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

function toHttpUrl(value: string): string | null {
  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:' ? url.toString() : null;
  } catch {
    return null;
  }
}
