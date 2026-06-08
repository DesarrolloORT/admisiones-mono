import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../../catalogs/services/catalogs';
import { Inscripcion } from './inscripcion';

describe('Inscripcion', () => {
  let fixture: ComponentFixture<Inscripcion>;
  let catalogsMock: {
    getCareers: ReturnType<typeof vi.fn>;
    getComienzos: ReturnType<typeof vi.fn>;
    getInitialSurveyCatalogs: ReturnType<typeof vi.fn>;
    getShifts: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    catalogsMock = {
      getCareers: vi.fn().mockReturnValue(
        of([
          {
            idProducto: 20,
            idNivelProducto: 1,
            nombreProducto: 'Ingeniería en Sistemas',
            nombreNivelProducto: 'Carrera universitaria',
          },
        ])
      ),
      getComienzos: vi.fn().mockReturnValue(of([{ idProceso: 200, nombreProceso: 'Agosto 2026' }])),
      getInitialSurveyCatalogs: vi.fn().mockReturnValue(
        of({
          aniosAprobadosEducacionSuperior: [],
          compartidoCon: [],
          decisionCarrera: [{ id: 1, label: 'Salida laboral' }],
          decisionUniversidad: [{ id: 2, label: 'Prestigio académico' }],
          estadoEducacionSuperior: [{ id: 3, label: 'No cursé estudios superiores' }],
          formacionTutores: [{ id: 4, label: 'Universitaria completa' }],
          nivelConocimiento: [],
        })
      ),
      getShifts: vi.fn().mockReturnValue(
        of([
          {
            idOferta: 300,
            idTurno: 10,
            nombreTurno: 'Nocturno',
            horarioReferencia: '19:00 a 23:00',
          },
        ])
      ),
    };

    TestBed.configureTestingModule({
      imports: [Inscripcion],
      providers: [provideRouter([]), { provide: Catalogs, useValue: catalogsMock }],
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
