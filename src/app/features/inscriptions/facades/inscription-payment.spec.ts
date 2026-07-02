import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { afterEach, vi } from 'vitest';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import { Inscripciones } from '../services/inscriptions';
import { InscripcionFormsStore } from '../store/inscription-forms';
import { InscripcionProcessStore } from '../store/inscription-process';
import { InscripcionPaymentFacade } from './inscription-payment';
import { InscripcionProposalFacade } from './inscription-proposal';

describe('InscripcionPaymentFacade', () => {
  let facade: InscripcionPaymentFacade;
  let process: InscripcionProcessStore;

  afterEach(() => {
    vi.useRealTimers();
  });

  beforeEach(() => {
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
        { provide: Inscripciones, useValue: {} },
      ],
    });
    facade = TestBed.inject(InscripcionPaymentFacade);
    process = TestBed.inject(InscripcionProcessStore);
  });

  it('uses a confirmation subview without changing the process step', () => {
    facade.paymentForm.controls.metodoPago.setValue('paganza');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.outcome()).toBe('reserva');
  });

  it('shows processing before completing an immediate payment', () => {
    vi.useFakeTimers();
    facade.paymentForm.controls.metodoPago.setValue('cuenta-bancaria');
    facade.paymentForm.controls.banco.setValue('1');

    facade.requestConfirmation();
    facade.confirm();

    expect(facade.view()).toBe('processing');
    vi.advanceTimersByTime(1000);
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

  it('loads bank options with their logos', () => {
    expect(facade.bankOptions()).toEqual([
      { value: '1', label: 'BROU', icon: 'assets/banks/brou.svg' },
    ]);
  });
});
