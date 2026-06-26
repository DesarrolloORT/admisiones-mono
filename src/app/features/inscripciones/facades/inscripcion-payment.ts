import { computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs/operators';

import { Catalogs } from '../../catalogs/services/catalogs';
import { toBankOption } from '../models/inscripcion-bank-logo';
import type { MetodoPago, OpcionInscripcion } from '../models/inscripcion-flow';
import { getResultadoPago, parseResultadoForzado } from '../models/inscripcion-flow-policy';
import {
  buildSummaryItems,
  formatInscriptionAmount,
  formatPaymentDeadline,
  getReservationInstructions,
} from '../models/inscripcion-flow-view';
import type { InscripcionOutcome, InscripcionPaymentView } from '../models/inscripcion-process';
import { COORDINATORS, PAYMENT_OPTIONS, SUBJECTS } from '../models/inscripcion-static-data';
import { InscripcionFormsStore } from '../store/inscripcion-forms';
import { InscripcionProcessStore } from '../store/inscripcion-process';
import { InscripcionProposalFacade } from './inscripcion-proposal';

interface InscripcionErrorAlertState {
  title: string;
  message: string;
}

export class InscripcionPaymentFacade {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly catalogs = inject(Catalogs);
  private readonly formsStore = inject(InscripcionFormsStore);
  private readonly process = inject(InscripcionProcessStore);
  private readonly proposal = inject(InscripcionProposalFacade);
  private processingTimer: ReturnType<typeof setTimeout> | null = null;

  public readonly paymentForm = this.formsStore.paymentForm;
  public readonly paymentOptions = PAYMENT_OPTIONS;
  public readonly coordinators = COORDINATORS;
  public readonly studentNumber = '397654';

  public readonly bankOptions = signal<readonly OpcionInscripcion[]>([]);
  public readonly loadingBanks = signal(false);
  public readonly bankLoadError = signal<string | null>(null);

  private readonly submitted = signal(false);
  public readonly view = signal<InscripcionPaymentView>('editing');
  public readonly outcome = signal<InscripcionOutcome | null>(null);
  public readonly selectedPaymentMethod = signal<MetodoPago | null>(null);
  public readonly showAllSubjects = signal(false);

  private readonly forcedResult = parseResultadoForzado(
    this.route.snapshot.queryParamMap.get('resultado')
  );

  public readonly paymentErrorAlert = computed<InscripcionErrorAlertState | null>(() => {
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
    return null;
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
    this.destroyRef.onDestroy(() => {
      if (this.processingTimer) clearTimeout(this.processingTimer);
    });
    this.loadBanks();
    this.configureBankValidator();
  }

  private loadBanks(): void {
    this.loadingBanks.set(true);
    this.bankLoadError.set(null);
    this.catalogs
      .getBancos()
      .pipe(
        finalize(() => this.loadingBanks.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: banks => this.bankOptions.set(banks.map(toBankOption)),
        error: () =>
          this.bankLoadError.set('No se pudieron cargar los bancos. Intentá nuevamente.'),
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
    if (!method) return;

    this.selectedPaymentMethod.set(method);
    const result = getResultadoPago(method, this.forcedResult);
    if (result === 'reservada') {
      this.finishAt('reserva');
      return;
    }
    if (result === 'en-proceso') {
      this.finishAt('inscripcion-en-proceso');
      return;
    }

    this.view.set('processing');
    this.processingTimer = setTimeout(() => {
      this.processingTimer = null;
      this.finishAt('inscripcion-confirmada');
    }, 1000);
  }

  public toggleSubjects(): void {
    this.showAllSubjects.update(showAll => !showAll);
  }

  public restore(
    method: MetodoPago | '',
    response: ReturnType<InscripcionProcessStore['preEnrollmentResponse']>
  ): void {
    this.paymentForm.controls.metodoPago.setValue(method, { emitEvent: false });
    this.syncBankValidator(method);
    this.process.preEnrollmentResponse.set(response);
    this.view.set('editing');
  }

  private finishAt(outcome: InscripcionOutcome): void {
    this.outcome.set(outcome);
    this.process.markCheckpoint();
  }
}
