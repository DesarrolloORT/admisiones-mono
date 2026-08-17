import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { EnrollmentSummary } from '../../models/enrollment-summary';
import { DashboardCard } from './dashboard-card';

describe('DashboardCard', () => {
  it('passes product and process identifiers to the degreeProgram action', async () => {
    TestBed.configureTestingModule({
      imports: [DashboardCard],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(DashboardCard);
    fixture.componentRef.setInput('enrollment', {
      enrollmentId: 100,
      offeringIds: [300],
      productId: 20,
      admissionProcessId: 200,
      productLevelId: 1,
      intakeId: 2,
      shiftId: 3,
      degreeProgramName: 'Sistemas',
      intakeName: 'Marzo 2027',
      shiftName: 'Noche',
      status: 'Confirmada',
      paymentDueDate: null,
      seminars: [],
    });

    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('a')?.getAttribute('href')).toBe(
      '/inscripciones?idProducto=20&idProceso=200&estado=Confirmada&nivel=1'
    );
  });

  it('renders the "Comienzo" summary row for a nivel 1/2 enrollment', async () => {
    TestBed.configureTestingModule({
      imports: [DashboardCard],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(DashboardCard);
    fixture.componentRef.setInput('enrollment', buildEnrollment({ seminars: [] }));

    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Comienzo');
  });

  it('renders the "Comienzo" summary row for a paquete with a single seminario', async () => {
    TestBed.configureTestingModule({
      imports: [DashboardCard],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(DashboardCard);
    fixture.componentRef.setInput(
      'enrollment',
      buildEnrollment({
        seminars: [buildSeminar({ enrollmentId: 1, offeringDescription: 'Seminario A' })],
      })
    );

    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Comienzo');
    expect(text).not.toContain('Anotado a');
  });

  it('renders only the title and the seminar count, without the "Comienzo" summary row, for a paquete con varios seminarios', async () => {
    TestBed.configureTestingModule({
      imports: [DashboardCard],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(DashboardCard);
    fixture.componentRef.setInput(
      'enrollment',
      buildEnrollment({
        seminars: [
          buildSeminar({ enrollmentId: 1, offeringDescription: 'Seminario A' }),
          buildSeminar({ enrollmentId: 2, offeringDescription: 'Seminario B' }),
        ],
      })
    );

    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Analista Programador');
    expect(text).toContain('Anotado a 2 seminarios');
    expect(text).not.toContain('Comienzo');
    expect(fixture.nativeElement.querySelector('a')?.getAttribute('href')).toBe(
      '/inscripciones?idProducto=20&idProceso=200&estado=Confirmada&nivel=1'
    );
  });

  function buildEnrollment(overrides: Partial<EnrollmentSummary>): EnrollmentSummary {
    return {
      enrollmentId: 100,
      offeringIds: [300],
      productId: 20,
      admissionProcessId: 200,
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

  function buildSeminar(
    overrides: Partial<EnrollmentSummary['seminars'][number]>
  ): EnrollmentSummary['seminars'][number] {
    return {
      enrollmentId: 1,
      offeringId: 10,
      offeringDescription: 'Seminario A',
      intakeId: 2,
      shiftId: 3,
      intakeName: 'Marzo 2027',
      shiftName: 'Noche',
      ...overrides,
    };
  }
});
