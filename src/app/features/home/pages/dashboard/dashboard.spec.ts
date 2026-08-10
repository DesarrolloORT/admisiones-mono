import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { AuthSession } from '../../../auth/models/auth.interface';
import { AuthSessionService } from '../../../auth/services/auth-session';
import { MiInscripcion } from '../../models/mi-inscripcion';
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
              primerNombre: 'Ana',
            }),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(Dashboard);
    fixture.componentRef.setInput('inscripciones', [createEnrollment({ idProducto: 1 })]);
    fixture.componentRef.setInput('becas', []);
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
      queryParams: { idProducto: 1, idProceso: 4 },
    });
  });

  // El texto con varias fechas se cubre en `mi-inscripcion.spec.ts`: con 2+ inscripciones la
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
      'inscripciones',
      deadlines.map((fechaVencimientoPago, index) =>
        createEnrollment({
          idProducto: index + 1,
          estado: 'Pago pendiente',
          fechaVencimientoPago,
        })
      )
    );
  }

  function createEnrollment(overrides: Partial<MiInscripcion>): MiInscripcion {
    return {
      idInscripto: 100,
      idOfertas: [300],
      idProducto: 1,
      idProceso: 4,
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
});
