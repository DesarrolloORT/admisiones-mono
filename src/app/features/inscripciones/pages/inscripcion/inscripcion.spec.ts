import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { Inscripcion } from './inscripcion';

describe('Inscripcion', () => {
  let fixture: ComponentFixture<Inscripcion>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Inscripcion],
      providers: [provideRouter([])],
    });

    fixture = TestBed.createComponent(Inscripcion);
  });

  it('should render the first mobile step', () => {
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Paso 1 de 3 - Propuesta académica');
    expect(text).toContain('Inscripción a carrera');
    expect(text).toContain('Propuesta académica');
    expect(text).toContain('Continuar');
  });

  it('should navigate through the visible flow states', () => {
    fixture.detectChanges();

    clickPrimaryAction();
    expect(fixture.nativeElement.textContent).toContain('Información personal');

    clickPrimaryAction();
    expect(fixture.nativeElement.textContent).toContain('Confirmá tu inscripción');

    clickPrimaryAction();
    expect(fixture.nativeElement.textContent).toContain('¡Confirmamos tu inscripción!');
    expect(fixture.nativeElement.textContent).toContain('Ir al panel principal');
  });

  function clickPrimaryAction(): void {
    const button = fixture.nativeElement.querySelector(
      '.inscription-footer button[ort-primary-button]'
    ) as HTMLButtonElement;

    button.click();
    fixture.detectChanges();
  }
});
