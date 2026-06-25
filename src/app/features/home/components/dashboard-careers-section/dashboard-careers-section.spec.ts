import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MiInscripcion } from '../../models/mi-inscripcion';
import { DashboardCareersSection } from './dashboard-careers-section';

describe('DashboardCareersSection', () => {
  let fixture: ComponentFixture<DashboardCareersSection>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DashboardCareersSection],
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

  it('should show a carousel for multiple enrollments', async () => {
    fixture = createComponent([createEnrollment(1), createEnrollment(2)]);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelectorAll('.swiper-slide')).toHaveLength(2);
  });

  function createComponent(
    inscripciones: MiInscripcion[]
  ): ComponentFixture<DashboardCareersSection> {
    const componentFixture = TestBed.createComponent(DashboardCareersSection);
    componentFixture.componentRef.setInput('inscripciones', inscripciones);
    componentFixture.componentRef.setInput('singleRow', false);
    return componentFixture;
  }

  function createEnrollment(idProducto: number): MiInscripcion {
    return {
      idProducto,
      idProceso: 4,
      idComienzo: 2,
      idTurno: 3,
      nombreProducto: `Carrera ${idProducto}`,
      nombreComienzo: 'Marzo 2027',
      nombreTurno: 'Noche',
      estado: 'Confirmada',
    };
  }
});
