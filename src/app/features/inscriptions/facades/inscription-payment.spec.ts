import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, Subject } from 'rxjs';
import { afterEach, vi } from 'vitest';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import type { InscripcionPaymentResponse, MetodoPago } from '../models/inscription-flow';
import { Inscripciones } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionPaymentFacade } from './inscription-payment';
import { InscripcionProposalFacade } from './inscription-proposal';

const PAYMENT_OK: InscripcionPaymentResponse = {
  success: true,
  resultado: null,
  urlPago: null,
  mensajes: [],
  message: null,
  errorCode: null,
};

describe('InscripcionPaymentFacade', () => {
  let facade: InscripcionPaymentFacade;
  let process: InscripcionProcessStore;
  let inscriptions: { pay: ReturnType<typeof vi.fn> };

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  beforeEach(() => {
    inscriptions = { pay: vi.fn().mockReturnValue(of(PAYMENT_OK)) };

    TestBed.configureTestingModule({
      providers: [
        AcademicProposalSelection,
        InscripcionFormsStore,
        InscripcionProcessStore,
        InscripcionProposalFacade,
        InscripcionPaymentFacade,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({}) } },
        },
        {
          provide: Catalogs,
          useValue: {
            getCareers: () => of([]),
            getComienzos: () => of([]),
            getTurnos: () => of([]),
            getBancos: () => of([{ id: 1, label: 'BROU', code: 'brou' }]),
          },
        },
        { provide: Inscripciones, useValue: inscriptions },
      ],
    });
    facade = TestBed.inject(InscripcionPaymentFacade);
    process = TestBed.inject(InscripcionProcessStore);
    process.preEnrollmentResponse.set({
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: null,
      seniaInscripcion: 15500,
      saldoCuenta: 70000,
      resumen: null,
    });
  });

  it('calls the payment service and shows reservation for Abitab', () => {
    facade.paymentForm.controls.metodoPago.setValue('abitab');

    facade.requestConfirmation();
    facade.confirm();

    expect(inscriptions.pay).toHaveBeenCalledWith({
      idInscripcion: 1072704,
      metodoPago: 'abitab',
      idBancoSistarbanc: null,
    });
    expect(facade.outcome()).toBe('reserva');
  });

  it('shows processing while the payment request is pending', () => {
    const payment = new Subject<typeof PAYMENT_OK>();
    inscriptions.pay.mockReturnValueOnce(payment.asObservable());
    facade.paymentForm.controls.metodoPago.setValue('cuenta-personal');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.view()).toBe('processing');
    payment.next({ ...PAYMENT_OK, resultado: 'confirmada' });
    payment.complete();
    expect(facade.outcome()).toBe('inscription-confirmada');
  });

  it('shows a reusable error alert when no payment method is selected', () => {
    expect(facade.paymentForm.controls.metodoPago.value).toBe('');

    facade.requestConfirmation();

    expect(facade.view()).toBe('editing');
    expect(facade.paymentErrorAlert()).toEqual({
      title: 'Medio de pago requerido',
      message: 'Elegí un medio de pago para poder continuar.',
    });
  });

  it('requires a bank when paying from a bank account', () => {
    facade.paymentForm.controls.metodoPago.setValue('cuenta-bancaria');
    facade.paymentForm.controls.banco.setValue('');

    facade.requestConfirmation();

    expect(facade.view()).toBe('editing');
    expect(facade.paymentForm.controls.banco.hasError('required')).toBe(true);
    expect(facade.paymentErrorAlert()?.title).toBe('Banco requerido');
  });

  it('surfaces payment API errors without leaving the user in processing', () => {
    inscriptions.pay.mockReturnValueOnce(
      of({
        ...PAYMENT_OK,
        success: false,
        message: 'Saldo insuficiente',
      })
    );
    facade.paymentForm.controls.metodoPago.setValue('cuenta-personal');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.view()).toBe('editing');
    expect(facade.paymentErrorAlert()).toEqual({
      title: 'No pudimos procesar el pago',
      message: 'Saldo insuficiente',
    });
  });

  it('redirects external payment methods and leaves an explicit pending state', () => {
    const assign = vi.fn();
    vi.stubGlobal('location', { assign });

    const externalMethods: readonly MetodoPago[] = ['banred', 'geopay', 'cuenta-bancaria'];
    for (const method of externalMethods) {
      inscriptions.pay.mockClear();
      assign.mockClear();
      facade.outcome.set(null);
      facade.view.set('editing');
      facade.paymentForm.controls.metodoPago.setValue(method);
      facade.paymentForm.controls.banco.setValue(method === 'cuenta-bancaria' ? 'brou' : '');
      inscriptions.pay.mockReturnValueOnce(
        of({ ...PAYMENT_OK, urlPago: `https://pagos.example/${method}` })
      );

      facade.requestConfirmation();
      facade.confirm();

      expect(inscriptions.pay).toHaveBeenCalledWith({
        idInscripcion: 1072704,
        metodoPago: method,
        idBancoSistarbanc: method === 'cuenta-bancaria' ? 'brou' : null,
      });
      expect(assign).toHaveBeenCalledWith(`https://pagos.example/${method}`);
      expect(facade.outcome()).toBe('pago-pendiente-externo');
    }
  });

  it('hides personal account payment when no deposit amount is available', () => {
    process.preEnrollmentResponse.set({
      confirmada: false,
      fechaVencimientoPago: null,
      seniaInscripcion: null,
      saldoCuenta: null,
      resumen: null,
    });

    expect(facade.paymentOptions().some(option => option.value === 'cuenta-personal')).toBe(false);
  });

  it('disables personal account payment when its balance does not cover the deposit', () => {
    process.preEnrollmentResponse.set({
      confirmada: false,
      fechaVencimientoPago: null,
      seniaInscripcion: 100000,
      saldoCuenta: 70000,
      resumen: null,
    });

    expect(facade.paymentOptions().find(option => option.value === 'cuenta-personal')).toEqual(
      expect.objectContaining({ disabled: true })
    );
  });

  it('loads bank options with their Sistarbanc code and logos', () => {
    expect(facade.bankOptions()).toEqual([
      { value: 'brou', label: 'BROU', icon: 'assets/banks/brou.svg' },
    ]);
  });
});
