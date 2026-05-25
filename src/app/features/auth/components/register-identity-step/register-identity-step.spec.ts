import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { createIdentityForm } from '../../forms/auth-forms';
import { RegisterIdentityStep } from './register-identity-step';

describe('RegisterIdentityStep', () => {
  let fixture: ComponentFixture<RegisterIdentityStep>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RegisterIdentityStep],
      providers: [provideRouter([])],
    });

    fixture = TestBed.createComponent(RegisterIdentityStep);
    fixture.componentRef.setInput('form', createIdentityForm());
    fixture.componentRef.setInput('documentTypes', of([]));
    fixture.detectChanges();
  });

  it('should render document identity controls', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Tipo de documento');
    expect(text).toContain('Nro. de documento');
    expect(text).toContain('Continuar');
  });
});
