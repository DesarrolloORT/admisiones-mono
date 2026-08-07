import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, RouterLink } from '@angular/router';

import { MiInscripcion } from '../../models/mi-inscripcion';
import { DashboardCareersSection } from './dashboard-careers-section';

describe('DashboardCareersSection', () => {
  let fixture: ComponentFixture<DashboardCareersSection>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DashboardCareersSection],
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
    inscripciones: MiInscripcion[]
  ): ComponentFixture<DashboardCareersSection> {
    const componentFixture = TestBed.createComponent(DashboardCareersSection);
    componentFixture.componentRef.setInput('inscripciones', inscripciones);
    componentFixture.componentRef.setInput('singleRow', false);
    return componentFixture;
  }

  function createEnrollment(idProducto: number, idProceso = 4): MiInscripcion {
    return {
      idInscripto: idProducto,
      idOfertas: [idProducto],
      idProducto,
      idProceso,
      idComienzo: 2,
      idTurno: 3,
      nombreProducto: `Carrera ${idProducto}`,
      nombreComienzo: 'Marzo 2027',
      nombreTurno: 'Noche',
      estado: 'Confirmada',
      fechaVencimientoPago: null,
      seminarios: [],
    };
  }
});
