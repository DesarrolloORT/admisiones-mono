import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Catalogs } from '../../../catalogs/services/catalogs';
import { InscripcionPaymentFacade } from '../../facades/inscripcion-payment';
import { Inscripciones } from '../../services/inscripciones';
import { InscripcionProcessStore } from '../../store/inscripcion-process';
import { Inscripcion } from './inscripcion';

describe('Inscripcion', () => {
  let fixture: ComponentFixture<Inscripcion>;
  let process: InscripcionProcessStore;
  let payment: InscripcionPaymentFacade;

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
            confirmPreEnrollment: vi.fn().mockReturnValue(
              of({
                confirmada: true,
                fechaVencimientoPago: null,
                seniaInscripcion: null,
                resumen: null,
              })
            ),
            getIdentityPreload: vi
              .fn()
              .mockReturnValue(
                of({ frente: null, dorso: null, selfie: null, fechaVencimiento: null })
              ),
            getStudentRegulationAcceptance: vi
              .fn()
              .mockReturnValue(of({ aceptoReglamentoEstudiantil: false, fechaAceptacion: null })),
            getInitialSurvey: vi.fn().mockReturnValue(
              of({
                tieneDerechoEncuesta: true,
                encuesta: null,
                opcionesMotivosSeleccionados: null,
              })
            ),
            registerProductInterest: vi.fn().mockReturnValue(of(true)),
            saveInitialSurvey: vi.fn().mockReturnValue(of(true)),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(Inscripcion);
    process = fixture.debugElement.injector.get(InscripcionProcessStore);
    payment = fixture.debugElement.injector.get(InscripcionPaymentFacade);
  });

  it('renders the academic proposal as the initial screen', async () => {
    await fixture.whenStable();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Paso 1 de 3 - Propuesta académica');
    expect(text).toContain('Propuesta académica');
    expect(text).toContain('Continuar');
  });

  it('renders survey, payment and terminal screens from explicit states', async () => {
    process.flow.goTo('encuesta');
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Información personal');
    expect(fixture.nativeElement.textContent).toContain('Verificación de identidad');

    process.flow.goTo('pago');
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Confirmación');

    payment.outcome.set('inscripcion-en-proceso');
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Inscripción en proceso');
  });
});
