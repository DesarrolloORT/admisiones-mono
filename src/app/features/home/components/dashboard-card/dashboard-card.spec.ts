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

  it('renders the "Comienzo" summary row for a paquete with a single seminario', async () => {
    TestBed.configureTestingModule({
      imports: [DashboardCard],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(DashboardCard);
    fixture.componentRef.setInput(
      'inscripcion',
      buildInscripcion({
        seminarios: [buildSeminario({ idInscripto: 1, descripcionOferta: 'Seminario A' })],
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
    expect(text).toContain('Anotado a 2 seminarios');
    expect(text).not.toContain('Comienzo');
    expect(fixture.nativeElement.querySelector('a')?.getAttribute('href')).toBe(
      '/inscripciones?idProducto=20&idProceso=200'
    );
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
