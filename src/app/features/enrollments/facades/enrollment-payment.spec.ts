import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, vi } from 'vitest';

import { CatalogsApi } from '../../catalogs/api/catalogs.api';
import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { EnrollmentsApi } from '../api/enrollments.api';
import { FALLBACK_BANK_OPTIONS } from '../models/enrollment-bank-logo';
import type { EnrollmentPaymentResponse, PaymentMethod } from '../models/enrollment-flow';
import { createEnrollmentFormsState, ENROLLMENT_FORMS } from '../models/enrollment-flow-forms';
import {
  createEnrollmentProcessState,
  ENROLLMENT_PROCESS_STATE,
  type EnrollmentProcessState,
} from '../models/enrollment-process';
import { EnrollmentResumeContextStore } from '../services/enrollment-resume-context';
import { ExternalPaymentSubmitter } from '../services/external-payment-submitter';
import { EnrollmentPaymentFacade } from './enrollment-payment';
import { EnrollmentProposalFacade } from './enrollment-proposal';

const PAYMENT_OK: EnrollmentPaymentResponse = {
  success: true,
  result: null,
  paymentUrl: null,
  encryptedParameters: null,
  messages: [],
  confirmed: null,
  message: null,
  errorCode: null,
};

const CONFIRMED_DETAIL = {
  studentNumber: 412001,
  summary: null,
  academicCoordinator: { name: 'Laura Pérez', email: 'laura.perez@ort.edu.uy' },
  courseCoordinator: { name: 'Diego Cursos', email: 'diego.cursos@ort.edu.uy' },
  enrollments: [
    {
      enrollmentId: 1072704,
      offeringId: 300,
      intake: 'Marzo',
      shift: 'Matutino',
      firstSemesterSubjects: [
        { subjectId: 1, name: 'Programación I' },
        { subjectId: 2, name: null },
      ],
    },
    // Segundo seminario confirmado (Actualización profesional): sus materias también
    // se listan y la que comparte con el primero no se repite.
    {
      enrollmentId: 1072705,
      offeringId: 301,
      intake: 'Marzo',
      shift: 'Nocturno',
      firstSemesterSubjects: [
        { subjectId: 1, name: 'Programación I' },
        { subjectId: 3, name: 'Bases de datos' },
      ],
    },
  ],
};

describe('EnrollmentPaymentFacade', () => {
  let facade: EnrollmentPaymentFacade;
  let process: EnrollmentProcessState;
  let getBanks: ReturnType<typeof vi.fn>;
  let enrollments: { pay: ReturnType<typeof vi.fn>; getDetail: ReturnType<typeof vi.fn> };
  let externalPaymentSubmitter: { submit: ReturnType<typeof vi.fn> };

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  beforeEach(() => {
    enrollments = {
      pay: vi.fn().mockReturnValue(of(PAYMENT_OK)),
      getDetail: vi.fn().mockReturnValue(
        of({
          status: 'Confirmada',
          summary: null,
          pendingPayment: null,
          confirmed: CONFIRMED_DETAIL,
        })
      ),
    };
    externalPaymentSubmitter = { submit: vi.fn().mockReturnValue(true) };
    configureFacade();
  });

  function configureFacade(
    options: {
      queryParams?: Record<string, string | readonly string[]>;
      banks?: Observable<readonly { id: number; label: string; code: string }[]>;
    } = {}
  ): void {
    getBanks = vi
      .fn()
      .mockReturnValue(options.banks ?? of([{ id: 1, label: 'BROU', code: 'brou' }]));
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        AcademicProposalSelection,
        { provide: ENROLLMENT_FORMS, useFactory: createEnrollmentFormsState },
        { provide: ENROLLMENT_PROCESS_STATE, useFactory: createEnrollmentProcessState },
        EnrollmentProposalFacade,
        EnrollmentPaymentFacade,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(options.queryParams ?? {}) } },
        },
        {
          provide: CatalogsApi,
          useValue: {
            getDegreePrograms: () => of([]),
            getIntakes: () => of([]),
            getShifts: () => of([]),
            getBanks,
          },
        },
        { provide: EnrollmentsApi, useValue: enrollments },
        { provide: ExternalPaymentSubmitter, useValue: externalPaymentSubmitter },
      ],
    });
    facade = TestBed.inject(EnrollmentPaymentFacade);
    process = TestBed.inject(ENROLLMENT_PROCESS_STATE);
    process.preEnrollmentResponse.set({
      enrollmentId: 1072704,
      confirmed: false,
      paymentDueDate: null,
      enrollmentDeposit: 15500,
      accountBalance: 70000,
      summary: null,
    });
  }

  it('calls the payment service and shows reservation for Abitab', () => {
    facade.paymentForm.controls.paymentMethod.setValue('abitab');

    facade.confirm();

    expect(enrollments.pay).toHaveBeenCalledWith({
      enrollmentIds: [1072704],
      paymentMethod: 'abitab',
      sistarbancBankId: null,
    });
    expect(facade.outcome()).toBe('reservation');
  });

  it('shows processing while the payment request is pending', () => {
    const payment = new Subject<typeof PAYMENT_OK>();
    enrollments.pay.mockReturnValueOnce(payment.asObservable());
    facade.paymentForm.controls.paymentMethod.setValue('personal-account');

    facade.confirm();

    expect(facade.view()).toBe('processing');
    payment.next({ ...PAYMENT_OK, result: 'confirmada' });
    payment.complete();
    expect(facade.outcome()).toBe('enrollment-confirmed');
  });

  it('shows a reusable error alert when no payment method is selected', () => {
    expect(facade.paymentForm.controls.paymentMethod.value).toBe('');

    facade.confirm();

    expect(facade.view()).toBe('editing');
    expect(facade.paymentErrorAlert()).toEqual({
      title: 'Medio de pago requerido',
      message: 'Elegí un medio de pago para poder continuar.',
    });
  });

  it('requires a bank when paying from a bank account', () => {
    facade.paymentForm.controls.paymentMethod.setValue('bank-account');
    facade.paymentForm.controls.bank.setValue('');

    facade.confirm();

    expect(facade.view()).toBe('editing');
    expect(facade.paymentForm.controls.bank.hasError('required')).toBe(true);
    expect(facade.paymentErrorAlert()?.title).toBe('Banco requerido');
  });

  it('surfaces payment API errors without leaving the user in processing', () => {
    enrollments.pay.mockReturnValueOnce(
      of({
        ...PAYMENT_OK,
        success: false,
        message: 'Saldo insuficiente',
      })
    );
    facade.paymentForm.controls.paymentMethod.setValue('personal-account');

    facade.confirm();

    expect(facade.view()).toBe('editing');
    expect(facade.paymentErrorAlert()).toEqual({
      title: 'No pudimos procesar el pago',
      message: 'Saldo insuficiente',
    });
  });

  it('submits external payment methods and leaves an explicit pending state', () => {
    const externalMethods: readonly PaymentMethod[] = ['banred', 'geopay', 'bank-account'];
    for (const method of externalMethods) {
      enrollments.pay.mockClear();
      externalPaymentSubmitter.submit.mockClear();
      facade.outcome.set(null);
      facade.view.set('editing');
      facade.paymentForm.controls.paymentMethod.setValue(method);
      facade.paymentForm.controls.bank.setValue(method === 'bank-account' ? 'brou' : '');
      enrollments.pay.mockReturnValueOnce(
        of({
          ...PAYMENT_OK,
          paymentUrl: `https://pagos.example/${method}`,
          encryptedParameters: `token-${method}`,
        })
      );

      facade.confirm();

      expect(enrollments.pay).toHaveBeenCalledWith({
        enrollmentIds: [1072704],
        paymentMethod: method,
        sistarbancBankId: method === 'bank-account' ? 'brou' : null,
      });
      expect(externalPaymentSubmitter.submit).toHaveBeenCalledWith({
        paymentUrl: `https://pagos.example/${method}`,
        encryptedParameters: `token-${method}`,
      });
      expect(facade.outcome()).toBe('external-payment-pending');
    }
  });

  it('hides personal account payment when no deposit amount is available', () => {
    process.preEnrollmentResponse.set({
      confirmed: false,
      paymentDueDate: null,
      enrollmentDeposit: null,
      accountBalance: null,
      summary: null,
    });

    expect(facade.paymentOptions().some(option => option.value === 'personal-account')).toBe(false);
  });

  it('hides personal account payment when the account balance is zero', () => {
    process.preEnrollmentResponse.set({
      confirmed: false,
      paymentDueDate: null,
      enrollmentDeposit: 100000,
      accountBalance: 0,
      summary: null,
    });

    expect(facade.paymentOptions().some(option => option.value === 'personal-account')).toBe(false);
  });

  it('disables personal account payment when its balance does not cover the deposit', () => {
    process.preEnrollmentResponse.set({
      confirmed: false,
      paymentDueDate: null,
      enrollmentDeposit: 100000,
      accountBalance: 70000,
      summary: null,
    });

    expect(facade.paymentOptions().find(option => option.value === 'personal-account')).toEqual(
      expect.objectContaining({ disabled: true })
    );
  });

  it('loads bank options only after entering the payment step', () => {
    expect(getBanks).not.toHaveBeenCalled();
    expect(facade.bankOptions()).toEqual([]);

    process.flow.goTo('payment');
    TestBed.tick();

    expect(getBanks).toHaveBeenCalledOnce();
    expect(facade.bankOptions()).toEqual([
      { value: 'brou', label: 'BROU', icon: 'assets/banks/brou.svg' },
    ]);
  });

  it('loads the confirmed detail after a confirmed payment', () => {
    const forms = TestBed.inject(ENROLLMENT_FORMS);
    forms.forms.academicForm.controls.degreeProgram.setValue('20');
    forms.forms.academicForm.controls.intake.setValue('200');
    enrollments.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, result: 'confirmada' }));
    facade.paymentForm.controls.paymentMethod.setValue('personal-account');

    facade.confirm();

    expect(enrollments.getDetail).toHaveBeenCalledWith(20, 200, null);
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
    expect(facade.visibleSubjects()).toEqual(['Programación I', 'Bases de datos']);
  });

  it('uses the confirmed detail from the pay response without re-fetching', () => {
    const forms = TestBed.inject(ENROLLMENT_FORMS);
    forms.forms.academicForm.controls.degreeProgram.setValue('20');
    forms.forms.academicForm.controls.intake.setValue('200');
    enrollments.pay.mockReturnValueOnce(
      of({ ...PAYMENT_OK, result: 'confirmada', confirmed: CONFIRMED_DETAIL })
    );
    facade.paymentForm.controls.paymentMethod.setValue('personal-account');

    facade.confirm();

    expect(facade.outcome()).toBe('enrollment-confirmed');
    expect(enrollments.getDetail).not.toHaveBeenCalled();
    expect(facade.studentNumber()).toBe(412001);
  });

  it('reads estado from the resume query params when retomando pago from the panel', () => {
    configureFacade({
      queryParams: { idProducto: '20', idProceso: '200', estado: 'Pago pendiente' },
    });
    enrollments.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, result: 'confirmada' }));
    facade.paymentForm.controls.paymentMethod.setValue('personal-account');

    facade.confirm();

    expect(enrollments.getDetail).toHaveBeenCalledWith(20, 200, 'Pago pendiente');
  });

  it('loads the reservation data after an Abitab reserva from the fresh flow', () => {
    const forms = TestBed.inject(ENROLLMENT_FORMS);
    forms.forms.academicForm.controls.degreeProgram.setValue('20');
    forms.forms.academicForm.controls.intake.setValue('200');
    enrollments.getDetail.mockReturnValueOnce(
      of({
        status: 'Pago pendiente',
        summary: null,
        pendingPayment: null,
        minimumDeposit: {
          paymentMethod: 'ABITAB',
          documentNumber: '12345678',
          personCode: 34692671,
          deposit: 15500,
        },
        confirmed: null,
      })
    );
    facade.paymentForm.controls.paymentMethod.setValue('abitab');

    facade.confirm();

    expect(facade.outcome()).toBe('reservation');
    expect(enrollments.getDetail).toHaveBeenCalledWith(20, 200, null);
    expect(facade.reservationInstructions().items).toContainEqual({
      label: 'Cédula de identidad',
      value: '12345678',
    });
    expect(facade.reservationInstructions().items).toContainEqual({
      label: 'Número de estudiante',
      value: '34692671',
    });
  });

  it('degrades to the amount-only reservation when getDetail fails on a reserva', () => {
    const forms = TestBed.inject(ENROLLMENT_FORMS);
    forms.forms.academicForm.controls.degreeProgram.setValue('20');
    forms.forms.academicForm.controls.intake.setValue('200');
    enrollments.getDetail.mockReturnValueOnce(throwError(() => new Error('network error')));
    facade.paymentForm.controls.paymentMethod.setValue('abitab');

    facade.confirm();

    expect(facade.outcome()).toBe('reservation');
    expect(facade.reservationData()).toBeNull();
    expect(facade.reservationInstructions().items).toEqual([
      { label: 'Monto a pagar', value: '$ 15.500' },
    ]);
  });

  it('keeps the success screen without detail sections when getDetail fails', () => {
    const forms = TestBed.inject(ENROLLMENT_FORMS);
    forms.forms.academicForm.controls.degreeProgram.setValue('20');
    forms.forms.academicForm.controls.intake.setValue('200');
    enrollments.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, result: 'confirmada' }));
    enrollments.getDetail.mockReturnValueOnce(throwError(() => new Error('network error')));
    facade.paymentForm.controls.paymentMethod.setValue('personal-account');

    facade.confirm();

    expect(facade.outcome()).toBe('enrollment-confirmed');
    expect(facade.studentNumber()).toBeNull();
    expect(facade.coordinators()).toEqual([]);
    expect(facade.visibleSubjects()).toEqual([]);
  });

  it('skips the detail request when no catalog ids are available', () => {
    enrollments.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, result: 'confirmada' }));
    facade.paymentForm.controls.paymentMethod.setValue('personal-account');

    facade.confirm();

    expect(facade.outcome()).toBe('enrollment-confirmed');
    expect(enrollments.getDetail).not.toHaveBeenCalled();
  });

  it('builds reservation instructions with the real deadline and amount', () => {
    process.preEnrollmentResponse.set({
      enrollmentId: 1072704,
      confirmed: false,
      paymentDueDate: '2027-03-04',
      enrollmentDeposit: 15500,
      accountBalance: null,
      summary: null,
    });
    facade.selectedPaymentMethod.set('abitab');

    const instructions = facade.reservationInstructions();

    expect(instructions.description).toContain('04/03/2027');
    expect(instructions.items).toContainEqual({ label: 'Monto a pagar', value: '$ 15.500' });
    expect(JSON.stringify(instructions.items)).not.toContain('397654');
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
    enrollments.pay.mockReturnValueOnce(payment.asObservable());
    facade.paymentForm.controls.paymentMethod.setValue('abitab');

    facade.confirm();
    facade.confirm();

    expect(enrollments.pay).toHaveBeenCalledTimes(1);
    payment.next(PAYMENT_OK);
    payment.complete();
    expect(facade.outcome()).toBe('reservation');
  });

  it('rejects external payment when the gateway returns no URL', () => {
    enrollments.pay.mockReturnValueOnce(
      of({ ...PAYMENT_OK, paymentUrl: null, encryptedParameters: 'token-encriptado' })
    );
    facade.paymentForm.controls.paymentMethod.setValue('banred');

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
    enrollments.pay.mockReturnValueOnce(
      of({ ...PAYMENT_OK, paymentUrl: 'https://pagos.example/geopay', encryptedParameters: null })
    );
    facade.paymentForm.controls.paymentMethod.setValue('geopay');

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
    enrollments.pay.mockReturnValueOnce(
      of({
        ...PAYMENT_OK,
        paymentUrl: 'https://pagos.example/banred',
        encryptedParameters: 'token-encriptado',
      })
    );
    facade.paymentForm.controls.paymentMethod.setValue('banred');

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
    enrollments.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, result: 'confirmada' }));
    facade.paymentForm.controls.paymentMethod.setValue('abitab');

    facade.confirm();

    expect(facade.outcome()).toBe('enrollment-in-progress');
  });

  it('recovers to editing with a visible error when the payment request fails', () => {
    enrollments.pay.mockReturnValueOnce(throwError(() => new Error('network down')));
    facade.paymentForm.controls.paymentMethod.setValue('abitab');

    facade.confirm();

    expect(facade.view()).toBe('editing');
    expect(facade.outcome()).toBeNull();
    expect(facade.paymentErrorAlert()).toEqual({
      title: 'No pudimos procesar el pago',
      message: 'Intentá nuevamente en unos minutos.',
    });
  });

  it('falls back to the static bank options when the catalog fails', () => {
    configureFacade({ banks: throwError(() => new Error('catalog down')) });
    process.flow.goTo('payment');
    TestBed.tick();

    expect(facade.bankOptions()).toEqual(FALLBACK_BANK_OPTIONS);
    expect(facade.loadingBanks()).toBe(false);
  });

  it('reserves on a reservada result for methods without their own branch', () => {
    enrollments.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, result: 'reservada' }));
    facade.paymentForm.controls.paymentMethod.setValue('personal-account');

    facade.confirm();

    expect(facade.outcome()).toBe('reservation');
  });

  it('confirms the enrollment when the backend result is unknown', () => {
    enrollments.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, result: 'algo-desconocido' }));
    facade.paymentForm.controls.paymentMethod.setValue('personal-account');

    facade.confirm();

    expect(facade.outcome()).toBe('enrollment-confirmed');
  });

  it('shows the 3 flat rows and no seminarios for a regular (non-AP) enrollment', () => {
    expect(facade.isProfessionalUpdate()).toBe(false);
    expect(facade.summaryItems().map(item => item.label)).toEqual(['Carrera', 'Comienzo', 'Turno']);
    expect(facade.seminarsSummary()).toEqual([]);
  });

  it('collapses the summary to Programa and lists seminarios for Actualización profesional', () => {
    const selection = TestBed.inject(AcademicProposalSelection);
    selection.setProposalType('3');
    process.preEnrollmentResponse.set({
      enrollmentId: 1072704,
      confirmed: false,
      paymentDueDate: null,
      enrollmentDeposit: 15500,
      accountBalance: 70000,
      summary: { degreeProgram: 'Actualización en IA', intake: null, shift: null },
      seminars: [
        {
          enrollmentId: 1,
          offeringId: 10,
          name: 'Seminario A',
          intake: 'Marzo',
          shift: 'Noche',
        },
        {
          enrollmentId: 2,
          offeringId: 11,
          name: 'Seminario B',
          intake: 'Abril',
          shift: 'Mañana',
        },
      ],
    });

    expect(facade.isProfessionalUpdate()).toBe(true);
    expect(facade.summaryItems()).toEqual([
      { icon: 'school', label: 'Programa', value: 'Actualización en IA' },
    ]);
    expect(facade.seminarsSummary()).toEqual([
      { enrollmentId: 1, name: 'Seminario A', intake: 'Marzo', shift: 'Noche' },
      { enrollmentId: 2, name: 'Seminario B', intake: 'Abril', shift: 'Mañana' },
    ]);
  });

  it('shows Programa and Comienzo without the seminarios block for a single seminario', () => {
    const selection = TestBed.inject(AcademicProposalSelection);
    selection.setProposalType('3');
    process.preEnrollmentResponse.set({
      enrollmentId: 1072704,
      confirmed: false,
      paymentDueDate: null,
      enrollmentDeposit: 15500,
      accountBalance: 70000,
      summary: { degreeProgram: 'Actualización en IA', intake: null, shift: null },
      seminars: [
        {
          enrollmentId: 1,
          offeringId: 10,
          name: 'Seminario A',
          intake: 'Marzo',
          shift: 'Noche',
        },
      ],
    });

    expect(facade.summaryItems()).toEqual([
      { icon: 'school', label: 'Programa', value: 'Actualización en IA' },
      { icon: 'calendar_today', label: 'Comienzo', value: 'Marzo' },
    ]);
    expect(facade.seminarsSummary()).toEqual([]);
  });

  it('uses the selected seminar catalog when confirmation omits the offer detail', () => {
    const selection = TestBed.inject(AcademicProposalSelection);
    const proposal = TestBed.inject(EnrollmentProposalFacade);
    selection.setProposalType('3');
    vi.spyOn(selection, 'seminars').mockReturnValue([
      {
        offeringId: 10,
        admissionProcessId: 210,
        name: 'Seminario A',
        startDate: '2027-03-04T00:00:00',
      },
      { offeringId: 11, admissionProcessId: 210, name: 'Seminario B', startDate: null },
    ]);
    proposal.academicForm.controls.seminars.setValue(['10', '11']);
    process.preEnrollmentResponse.update(response => ({ ...response!, seminars: [] }));

    expect(facade.seminarsSummary()).toEqual([
      {
        enrollmentId: null,
        name: 'Seminario A',
        intake: '04/03/2027',
        shift: 'No informado',
      },
      {
        enrollmentId: null,
        name: 'Seminario B',
        intake: 'No informado',
        shift: 'No informado',
      },
    ]);
  });

  it('collapses the catalog fallback to a Comienzo row when a single offer is selected', () => {
    const selection = TestBed.inject(AcademicProposalSelection);
    const proposal = TestBed.inject(EnrollmentProposalFacade);
    selection.setProposalType('3');
    vi.spyOn(selection, 'seminars').mockReturnValue([
      {
        offeringId: 10,
        admissionProcessId: 210,
        name: 'Seminario A',
        startDate: '2027-03-04T00:00:00',
      },
    ]);
    proposal.academicForm.controls.seminars.setValue(['10']);
    process.preEnrollmentResponse.update(response => ({ ...response!, seminars: [] }));

    expect(facade.summaryItems().at(-1)).toEqual({
      icon: 'calendar_today',
      label: 'Comienzo',
      value: '04/03/2027',
    });
    expect(facade.seminarsSummary()).toEqual([]);
  });

  it('uses enrollment ids from the resume session when confirmation omits them', () => {
    configureFacade({
      queryParams: { idProducto: '40', idProceso: '210' },
    });
    TestBed.inject(EnrollmentResumeContextStore).save({
      productId: 40,
      admissionProcessId: 210,
      offeringIds: [310, 311],
      enrollmentIds: [7010, 7011],
    });
    process.preEnrollmentResponse.update(response => ({
      ...response!,
      enrollmentId: null,
      seminars: [],
    }));
    facade.paymentForm.controls.paymentMethod.setValue('personal-account');

    facade.confirm();

    expect(enrollments.pay).toHaveBeenCalledWith({
      enrollmentIds: [7010, 7011],
      paymentMethod: 'personal-account',
      sistarbancBankId: null,
    });
  });

  it('charges every seminario of an Actualización profesional package', () => {
    process.preEnrollmentResponse.set({
      enrollmentId: 1072704,
      confirmed: false,
      paymentDueDate: null,
      enrollmentDeposit: 15500,
      accountBalance: 70000,
      summary: null,
      seminars: [
        {
          enrollmentId: 1072704,
          offeringId: 10,
          name: 'Seminario A',
          intake: null,
          shift: null,
        },
        {
          enrollmentId: 1072705,
          offeringId: 11,
          name: 'Seminario B',
          intake: null,
          shift: null,
        },
      ],
    });
    facade.paymentForm.controls.paymentMethod.setValue('abitab');

    facade.confirm();

    expect(enrollments.pay).toHaveBeenCalledWith({
      enrollmentIds: [1072704, 1072705],
      paymentMethod: 'abitab',
      sistarbancBankId: null,
    });
  });

  it('rejects the payment when the pending enrollment id is missing', () => {
    process.preEnrollmentResponse.set({
      enrollmentId: null,
      confirmed: false,
      paymentDueDate: null,
      enrollmentDeposit: 15500,
      accountBalance: 70000,
      summary: null,
    });
    facade.paymentForm.controls.paymentMethod.setValue('abitab');

    facade.confirm();

    expect(enrollments.pay).not.toHaveBeenCalled();
    expect(facade.view()).toBe('editing');
    expect(facade.paymentErrorAlert()).toEqual({
      title: 'No pudimos procesar el pago',
      message: 'No pudimos identificar la inscripción pendiente.',
    });
  });
});
