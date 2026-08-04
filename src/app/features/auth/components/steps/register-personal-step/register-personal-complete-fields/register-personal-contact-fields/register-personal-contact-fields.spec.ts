import { ComponentFixture, TestBed } from '@angular/core/testing';

import { createPersonalForm } from '../../../../../forms/auth-forms';
import { RegisterPersonalContactFields } from './register-personal-contact-fields';

describe('RegisterPersonalContactFields', () => {
  let fixture: ComponentFixture<RegisterPersonalContactFields>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RegisterPersonalContactFields],
    });

    fixture = TestBed.createComponent(RegisterPersonalContactFields);
    fixture.componentRef.setInput('form', createPersonalForm());
    fixture.detectChanges();
  });

  it('should render contact fields', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Datos de contacto');
    expect(text).toContain('Celular');
    expect(text).toContain('Confirmar e-mail');
    expect(fixture.nativeElement.querySelector('ort-phone-input')).not.toBeNull();
  });
});
