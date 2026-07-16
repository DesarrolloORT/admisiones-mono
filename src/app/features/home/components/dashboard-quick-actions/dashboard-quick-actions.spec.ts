import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';

import { HomeService } from '../../services/home';
import { DashboardQuickActions } from './dashboard-quick-actions';

describe('DashboardQuickActions', () => {
  let homeService: { reactivarInscripcion: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    homeService = { reactivarInscripcion: vi.fn().mockReturnValue(of(true)) };

    TestBed.configureTestingModule({
      imports: [DashboardQuickActions],
      providers: [provideRouter([]), { provide: HomeService, useValue: homeService }],
    });
  });

  it.each(['En proceso', 'Pendiente', 'Pago pendiente', 'Confirmada'])(
    'resumes %s in the common flow with the enrollment context',
    async status => {
      const fixture = createComponent(status);

      await fixture.whenStable();

      const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
      expect(link.getAttribute('href')).toBe('/inscripciones?idProducto=20&idProceso=200');
    }
  );

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
    fixture.componentRef.setInput('idInscripto', 100);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    await fixture.whenStable();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button).not.toBeNull();
    button.click();

    expect(homeService.reactivarInscripcion).toHaveBeenCalledWith(100);
    expect(navigateSpy).toHaveBeenCalledWith(['/inscripciones'], {
      queryParams: { idProducto: 20, idProceso: 200 },
    });
  });

  it('keeps Dada de baja inert without an idInscripto', async () => {
    const fixture = createComponent('Dada de baja');

    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('button')).not.toBeNull();
    fixture.nativeElement.querySelector('button')?.click();

    expect(homeService.reactivarInscripcion).not.toHaveBeenCalled();
  });

  function createComponent(status: string): ComponentFixture<DashboardQuickActions> {
    const fixture = TestBed.createComponent(DashboardQuickActions);
    fixture.componentRef.setInput('status', status);
    fixture.componentRef.setInput('careerName', 'Sistemas');
    fixture.componentRef.setInput('idProducto', 20);
    fixture.componentRef.setInput('idProceso', 200);
    return fixture;
  }
});
