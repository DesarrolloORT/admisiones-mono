import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, RouterLink } from '@angular/router';

import { EnrollmentSummary } from '../../models/enrollment-summary';
import { DashboardEnrollmentsSection } from './dashboard-enrollments-section';

describe('DashboardCareersSection', () => {
  let fixture: ComponentFixture<DashboardEnrollmentsSection>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DashboardEnrollmentsSection],
      providers: [provideRouter([])],
    });
  });

  it('should show the career action when there are no enrollments', async () => {
    fixture = createComponent([]);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('app-dashboard-action-card')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-dashboard-card')).toBeNull();
  });

  it('should show one card without a carousel for one enrollment', async () => {
    fixture = createComponent([createEnrollment(1)]);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelectorAll('app-dashboard-card')).toHaveLength(1);
    expect(fixture.nativeElement.querySelector('.swiper')).toBeNull();
  });

  it('should navigate to inscriptions from the add career button', async () => {
    fixture = createComponent([createEnrollment(1)]);
    await fixture.whenStable();

    const button = fixture.debugElement.query(By.css('.section-header__add'));

    expect(button.injector.get(RouterLink).urlTree?.toString()).toBe('/inscripciones');
  });

  it('should show a carousel for multiple enrollments', async () => {
    fixture = createComponent([createEnrollment(1), createEnrollment(2)]);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelectorAll('.swiper-slide')).toHaveLength(2);
  });

  it('should not throw and show a carousel for groups sharing the same idProducto with different idProceso', async () => {
    fixture = createComponent([createEnrollment(1, 100), createEnrollment(1, 200)]);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelectorAll('.swiper-slide')).toHaveLength(2);
  });

  function createComponent(
    enrollments: EnrollmentSummary[]
  ): ComponentFixture<DashboardEnrollmentsSection> {
    const componentFixture = TestBed.createComponent(DashboardEnrollmentsSection);
    componentFixture.componentRef.setInput('enrollments', enrollments);
    componentFixture.componentRef.setInput('singleRow', false);
    return componentFixture;
  }

  function createEnrollment(productId: number, admissionProcessId = 4): EnrollmentSummary {
    return {
      enrollmentId: productId,
      offeringIds: [productId],
      productId,
      admissionProcessId,
      productLevelId: 1,
      intakeId: 2,
      shiftId: 3,
      degreeProgramName: `Carrera ${productId}`,
      intakeName: 'Marzo 2027',
      shiftName: 'Noche',
      status: 'Confirmada',
      paymentDueDate: null,
      seminars: [],
    };
  }
});
