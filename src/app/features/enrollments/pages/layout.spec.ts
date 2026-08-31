import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { CatalogsApi } from '../../catalogs/api/catalogs.api';
import { AcademicProposalSelection } from '../../catalogs/services/academic-proposal-selection';
import { EnrollmentsApi } from '../api/enrollments.api';
import { EnrollmentPaymentFacade } from '../facades/enrollment-payment';
import { EnrollmentSurveyFacade } from '../facades/enrollment-survey';
import {
  ENROLLMENT_PROCESS_STATE,
  type EnrollmentProcessState,
} from '../models/enrollment-process';
import { Enrollments } from '../services/enrollments';
import { Layout } from './layout';

describe('Layout', () => {
  let fixture: ComponentFixture<Layout>;
  let process: EnrollmentProcessState;
  let payment: EnrollmentPaymentFacade;
  let survey: EnrollmentSurveyFacade;

  beforeEach(() => {
    sessionStorage.clear();
    const enrollmentsMock = {
      confirmPreEnrollment: vi.fn().mockReturnValue(
        of({
          confirmed: true,
          paymentDueDate: null,
          enrollmentDeposit: null,
          accountBalance: null,
          summary: null,
        })
      ),
      getIdentityPreload: vi
        .fn()
        .mockReturnValue(of({ front: null, back: null, selfie: null, expirationDate: null })),
      getStudentRegulationAcceptance: vi
        .fn()
        .mockReturnValue(of({ acceptedStudentRegulation: false, acceptanceDate: null })),
      getInitialSurvey: vi.fn().mockReturnValue(
        of({
          isEligibleForSurvey: true,
          survey: null,
          consideredUniversities: [],
          otherConsideredUniversities: [],
          higherEducationUniversities: [],
          otherHigherEducationUniversities: [],
          selectedReasonOptions: [],
          selectedAdvertisingOptions: [],
        })
      ),
      registerProductInterest: vi.fn().mockReturnValue(of(true)),
      saveInitialSurvey: vi.fn().mockReturnValue(of('in-progress')),
    };
    TestBed.configureTestingModule({
      imports: [Layout],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              data: {
                entry: { intent: 'new' },
                initialSurvey: {
                  initialSurvey: {
                    isEligibleForSurvey: true,
                    survey: null,
                    consideredUniversities: [],
                    otherConsideredUniversities: [],
                    higherEducationUniversities: [],
                    otherHigherEducationUniversities: [],
                    selectedReasonOptions: [],
                    selectedAdvertisingOptions: [],
                  },
                  loadFailed: false,
                },
              },
              queryParamMap: convertToParamMap({}),
            },
          },
        },
        {
          provide: CatalogsApi,
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
          provide: Enrollments,
          useValue: enrollmentsMock,
        },
        {
          provide: EnrollmentsApi,
          useValue: enrollmentsMock,
        },
      ],
    });

    fixture = TestBed.createComponent(Layout);
    process = fixture.debugElement.injector.get(ENROLLMENT_PROCESS_STATE);
    payment = fixture.debugElement.injector.get(EnrollmentPaymentFacade);
    survey = fixture.debugElement.injector.get(EnrollmentSurveyFacade);
  });

  it('renders the academic proposal as the initial screen', async () => {
    await fixture.whenStable();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Paso 1 de 3 - Propuesta académica');
    expect(text).toContain('Propuesta académica');
    expect(text).toContain('Continuar');
  });

  it('renders survey, payment and terminal screens from explicit states', async () => {
    process.flow.goTo('survey');
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Información personal');
    expect(fixture.nativeElement.textContent).toContain('Verificación de identidad');

    process.flow.goTo('payment');
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Confirmación');

    payment.outcome.set('enrollment-in-progress');
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Inscripción en proceso');
  });

  it('renders the corporate payment pending message for a fresh AP flow', async () => {
    fixture.debugElement.injector.get(AcademicProposalSelection).setProposalType('3');
    survey.workForm.controls.isCorporate.setValue(true);
    payment.outcome.set('enrollment-in-progress');

    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Inscripción corporativa pendiente');
    expect(fixture.nativeElement.textContent).toContain(
      'Tu empresa deberá enviar la solicitud con los datos de la inscripción a sae@ort.edu.uy'
    );
    expect(fixture.nativeElement.querySelector('a[href="/inicio"]')).toBeTruthy();
  });
});
