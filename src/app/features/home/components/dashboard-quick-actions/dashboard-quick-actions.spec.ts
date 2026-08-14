import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';

import { EnrollmentResumeContextStore } from '../../../enrollments/services/enrollment-resume-context';
import { Enrollments } from '../../../enrollments/services/enrollments';
import { DashboardQuickActions } from './dashboard-quick-actions';

const REACTIVATION_RESPONSE = {
  confirmada: false,
  enEspera: false,
  idEnrollment: 7010,
  fechaVencimientoPago: '2027-03-04',
  seniaInscripcion: 15500,
  saldoCuenta: 1200,
  resumen: { carrera: 'Sistemas', comienzo: 'Marzo 2027', turno: 'Noche' },
  seminarios: [
    {
      idEnrollment: 7010,
      idOferta: 310,
      nombre: 'Seminario de Liderazgo',
      comienzo: 'Marzo 2027',
      turno: 'Noche',
    },
    {
      idEnrollment: 7011,
      idOferta: 311,
      nombre: 'Seminario de Finanzas',
      comienzo: 'Abril 2027',
      turno: 'Noche',
    },
  ],
};

describe('DashboardQuickActions', () => {
  let inscriptions: { reactivate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    sessionStorage.clear();
    inscriptions = { reactivate: vi.fn().mockReturnValue(of(REACTIVATION_RESPONSE)) };

    TestBed.configureTestingModule({
      imports: [DashboardQuickActions],
      providers: [provideRouter([]), { provide: Enrollments, useValue: inscriptions }],
    });
  });

  it.each(['En proceso', 'Pendiente', 'Pago pendiente', 'Confirmada'])(
    'resumes %s in the common flow with the enrollment context',
    async status => {
      const fixture = createComponent(status);

      await fixture.whenStable();

      const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
      expect(link.getAttribute('href')).toBe(
        `/inscripciones?idProducto=20&idProceso=200&estado=${encodeURIComponent(status)}&nivel=1`
      );
    }
  );

  it('keeps every selected offer and enrollment id in sessionStorage when continuing', async () => {
    const fixture = createComponent('En proceso', [310, 311], [7010, 7011]);

    await fixture.whenStable();

    const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
    prepareNavigation(fixture);

    expect(TestBed.inject(EnrollmentResumeContextStore).read(20, 200)).toEqual({
      productId: 20,
      admissionProcessId: 200,
      offeringIds: [310, 311],
      enrollmentIds: [7010, 7011],
    });
    expect(link.getAttribute('href')).toBe(
      '/inscripciones?idProducto=20&idProceso=200&estado=En%20proceso&nivel=1'
    );
  });

  it('clears the transient context when opening a detail action', async () => {
    const store = TestBed.inject(EnrollmentResumeContextStore);
    store.save({
      productId: 20,
      admissionProcessId: 200,
      offeringIds: [300],
      enrollmentIds: [100],
    });
    const fixture = createComponent('Confirmada');

    await fixture.whenStable();
    prepareNavigation(fixture);

    expect(store.read(20, 200)).toBeNull();
  });

  it('keeps waiting enrollments informational', async () => {
    const fixture = createComponent('A la espera');

    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('a')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain(
      'El coordinador académico de la carrera se pondrá en contacto contigo.'
    );
  });

  it('reactivates a cancelled enrollment and navigates to resume it', async () => {
    const fixture = createComponent('Dada de baja');
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    await fixture.whenStable();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button).not.toBeNull();
    button.click();

    expect(inscriptions.reactivate).toHaveBeenCalledWith([100]);
    expect(navigateSpy).toHaveBeenCalledWith(['/inscripciones'], {
      queryParams: {
        productId: 20,
        admissionProcessId: 200,
        estado: 'Dada de baja',
        nivel: 1,
        modo: 'reactivar',
      },
    });
    const store = TestBed.inject(EnrollmentResumeContextStore);
    expect(store.read(20, 200)).toEqual({
      productId: 20,
      admissionProcessId: 200,
      offeringIds: [310, 311],
      enrollmentIds: [7010, 7011],
    });
    expect(store.takeReactivation(20, 200)).toEqual(REACTIVATION_RESPONSE);
    expect(store.takeReactivation(20, 200)).toBeNull();
  });

  it('reactivates every enrollment of a cancelled professional update package', async () => {
    const fixture = createComponent('Dada de baja', [310, 311], [7010, 7011]);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    await fixture.whenStable();
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();

    expect(inscriptions.reactivate).toHaveBeenCalledWith([7010, 7011]);
  });

  it('keeps Dada de baja inert without enrollment ids', async () => {
    const fixture = createComponent('Dada de baja', [300], []);

    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('button')).not.toBeNull();
    fixture.nativeElement.querySelector('button')?.click();

    expect(inscriptions.reactivate).not.toHaveBeenCalled();
  });

  function createComponent(
    status: string,
    offeringIds: readonly number[] = [300],
    enrollmentIds: readonly number[] = [100]
  ): ComponentFixture<DashboardQuickActions> {
    const fixture = TestBed.createComponent(DashboardQuickActions);
    fixture.componentRef.setInput('status', status);
    fixture.componentRef.setInput('careerName', 'Sistemas');
    fixture.componentRef.setInput('idProducto', 20);
    fixture.componentRef.setInput('idProceso', 200);
    fixture.componentRef.setInput('idNivelProducto', 1);
    fixture.componentRef.setInput('idEnrollments', enrollmentIds);
    fixture.componentRef.setInput('idOfertas', offeringIds);
    return fixture;
  }

  function prepareNavigation(fixture: ComponentFixture<DashboardQuickActions>): void {
    (fixture.componentInstance as unknown as { prepareNavigation: () => void }).prepareNavigation();
  }
});
