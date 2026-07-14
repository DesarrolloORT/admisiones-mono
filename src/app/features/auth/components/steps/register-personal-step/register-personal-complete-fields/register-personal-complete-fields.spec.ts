import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { Catalogs } from '../../../../../catalogs/services/catalogs';
import { createPersonalForm } from '../../../../forms/auth-forms';
import { RegisterPersonalCompleteFields } from './register-personal-complete-fields';

describe('RegisterPersonalCompleteFields', () => {
  let fixture: ComponentFixture<RegisterPersonalCompleteFields>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RegisterPersonalCompleteFields],
      providers: [
        {
          provide: Catalogs,
          useValue: {
            getCountryLocations: vi.fn().mockReturnValue(of([])),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(RegisterPersonalCompleteFields);
    fixture.componentRef.setInput('form', createPersonalForm());
    fixture.detectChanges();
  });

  it('should render full personal data fields', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Primer nombre');
    expect(text).toContain('Datos de contacto');
    expect(text).toContain('Confirmar e-mail');
  });
});
