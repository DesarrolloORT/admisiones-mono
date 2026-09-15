import { ComponentFixture, TestBed } from '@angular/core/testing';

import { createPersonalForm } from '../../../../../forms/auth-forms';
import { RegisterPersonalContactFields } from './register-personal-contact-fields';

describe('RegisterPersonalContactFields', () => {
  let fixture: ComponentFixture<RegisterPersonalContactFields>;
  let form: ReturnType<typeof createPersonalForm>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RegisterPersonalContactFields],
    });

    fixture = TestBed.createComponent(RegisterPersonalContactFields);
    form = createPersonalForm();
    fixture.componentRef.setInput('form', form);
    fixture.detectChanges();
  });

  it('should render contact fields', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Datos de contacto');
    expect(text).toContain('Celular');
    expect(text).toContain('Confirmar e-mail');
    expect(fixture.nativeElement.querySelector('ort-phone-input')).not.toBeNull();
  });

  it('should prioritize invalid over required when both phone errors are present', () => {
    form.controls.primaryPhone.setErrors({ required: true, phone: true });
    form.controls.primaryPhone.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('ort-error')?.textContent as string;

    expect(error).toContain('Ingresá un celular válido.');
    expect(error).not.toContain('obligatorio');
  });
});
