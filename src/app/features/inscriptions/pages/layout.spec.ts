import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { Catalogs } from '../../catalogs/services/catalogs';
import { InscripcionPaymentFacade } from '../facades/inscription-payment';
import { InscripcionSurveyFacade } from '../facades/inscription-survey';
import { Inscripciones } from '../services/inscriptions';
import { InscripcionProcessStore } from '../store/inscription-process';
import { Layout } from './layout';

describe('Layout', () => {
  let fixture: ComponentFixture<Layout>;
  let process: InscripcionProcessStore;
  let payment: InscripcionPaymentFacade;
  let survey: InscripcionSurveyFacade;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [Layout],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              data: {
                entry: { intent: 'nueva' },
                initialSurvey: {
                  initialSurvey: {
                    tieneDerechoEncuesta: true,
                    encuesta: null,
                    universidadesConsideradas: [],
                    universidadesConsideradasOtros: [],
                    universidadesEducacionSuperior: [],
                    universidadesEducacionSuperiorOtros: [],
                    opcionesMotivosSeleccionados: [],
                    opcionesPublicidadSeleccionadas: [],
                  },
                  loadFailed: false,
                },
              },
              queryParamMap: convertToParamMap({}),
            },
          },
        },
        {
          provide: Catalogs,
          useValue: {
            getDegreePrograms: vi.fn().mockReturnValue(
              of([
                {
                  productId: 20,
                  productLevelId: 1,
                  productName: 'Ingeniería en Sistemas',
                  productLevelName: 'Carrera universitaria',
                },
              ])
            ),
            getIntakes: vi
              .fn()
              .mockReturnValue(
                of([{ admissionProcessId: 200, admissionProcessName: 'Agosto 2026' }])
              ),
            getCountryLocations: vi.fn().mockReturnValue(of([])),
            getBanks: vi.fn().mockReturnValue(of([])),
            getInitialSurveyCatalogs: vi.fn().mockReturnValue(
              of({
                education: {
                  lastSecondaryYearLocations: [],
                  highSchoolYears: [],
                  previousHigherEducationOptions: [
                    { id: 3, label: 'No cursé estudios superiores' },
                  ],
                  universities: [],
                  guardianEducationLevels: [{ id: 5, label: 'Universitaria completa' }],
                },
                academicDecision: {
                  upperSecondaryYears: [{ id: 2, label: 'Prestigio académico' }],
                  decisionSupports: [{ id: 5, label: 'Familia' }],
                  decisionLevels: [],
                  universities: [],
                  ortChoiceReasons: [],
                },
                ortExperience: { ratings: [], ortAdvertisements: [] },
              })
            ),
            getShifts: vi.fn().mockReturnValue(
              of([
                {
                  offeringId: 300,
                  shiftId: 10,
                  shiftName: 'Nocturno',
                  referenceSchedule: '19:00 a 23:00',
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
                saldoCuenta: null,
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
                universidadesConsideradas: [],
                universidadesConsideradasOtros: [],
                universidadesEducacionSuperior: [],
                universidadesEducacionSuperiorOtros: [],
                opcionesMotivosSeleccionados: [],
                opcionesPublicidadSeleccionadas: [],
              })
            ),
            registerProductInterest: vi.fn().mockReturnValue(of(true)),
            saveInitialSurvey: vi.fn().mockReturnValue(of(true)),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(Layout);
    process = fixture.debugElement.injector.get(InscripcionProcessStore);
    payment = fixture.debugElement.injector.get(InscripcionPaymentFacade);
    survey = fixture.debugElement.injector.get(InscripcionSurveyFacade);
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

    payment.outcome.set('inscription-en-proceso');
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Inscripción en proceso');
  });

  it('renders the corporate payment pending message for a fresh AP flow', async () => {
    fixture.debugElement.injector.get(AcademicProposalSelection).setProposalType('3');
    survey.workForm.controls.isCorporate.setValue(true);
    payment.outcome.set('inscription-en-proceso');

    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Inscripción corporativa pendiente');
    expect(fixture.nativeElement.textContent).toContain(
      'Tu inscripción quedó pendiente del pago de la empresa. Se confirmará automáticamente cuando el pago se acredite.'
    );
    expect(fixture.nativeElement.querySelector('a[href="/inicio"]')).toBeTruthy();
  });
});
