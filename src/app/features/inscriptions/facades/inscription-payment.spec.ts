import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, vi } from 'vitest';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import { FALLBACK_BANK_OPTIONS } from '../models/inscription-bank-logo';
import type { InscripcionPaymentResponse, MetodoPago } from '../models/inscription-flow';
import { ExternalPaymentSubmitter } from '../services/external-payment-submitter';
import { Inscripciones } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionPaymentFacade } from './inscription-payment';
import { InscripcionProposalFacade } from './inscription-proposal';

const PAYMENT_OK: InscripcionPaymentResponse = {
  success: true,
  resultado: null,
  urlPago: null,
  parametrosEncriptados: null,
  mensajes: [],
  message: null,
  errorCode: null,
};

const CONFIRMED_DETAIL = {
  numeroEstudiante: 412001,
  resumen: null,
  coordinadorAcademico: { nombre: 'Laura Pérez', email: 'laura.perez@ort.edu.uy' },
  coordinadorCursos: { nombre: 'Diego Cursos', email: 'diego.cursos@ort.edu.uy' },
  materiasPrimerSemestre: [
    { idMateria: 1, nombre: 'Programación I' },
    { idMateria: 2, nombre: null },
  ],
};

describe('InscripcionPaymentFacade', () => {
  let facade: InscripcionPaymentFacade;
  let process: InscripcionProcessStore;
  let inscriptions: { pay: ReturnType<typeof vi.fn>; getDetail: ReturnType<typeof vi.fn> };
  let externalPaymentSubmitter: { submit: ReturnType<typeof vi.fn> };

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  beforeEach(() => {
    inscriptions = {
      pay: vi.fn().mockReturnValue(of(PAYMENT_OK)),
      getDetail: vi.fn().mockReturnValue(
        of({
          estado: 'Confirmada',
          detalle: null,
          pagoPendiente: null,
          confirmada: CONFIRMED_DETAIL,
        })
      ),
    };
    externalPaymentSubmitter = { submit: vi.fn().mockReturnValue(true) };
    configureFacade();
  });

  function configureFacade(
    options: {
      queryParams?: Record<string, string>;
      bancos?: Observable<readonly { id: number; label: string; code: string }[]>;
    } = {}
  ): void {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        AcademicProposalSelection,
        InscripcionFormsStore,
        InscripcionProcessStore,
        InscripcionProposalFacade,
        InscripcionPaymentFacade,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(options.queryParams ?? {}) } },
        },
        {
          provide: Catalogs,
          useValue: {
            getCareers: () => of([]),
            getComienzos: () => of([]),
            getTurnos: () => of([]),
            getBancos: () => options.bancos ?? of([{ id: 1, label: 'BROU', code: 'brou' }]),
          },
        },
        { provide: Inscripciones, useValue: inscriptions },
        { provide: ExternalPaymentSubmitter, useValue: externalPaymentSubmitter },
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
  }

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

  it('submits external payment methods and leaves an explicit pending state', () => {
    const externalMethods: readonly MetodoPago[] = ['banred', 'geopay', 'cuenta-bancaria'];
    for (const method of externalMethods) {
      inscriptions.pay.mockClear();
      externalPaymentSubmitter.submit.mockClear();
      facade.outcome.set(null);
      facade.view.set('editing');
      facade.paymentForm.controls.metodoPago.setValue(method);
      facade.paymentForm.controls.banco.setValue(method === 'cuenta-bancaria' ? 'brou' : '');
      inscriptions.pay.mockReturnValueOnce(
        of({
          ...PAYMENT_OK,
          urlPago: `https://pagos.example/${method}`,
          parametrosEncriptados: `token-${method}`,
        })
      );

      facade.requestConfirmation();
      facade.confirm();

      expect(inscriptions.pay).toHaveBeenCalledWith({
        idInscripcion: 1072704,
        metodoPago: method,
        idBancoSistarbanc: method === 'cuenta-bancaria' ? 'brou' : null,
      });
      expect(externalPaymentSubmitter.submit).toHaveBeenCalledWith({
        urlPago: `https://pagos.example/${method}`,
        parametrosEncriptados: `token-${method}`,
      });
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

  it('loads the confirmed detail after a confirmed payment', () => {
    const forms = TestBed.inject(InscripcionFormsStore);
    forms.academicForm.controls.carrera.setValue('20');
    forms.academicForm.controls.comienzo.setValue('200');
    inscriptions.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, resultado: 'confirmada' }));
    facade.paymentForm.controls.metodoPago.setValue('cuenta-personal');

    facade.requestConfirmation();
    facade.confirm();

    expect(inscriptions.getDetail).toHaveBeenCalledWith(20, 200);
    expect(facade.studentNumber()).toBe(412001);
    expect(facade.coordinators()).toEqual([
      {
        role: 'Coordinador(a) Académico:',
        name: 'Laura Pérez',
        email: 'laura.perez@ort.edu.uy',
      },
      {
        role: 'Coordinador(a) de Cursos:',
        name: 'Diego Cursos',
        email: 'diego.cursos@ort.edu.uy',
      },
    ]);
    expect(facade.visibleSubjects()).toEqual(['Programación I']);
  });

  it('keeps the success screen without detail sections when getDetail fails', () => {
    const forms = TestBed.inject(InscripcionFormsStore);
    forms.academicForm.controls.carrera.setValue('20');
    forms.academicForm.controls.comienzo.setValue('200');
    inscriptions.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, resultado: 'confirmada' }));
    inscriptions.getDetail.mockReturnValueOnce(throwError(() => new Error('network error')));
    facade.paymentForm.controls.metodoPago.setValue('cuenta-personal');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.outcome()).toBe('inscription-confirmada');
    expect(facade.studentNumber()).toBeNull();
    expect(facade.coordinators()).toEqual([]);
    expect(facade.visibleSubjects()).toEqual([]);
  });

  it('skips the detail request when no catalog ids are available', () => {
    inscriptions.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, resultado: 'confirmada' }));
    facade.paymentForm.controls.metodoPago.setValue('cuenta-personal');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.outcome()).toBe('inscription-confirmada');
    expect(inscriptions.getDetail).not.toHaveBeenCalled();
  });

  it('builds reservation instructions with the real deadline and amount', () => {
    process.preEnrollmentResponse.set({
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: '2027-03-04',
      seniaInscripcion: 15500,
      saldoCuenta: null,
      resumen: null,
    });
    facade.selectedPaymentMethod.set('abitab');

    const instructions = facade.reservationInstructions();

    expect(instructions.description).toContain('04/03/2027');
    expect(instructions.items).toContain('Monto a pagar: $ 15.500');
    expect(instructions.items.join(' ')).not.toContain('397654');
  });

  it('falls back to generic reservation instructions without method or data', () => {
    process.preEnrollmentResponse.set(null);
    facade.selectedPaymentMethod.set(null);

    const instructions = facade.reservationInstructions();

    expect(instructions.title).toBe('¡Inscripción reservada!');
    expect(instructions.description).toContain('Realizá el pago de la seña');
    expect(instructions.items).toEqual([]);
  });

  it('ignores a second confirm while a payment is already processing', () => {
    const payment = new Subject<typeof PAYMENT_OK>();
    inscriptions.pay.mockReturnValueOnce(payment.asObservable());
    facade.paymentForm.controls.metodoPago.setValue('abitab');

    facade.requestConfirmation();
    facade.confirm();
    facade.confirm();

    expect(inscriptions.pay).toHaveBeenCalledTimes(1);
    payment.next(PAYMENT_OK);
    payment.complete();
    expect(facade.outcome()).toBe('reserva');
  });

  it('rejects external payment when the gateway returns no URL', () => {
    inscriptions.pay.mockReturnValueOnce(
      of({ ...PAYMENT_OK, urlPago: null, parametrosEncriptados: 'token-encriptado' })
    );
    facade.paymentForm.controls.metodoPago.setValue('banred');

    facade.requestConfirmation();
    facade.confirm();

    expect(externalPaymentSubmitter.submit).not.toHaveBeenCalled();
    expect(facade.outcome()).toBeNull();
    expect(facade.view()).toBe('editing');
    expect(facade.paymentErrorAlert()).toEqual({
      title: 'No pudimos procesar el pago',
      message: 'La pasarela no devolvió los datos necesarios para iniciar el pago.',
    });
  });

  it('rejects external payment when the gateway returns no encrypted params', () => {
    inscriptions.pay.mockReturnValueOnce(
      of({ ...PAYMENT_OK, urlPago: 'https://pagos.example/geopay', parametrosEncriptados: null })
    );
    facade.paymentForm.controls.metodoPago.setValue('geopay');

    facade.requestConfirmation();
    facade.confirm();

    expect(externalPaymentSubmitter.submit).not.toHaveBeenCalled();
    expect(facade.outcome()).toBeNull();
    expect(facade.view()).toBe('editing');
    expect(facade.paymentErrorAlert()).toEqual({
      title: 'No pudimos procesar el pago',
      message: 'La pasarela no devolvió los datos necesarios para iniciar el pago.',
    });
  });

  it('rejects external payment when the submitter rejects the gateway URL', () => {
    externalPaymentSubmitter.submit.mockReturnValueOnce(false);
    inscriptions.pay.mockReturnValueOnce(
      of({
        ...PAYMENT_OK,
        urlPago: 'https://pagos.example/banred',
        parametrosEncriptados: 'token-encriptado',
      })
    );
    facade.paymentForm.controls.metodoPago.setValue('banred');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.outcome()).toBeNull();
    expect(facade.view()).toBe('editing');
    expect(facade.paymentErrorAlert()).toEqual({
      title: 'No pudimos procesar el pago',
      message: 'La pasarela devolvió una URL inválida.',
    });
  });

  it('forces the in-process outcome from the resultado query param', () => {
    configureFacade({ queryParams: { resultado: 'en-proceso' } });
    inscriptions.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, resultado: 'confirmada' }));
    facade.paymentForm.controls.metodoPago.setValue('abitab');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.outcome()).toBe('inscription-en-proceso');
  });

  it('recovers to editing with a visible error when the payment request fails', () => {
    inscriptions.pay.mockReturnValueOnce(throwError(() => new Error('network down')));
    facade.paymentForm.controls.metodoPago.setValue('abitab');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.view()).toBe('editing');
    expect(facade.outcome()).toBeNull();
    expect(facade.paymentErrorAlert()).toEqual({
      title: 'No pudimos procesar el pago',
      message: 'Intentá nuevamente en unos minutos.',
    });
  });

  it('falls back to the static bank options when the catalog fails', () => {
    configureFacade({ bancos: throwError(() => new Error('catalog down')) });

    expect(facade.bankOptions()).toEqual(FALLBACK_BANK_OPTIONS);
    expect(facade.loadingBanks()).toBe(false);
  });

  it('closes the confirmation dialog without paying', () => {
    facade.paymentForm.controls.metodoPago.setValue('abitab');
    facade.requestConfirmation();
    expect(facade.view()).toBe('confirming');

    facade.cancelConfirmation();

    expect(facade.view()).toBe('editing');
    expect(inscriptions.pay).not.toHaveBeenCalled();
  });

  it('reserves on a reservada result for methods without their own branch', () => {
    inscriptions.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, resultado: 'reservada' }));
    facade.paymentForm.controls.metodoPago.setValue('cuenta-personal');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.outcome()).toBe('reserva');
  });

  it('confirms the inscription when the backend result is unknown', () => {
    inscriptions.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, resultado: 'algo-desconocido' }));
    facade.paymentForm.controls.metodoPago.setValue('cuenta-personal');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.outcome()).toBe('inscription-confirmada');
  });

  it('rejects the payment when the pending inscription id is missing', () => {
    process.preEnrollmentResponse.set({
      idInscripcion: null,
      confirmada: false,
      fechaVencimientoPago: null,
      seniaInscripcion: 15500,
      saldoCuenta: 70000,
      resumen: null,
    });
    facade.paymentForm.controls.metodoPago.setValue('abitab');

    facade.requestConfirmation();
    facade.confirm();

    expect(inscriptions.pay).not.toHaveBeenCalled();
    expect(facade.view()).toBe('editing');
    expect(facade.paymentErrorAlert()).toEqual({
      title: 'No pudimos procesar el pago',
      message: 'No pudimos identificar la inscripción pendiente.',
    });
  });
});
