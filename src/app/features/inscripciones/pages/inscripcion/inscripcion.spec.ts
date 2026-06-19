import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../../catalogs/services/catalogs';
import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';
import { Inscripciones } from '../../services/inscripciones';
import { Inscripcion } from './inscripcion';

describe('Inscripcion', () => {
  let fixture: ComponentFixture<Inscripcion>;
  let facade: InscripcionFlowFacade;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [Inscripcion],
      providers: [
        provideRouter([]),
        {
          provide: Catalogs,
          useValue: {
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
            getComienzos: vi
              .fn()
              .mockReturnValue(of([{ idProceso: 200, nombreProceso: 'Agosto 2026' }])),
            getInitialSurveyCatalogs: vi.fn().mockReturnValue(
              of({
                aniosAprobadosEducacionSuperior: [],
                compartidoCon: [{ id: 5, label: 'Familia' }],
                decisionCarrera: [],
                decisionUniversidad: [{ id: 2, label: 'Prestigio académico' }],
                estadoEducacionSuperior: [{ id: 3, label: 'No cursé estudios superiores' }],
                formacionTutores: [{ id: 4, label: 'Universitaria completa' }],
                nivelConocimiento: [],
              })
            ),
            getTurnos: vi.fn().mockReturnValue(
              of([
                {
                  idOferta: 300,
                  idTurno: 10,
                  nombreTurno: 'Nocturno',
                  horarioReferencia: '19:00 a 23:00',
                },
              ])
            ),
          },
        },
        {
          provide: Inscripciones,
          useValue: {
            registerProductInterest: vi.fn().mockReturnValue(of(true)),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(Inscripcion);
    facade = fixture.debugElement.injector.get(InscripcionFlowFacade);
  });

  it('renders the academic proposal as the initial screen', () => {
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Paso 1 de 3 - Propuesta académica');
    expect(text).toContain('Propuesta académica');
    expect(text).toContain('Continuar');
  });

  it('renders survey, payment and terminal screens from explicit states', () => {
    facade.screen.set('encuesta');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Información personal');
    expect(fixture.nativeElement.textContent).toContain('Verificación de identidad');

    facade.screen.set('pago');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Confirmá tu inscripción');

    facade.screen.set('inscripcion-en-proceso');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Inscripción en proceso');
  });
});
