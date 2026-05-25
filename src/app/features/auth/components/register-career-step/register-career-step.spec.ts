import { ComponentFixture, TestBed } from '@angular/core/testing';

import { createCareerForm } from '../../forms/auth-forms';
import { RegisterCareerStep } from './register-career-step';

describe('RegisterCareerStep', () => {
  let fixture: ComponentFixture<RegisterCareerStep>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RegisterCareerStep],
    });

    fixture = TestBed.createComponent(RegisterCareerStep);
    fixture.componentRef.setInput('form', createCareerForm());
    fixture.componentRef.setInput('academicLevels', [{ id: 1, nombre: 'Carreras' }]);
    fixture.componentRef.setInput('filteredCareers', [
      {
        idProducto: 20,
        idNivelProducto: 1,
        nombreProducto: 'Diseño',
        nombreNivelProducto: 'Carreras',
      },
    ]);
    fixture.componentRef.setInput('comienzos', [{ idProceso: 30, nombreProceso: 'Marzo' }]);
    fixture.detectChanges();
  });

  it('should render career selection controls', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Propuesta académica');
    expect(text).toContain('Carrera');
    expect(text).toContain('Comienzo');
  });
});
