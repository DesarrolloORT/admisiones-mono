import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { DashboardQuickActions } from './dashboard-quick-actions';

describe('DashboardQuickActions', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DashboardQuickActions],
      providers: [provideRouter([])],
    });
  });

  it('resumes actionable career states in the common flow with the enrollment context', async () => {
    const fixture = createComponent('Pago pendiente');

    await fixture.whenStable();

    const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
    expect(link.textContent).toContain('Ver instrucciones de pago');
    expect(link.getAttribute('href')).toBe('/inscripciones?idProducto=20&idProceso=200');
  });

  it('keeps waiting enrollments informational', async () => {
    const fixture = createComponent('A la espera');

    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('a')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain(
      'El coordinador académico de la carrera se pondrá en contacto contigo.'
    );
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
