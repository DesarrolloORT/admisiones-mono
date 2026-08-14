import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { AuthSession } from '../../../auth/models/auth.interface';
import { AuthSessionService } from '../../../auth/services/auth-session';
import { EnrollmentSummary } from '../../models/enrollment-summary';
import { Dashboard } from './dashboard';

describe('Dashboard', () => {
  let fixture: ComponentFixture<Dashboard>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            logout: vi.fn(),
            session: signal<AuthSession | null>({
              documentType: 'CI',
              documentNumber: '12345678',
              firstName: 'Ana',
            }),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(Dashboard);
    fixture.componentRef.setInput('enrollments', [createEnrollment({ productId: 1 })]);
    fixture.componentRef.setInput('scholarships', []);
  });

  it('should render the enrollments supplied by the home entry point', async () => {
    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('¡Hola Ana!');
    expect(text).toContain('Mis carreras');
    expect(text).toContain('Analista Programador');
  });

  it('should not render the pending payment alert without pending enrollments', async () => {
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('ort-alert')).toBeNull();
  });

  it('should ask for the payment before the deadline of the only pending enrollment', async () => {
    setPendingEnrollments(['2026-07-15']);

    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Inscripción pendiente de pago.');
    expect(text).toContain('Realizá el pago antes del 15/07/2026.');
  });

  it('should navigate to the pending enrollment payment when the alert arrow is clicked', async () => {
    setPendingEnrollments(['2026-07-15']);
    await fixture.whenStable();

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    const actionButton = (fixture.nativeElement as HTMLElement).querySelector(
      '.ort-alert__action'
    ) as HTMLButtonElement | null;
    expect(actionButton).not.toBeNull();
    actionButton?.click();

    expect(navigateSpy).toHaveBeenCalledWith(['/inscripciones'], {
      queryParams: { idProducto: 1, idProceso: 4, estado: 'Pago pendiente' },
    });
  });

  // El texto con varias fechas se cubre en `enrollment-summary.spec.ts`: con 2+ inscripciones la
  // sección de carreras monta un Swiper que jsdom no soporta en este entorno de test.

  it('should fall back to the generic detail when no deadline is informed', async () => {
    setPendingEnrollments([null]);

    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Inscripción pendiente de pago.');
    expect(text).toContain('Consultá el detalle desde Mis carreras.');
  });

  function setPendingEnrollments(deadlines: (string | null)[]): void {
    fixture.componentRef.setInput(
      'enrollments',
      deadlines.map((paymentDueDate, index) =>
        createEnrollment({
          productId: index + 1,
          status: 'Pago pendiente',
          paymentDueDate,
        })
      )
    );
  }

  function createEnrollment(overrides: Partial<EnrollmentSummary>): EnrollmentSummary {
    return {
      enrollmentId: 100,
      offeringIds: [300],
      productId: 1,
      admissionProcessId: 4,
      productLevelId: 1,
      intakeId: 2,
      shiftId: 3,
      degreeProgramName: 'Analista Programador',
      intakeName: 'Marzo 2027',
      shiftName: 'Noche',
      status: 'Confirmada',
      paymentDueDate: null,
      seminars: [],
      ...overrides,
    };
  }
});
