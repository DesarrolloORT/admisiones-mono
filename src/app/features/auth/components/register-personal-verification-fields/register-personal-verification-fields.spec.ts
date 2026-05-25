import { ComponentFixture, TestBed } from '@angular/core/testing';

import { createPersonalForm } from '../../forms/auth-forms';
import { RegisterPersonalVerificationFields } from './register-personal-verification-fields';

describe('RegisterPersonalVerificationFields', () => {
  let fixture: ComponentFixture<RegisterPersonalVerificationFields>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RegisterPersonalVerificationFields],
    });

    fixture = TestBed.createComponent(RegisterPersonalVerificationFields);
    fixture.componentRef.setInput('form', createPersonalForm());
    fixture.detectChanges();
  });

  it('should render only identity verification fields', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Primer apellido');
    expect(text).toContain('E-mail');
    expect(text).not.toContain('Primer nombre');
    expect(text).not.toContain('Confirmar e-mail');
  });
});
