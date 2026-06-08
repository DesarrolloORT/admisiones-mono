import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { InscripcionShell } from './inscripcion-shell';

describe('InscripcionShell', () => {
  let fixture: ComponentFixture<InscripcionShell>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [InscripcionShell],
      providers: [provideRouter([])],
    });

    fixture = TestBed.createComponent(InscripcionShell);
  });

  it('should render the admissions brand and step label', () => {
    fixture.componentRef.setInput('stepLabel', 'Paso 2 de 3 - Información personal');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Admisiones');
    expect(text).toContain('Inscripción a carrera');
    expect(text).toContain('Paso 2 de 3 - Información personal');
  });
});
