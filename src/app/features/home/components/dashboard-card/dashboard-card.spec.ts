import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { MiInscripcion } from '../../models/mi-inscripcion';
import { DashboardCard } from './dashboard-card';

describe('DashboardCard', () => {
  it('passes product and process identifiers to the career action', async () => {
    TestBed.configureTestingModule({
      imports: [DashboardCard],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(DashboardCard);
    fixture.componentRef.setInput('inscripcion', {
      idInscripto: 100,
      idOfertas: [300],
      idProducto: 20,
      idProceso: 200,
      idComienzo: 2,
      idTurno: 3,
      nombreProducto: 'Sistemas',
      nombreComienzo: 'Marzo 2027',
      nombreTurno: 'Noche',
      estado: 'Confirmada',
      fechaVencimientoPago: null,
      seminarios: [],
    });

    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('a')?.getAttribute('href')).toBe(
      '/inscripciones?idProducto=20&idProceso=200'
    );
  });

  it('renders the "Comienzo" summary row for a nivel 1/2 enrollment', async () => {
    TestBed.configureTestingModule({
      imports: [DashboardCard],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(DashboardCard);
    fixture.componentRef.setInput('inscripcion', buildInscripcion({ seminarios: [] }));

    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Comienzo');
  });

  it('renders only the title, without the "Comienzo" summary row, for a paquete (niveles 3/4)', async () => {
    TestBed.configureTestingModule({
      imports: [DashboardCard],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(DashboardCard);
    fixture.componentRef.setInput(
      'inscripcion',
      buildInscripcion({
        seminarios: [
          buildSeminario({ idInscripto: 1, descripcionOferta: 'Seminario A' }),
          buildSeminario({ idInscripto: 2, descripcionOferta: 'Seminario B' }),
        ],
      })
    );

    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Analista Programador');
    expect(text).not.toContain('Comienzo');
    expect(fixture.nativeElement.querySelector('a')?.getAttribute('href')).toBe(
      '/inscripciones?idProducto=20&idProceso=200'
    );
  });

  it('renders the payment deadline only for a pending payment with an informed date', async () => {
    TestBed.configureTestingModule({
      imports: [DashboardCard],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(DashboardCard);
    fixture.componentRef.setInput(
      'inscripcion',
      buildInscripcion({ estado: 'Pago pendiente', fechaVencimientoPago: '2026-07-15' })
    );

    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Fecha límite: 15/07/2026');

    fixture.componentRef.setInput(
      'inscripcion',
      buildInscripcion({ estado: 'Confirmada', fechaVencimientoPago: '2026-07-15' })
    );
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).not.toContain('Fecha límite');

    fixture.componentRef.setInput(
      'inscripcion',
      buildInscripcion({ estado: 'Pago pendiente', fechaVencimientoPago: null })
    );
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).not.toContain('Fecha límite');
  });

  function buildInscripcion(overrides: Partial<MiInscripcion>): MiInscripcion {
    return {
      idInscripto: 100,
      idOfertas: [300],
      idProducto: 20,
      idProceso: 200,
      idComienzo: 2,
      idTurno: 3,
      nombreProducto: 'Analista Programador',
      nombreComienzo: 'Marzo 2027',
      nombreTurno: 'Noche',
      estado: 'Confirmada',
      fechaVencimientoPago: null,
      seminarios: [],
      ...overrides,
    };
  }

  function buildSeminario(
    overrides: Partial<MiInscripcion['seminarios'][number]>
  ): MiInscripcion['seminarios'][number] {
    return {
      idInscripto: 1,
      idOferta: 10,
      descripcionOferta: 'Seminario A',
      idComienzo: 2,
      idTurno: 3,
      nombreComienzo: 'Marzo 2027',
      nombreTurno: 'Noche',
      ...overrides,
    };
  }
});
