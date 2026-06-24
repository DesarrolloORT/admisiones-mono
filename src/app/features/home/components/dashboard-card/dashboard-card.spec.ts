import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { DashboardCard } from './dashboard-card';

describe('DashboardCard', () => {
  it('passes product and process identifiers to the career action', async () => {
    TestBed.configureTestingModule({
      imports: [DashboardCard],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(DashboardCard);
    fixture.componentRef.setInput('inscripcion', {
      idProducto: 20,
      idProceso: 200,
      idComienzo: 2,
      idTurno: 3,
      nombreProducto: 'Sistemas',
      nombreComienzo: 'Marzo 2027',
      nombreTurno: 'Noche',
      estado: 'Confirmada',
    });

    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('a')?.getAttribute('href')).toBe(
      '/inscripciones/detalle?idProducto=20&idProceso=200'
    );
  });
});
