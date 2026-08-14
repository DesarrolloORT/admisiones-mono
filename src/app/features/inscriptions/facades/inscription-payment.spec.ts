import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, vi } from 'vitest';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import { FALLBACK_BANK_OPTIONS } from '../models/inscription-bank-logo';
import type { InscripcionPaymentResponse, MetodoPago } from '../models/inscription-flow';
import { ExternalPaymentSubmitter } from '../services/external-payment-submitter';
import { InscriptionResumeContextStore } from '../services/inscription-resume-context';
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
  confirmada: null,
  message: null,
  errorCode: null,
};

const CONFIRMED_DETAIL = {
  numeroEstudiante: 412001,
  resumen: null,
  coordinadorAcademico: { nombre: 'Laura Pérez', email: 'laura.perez@ort.edu.uy' },
  coordinadorCursos: { nombre: 'Diego Cursos', email: 'diego.cursos@ort.edu.uy' },
  inscripciones: [
    {
      idInscripcion: 1072704,
      idOferta: 300,
      comienzo: 'Marzo',
      turno: 'Matutino',
      materiasPrimerSemestre: [
        { idMateria: 1, nombre: 'Programación I' },
        { idMateria: 2, nombre: null },
      ],
    },
    // Segundo seminario confirmado (Actualización profesional): sus materias también
    // se listan y la que comparte con el primero no se repite.
    {
      idInscripcion: 1072705,
      idOferta: 301,
      comienzo: 'Marzo',
      turno: 'Nocturno',
      materiasPrimerSemestre: [
        { idMateria: 1, nombre: 'Programación I' },
        { idMateria: 3, nombre: 'Bases de datos' },
      ],
    },
  ],
};

describe('InscripcionPaymentFacade', () => {
  let facade: InscripcionPaymentFacade;
  let process: InscripcionProcessStore;
  let getBanks: ReturnType<typeof vi.fn>;
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
      queryParams?: Record<string, string | readonly string[]>;
      bancos?: Observable<readonly { id: number; label: string; code: string }[]>;
    } = {}
  ): void {
    getBanks = vi
      .fn()
      .mockReturnValue(options.bancos ?? of([{ id: 1, label: 'BROU', code: 'brou' }]));
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
            getDegreePrograms: () => of([]),
            getIntakes: () => of([]),
            getShifts: () => of([]),
            getBanks,
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
      idsInscripcion: [1072704],
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
        idsInscripcion: [1072704],
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

  it('loads bank options only after entering the payment step', () => {
    expect(getBanks).not.toHaveBeenCalled();
    expect(facade.bankOptions()).toEqual([]);

    process.flow.goTo('pago');
    TestBed.tick();

    expect(getBanks).toHaveBeenCalledOnce();
    expect(facade.bankOptions()).toEqual([
      { value: 'brou', label: 'BROU', icon: 'assets/banks/brou.svg' },
    ]);
  });

  it('loads the confirmed detail after a confirmed payment', () => {
    const forms = TestBed.inject(InscripcionFormsStore);
    forms.academicForm.controls.degreeProgram.setValue('20');
    forms.academicForm.controls.intake.setValue('200');
    inscriptions.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, resultado: 'confirmada' }));
    facade.paymentForm.controls.metodoPago.setValue('cuenta-personal');

    facade.requestConfirmation();
    facade.confirm();

    expect(inscriptions.getDetail).toHaveBeenCalledWith(20, 200, null);
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
    const forms = TestBed.inject(InscripcionFormsStore);
    forms.academicForm.controls.degreeProgram.setValue('20');
    forms.academicForm.controls.intake.setValue('200');
    inscriptions.pay.mockReturnValueOnce(
      of({ ...PAYMENT_OK, resultado: 'confirmada', confirmada: CONFIRMED_DETAIL })
    );
    facade.paymentForm.controls.metodoPago.setValue('cuenta-personal');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.outcome()).toBe('inscription-confirmada');
    expect(inscriptions.getDetail).not.toHaveBeenCalled();
    expect(facade.studentNumber()).toBe(412001);
  });

  it('reads estado from the resume query params when retomando pago from the panel', () => {
    configureFacade({
      queryParams: { idProducto: '20', idProceso: '200', estado: 'Pago pendiente' },
    });
    inscriptions.pay.mockReturnValueOnce(of({ ...PAYMENT_OK, resultado: 'confirmada' }));
    facade.paymentForm.controls.metodoPago.setValue('cuenta-personal');

    facade.requestConfirmation();
    facade.confirm();

    expect(inscriptions.getDetail).toHaveBeenCalledWith(20, 200, 'Pago pendiente');
  });

  it('loads the reservation data after an Abitab reserva from the fresh flow', () => {
    const forms = TestBed.inject(InscripcionFormsStore);
    forms.academicForm.controls.degreeProgram.setValue('20');
    forms.academicForm.controls.intake.setValue('200');
    inscriptions.getDetail.mockReturnValueOnce(
      of({
        estado: 'Pago pendiente',
        detalle: null,
        pagoPendiente: null,
        seniaMinima: {
          metodoPago: 'ABITAB',
          cedula: '12345678',
          codigoPersona: 34692671,
          senia: 15500,
        },
        confirmada: null,
      })
    );
    facade.paymentForm.controls.metodoPago.setValue('abitab');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.outcome()).toBe('reserva');
    expect(inscriptions.getDetail).toHaveBeenCalledWith(20, 200, null);
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
    const forms = TestBed.inject(InscripcionFormsStore);
    forms.academicForm.controls.degreeProgram.setValue('20');
    forms.academicForm.controls.intake.setValue('200');
    inscriptions.getDetail.mockReturnValueOnce(throwError(() => new Error('network error')));
    facade.paymentForm.controls.metodoPago.setValue('abitab');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.outcome()).toBe('reserva');
    expect(facade.reservationData()).toBeNull();
    expect(facade.reservationInstructions().items).toEqual([
      { label: 'Monto a pagar', value: '$ 15.500' },
    ]);
  });

  it('keeps the success screen without detail sections when getDetail fails', () => {
    const forms = TestBed.inject(InscripcionFormsStore);
    forms.academicForm.controls.degreeProgram.setValue('20');
    forms.academicForm.controls.intake.setValue('200');
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
    process.flow.goTo('pago');
    TestBed.tick();

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

  it('shows the 3 flat rows and no seminarios for a regular (non-AP) inscription', () => {
    expect(facade.isProfessionalUpdate()).toBe(false);
    expect(facade.summaryItems().map(item => item.label)).toEqual(['Carrera', 'Comienzo', 'Turno']);
    expect(facade.seminariosResumen()).toEqual([]);
  });

  it('collapses the summary to Programa and lists seminarios for Actualización profesional', () => {
    const selection = TestBed.inject(AcademicProposalSelection);
    selection.setProposalType('3');
    process.preEnrollmentResponse.set({
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: null,
      seniaInscripcion: 15500,
      saldoCuenta: 70000,
      resumen: { carrera: 'Actualización en IA', comienzo: null, turno: null },
      seminarios: [
        {
          idInscripcion: 1,
          idOferta: 10,
          nombre: 'Seminario A',
          comienzo: 'Marzo',
          turno: 'Noche',
        },
        {
          idInscripcion: 2,
          idOferta: 11,
          nombre: 'Seminario B',
          comienzo: 'Abril',
          turno: 'Mañana',
        },
      ],
    });

    expect(facade.isProfessionalUpdate()).toBe(true);
    expect(facade.summaryItems()).toEqual([
      { icon: 'school', label: 'Programa', value: 'Actualización en IA' },
    ]);
    expect(facade.seminariosResumen()).toEqual([
      { idInscripcion: 1, nombre: 'Seminario A', comienzo: 'Marzo', turno: 'Noche' },
      { idInscripcion: 2, nombre: 'Seminario B', comienzo: 'Abril', turno: 'Mañana' },
    ]);
  });

  it('shows Programa and Comienzo without the seminarios block for a single seminario', () => {
    const selection = TestBed.inject(AcademicProposalSelection);
    selection.setProposalType('3');
    process.preEnrollmentResponse.set({
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: null,
      seniaInscripcion: 15500,
      saldoCuenta: 70000,
      resumen: { carrera: 'Actualización en IA', comienzo: null, turno: null },
      seminarios: [
        {
          idInscripcion: 1,
          idOferta: 10,
          nombre: 'Seminario A',
          comienzo: 'Marzo',
          turno: 'Noche',
        },
      ],
    });

    expect(facade.summaryItems()).toEqual([
      { icon: 'school', label: 'Programa', value: 'Actualización en IA' },
      { icon: 'calendar_today', label: 'Comienzo', value: 'Marzo' },
    ]);
    expect(facade.seminariosResumen()).toEqual([]);
  });

  it('uses the selected seminar catalog when confirmation omits the offer detail', () => {
    const selection = TestBed.inject(AcademicProposalSelection);
    const proposal = TestBed.inject(InscripcionProposalFacade);
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
    process.preEnrollmentResponse.update(response => ({ ...response!, seminarios: [] }));

    expect(facade.seminariosResumen()).toEqual([
      {
        idInscripcion: null,
        nombre: 'Seminario A',
        comienzo: '04/03/2027',
        turno: 'No informado',
      },
      {
        idInscripcion: null,
        nombre: 'Seminario B',
        comienzo: 'No informado',
        turno: 'No informado',
      },
    ]);
  });

  it('collapses the catalog fallback to a Comienzo row when a single offer is selected', () => {
    const selection = TestBed.inject(AcademicProposalSelection);
    const proposal = TestBed.inject(InscripcionProposalFacade);
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
    process.preEnrollmentResponse.update(response => ({ ...response!, seminarios: [] }));

    expect(facade.summaryItems().at(-1)).toEqual({
      icon: 'calendar_today',
      label: 'Comienzo',
      value: '04/03/2027',
    });
    expect(facade.seminariosResumen()).toEqual([]);
  });

  it('uses enrollment ids from the resume session when confirmation omits them', () => {
    configureFacade({
      queryParams: { idProducto: '40', idProceso: '210' },
    });
    TestBed.inject(InscriptionResumeContextStore).save({
      idProducto: 40,
      idProceso: 210,
      idOfertas: [310, 311],
      idInscripciones: [7010, 7011],
    });
    process.preEnrollmentResponse.update(response => ({
      ...response!,
      idInscripcion: null,
      seminarios: [],
    }));
    facade.paymentForm.controls.metodoPago.setValue('cuenta-personal');

    facade.requestConfirmation();
    facade.confirm();

    expect(inscriptions.pay).toHaveBeenCalledWith({
      idsInscripcion: [7010, 7011],
      metodoPago: 'cuenta-personal',
      idBancoSistarbanc: null,
    });
  });

  it('charges every seminario of an Actualización profesional package', () => {
    process.preEnrollmentResponse.set({
      idInscripcion: 1072704,
      confirmada: false,
      fechaVencimientoPago: null,
      seniaInscripcion: 15500,
      saldoCuenta: 70000,
      resumen: null,
      seminarios: [
        {
          idInscripcion: 1072704,
          idOferta: 10,
          nombre: 'Seminario A',
          comienzo: null,
          turno: null,
        },
        {
          idInscripcion: 1072705,
          idOferta: 11,
          nombre: 'Seminario B',
          comienzo: null,
          turno: null,
        },
      ],
    });
    facade.paymentForm.controls.metodoPago.setValue('abitab');

    facade.requestConfirmation();
    facade.confirm();

    expect(inscriptions.pay).toHaveBeenCalledWith({
      idsInscripcion: [1072704, 1072705],
      metodoPago: 'abitab',
      idBancoSistarbanc: null,
    });
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
