import { TestBed } from '@angular/core/testing';

import { createLoginForm } from '../../../forms/auth-forms';
import { DocumentFields } from './document-fields';

describe('DocumentFields', () => {
  function createComponent(form = createLoginForm()) {
    const fixture = TestBed.createComponent(DocumentFields);
    fixture.componentRef.setInput('form', form);
    fixture.componentRef.setInput('idPrefix', 'login');
    fixture.componentRef.setInput('autocomplete', 'username');
    fixture.detectChanges();
    return { fixture, form };
  }

  it('renders both fields with prefixed ids and the autocomplete hint', () => {
    const { fixture } = createComponent();
    const host: HTMLElement = fixture.nativeElement;

    expect(host.querySelector('#login-document-type')).toBeTruthy();
    const numberInput = host.querySelector<HTMLInputElement>('#login-document-number');
    expect(numberInput?.getAttribute('autocomplete')).toBe('username');
  });

  it('switches document number validators when the type changes', () => {
    const { fixture, form } = createComponent();

    form.controls.documentNumber.setValue('ABC-123');
    expect(form.controls.documentNumber.hasError('nationalId')).toBe(true);

    form.controls.documentType.setValue('PS');
    fixture.detectChanges();

    expect(form.controls.documentNumber.hasError('nationalId')).toBe(false);
    expect(form.controls.documentNumber.valid).toBe(true);

    form.controls.documentNumber.setValue('con espacios');
    expect(form.controls.documentNumber.hasError('pattern')).toBe(true);
  });

  it('shows the format hint for every document type', () => {
    const { fixture, form } = createComponent();
    const host: HTMLElement = fixture.nativeElement;

    expect(host.querySelector('ort-hint')?.textContent).toContain('Sin puntos ni guiones');

    form.controls.documentType.setValue('DE');
    fixture.detectChanges();

    expect(host.querySelector('ort-hint')?.textContent).toContain('Sin puntos ni guiones');
  });
});
