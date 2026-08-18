import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { afterEach, vi } from 'vitest';

import { ExternalPaymentSubmitter } from './external-payment-submitter';

describe('ExternalPaymentSubmitter', () => {
  let service: ExternalPaymentSubmitter;

  beforeEach(() => {
    document.body.replaceChildren();
    TestBed.configureTestingModule({
      providers: [ExternalPaymentSubmitter, { provide: DOCUMENT, useValue: document }],
    });
    service = TestBed.inject(ExternalPaymentSubmitter);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    document.body.replaceChildren();
  });

  it('submits encrypted gateway params through a temporary POST form', () => {
    const submitted = { form: null as HTMLFormElement | null };
    vi.spyOn(HTMLFormElement.prototype, 'submit').mockImplementation(function (
      this: HTMLFormElement
    ) {
      submitted.form = this;
    });

    expect(
      service.submit({
        paymentUrl: 'https://pagos.example/sistarbanc',
        encryptedParameters: 'token-encriptado',
      })
    ).toBe(true);

    const form = submitted.form;
    if (!form) throw new Error('Expected payment form submission');
    const input = form.querySelector<HTMLInputElement>('input[name="data"]');
    expect(form.method).toBe('post');
    expect(form.action).toBe('https://pagos.example/sistarbanc');
    expect(form.target).toBe('_blank');
    expect(input?.type).toBe('hidden');
    expect(input?.value).toBe(JSON.stringify({ params: 'parametrosEncriptados=token-encriptado' }));
    expect(document.body.querySelector('form')).toBeNull();
  });

  it('rejects invalid or unsafe gateway URLs without adding a form', () => {
    const submit = vi.spyOn(HTMLFormElement.prototype, 'submit');

    for (const paymentUrl of [
      'javascript:alert(1)',
      'data:text/html,test',
      'ftp://pagos.example',
      'no-es-una-url',
    ]) {
      expect(service.submit({ paymentUrl, encryptedParameters: 'token-encriptado' })).toBe(false);
    }

    expect(submit).not.toHaveBeenCalled();
    expect(document.body.querySelector('form')).toBeNull();
  });
});
