import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { Catalogs } from '../../../catalogs/services/catalogs';
import { createPersonalForm } from '../../forms/auth-forms';
import { RegisterPersonalStep } from './register-personal-step';

describe('RegisterPersonalStep', () => {
  let fixture: ComponentFixture<RegisterPersonalStep>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RegisterPersonalStep],
      providers: [
        {
          provide: Catalogs,
          useValue: {
            getCountryLocations: vi.fn().mockReturnValue(of([])),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(RegisterPersonalStep);
    fixture.componentRef.setInput('form', createPersonalForm());
    fixture.detectChanges();
  });

  it('should render complete personal fields by default', () => {
    expect(fixture.nativeElement.textContent).toContain('Primer nombre');
  });

  it('should render verification fields in verification mode', () => {
    fixture.componentRef.setInput('personalMode', 'verification');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Confirmá tus datos');
    expect(text).not.toContain('Primer nombre');
  });

  it('should keep the error summary stable while the user edits', () => {
    const component = fixture.componentInstance;
    const form = component.form();

    component.submitted.set(true);
    component.refreshErrorSummary();

    const previousSummary = component.errorSummary();

    form.controls.mail.setValue('postulante@ort.edu.uy');

    expect(component.errorSummary()).toBe(previousSummary);
  });
});
