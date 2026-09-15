import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';
import { vi } from 'vitest';

import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
import { type ScholarshipKind, ScholarshipProcess } from './scholarship-process';

describe('ScholarshipProcess', () => {
  let fixture: ComponentFixture<ScholarshipProcess>;
  let component: ScholarshipProcess;
  let process: ScholarshipProcessFacade;
  let breakpoint: ReturnType<typeof signal<{ isXSmall: boolean; isSmall: boolean }>>;
  let logout: ReturnType<typeof vi.fn>;

  function setup(kind: ScholarshipKind): void {
    breakpoint = signal({ isXSmall: false, isSmall: false });
    logout = vi.fn();

    TestBed.configureTestingModule({
      imports: [ScholarshipProcess],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { data: { kind } } } },
        { provide: BreakpointService, useValue: { breakpoint } },
        { provide: AuthSessionService, useValue: { logout } },
      ],
    });

    fixture = TestBed.createComponent(ScholarshipProcess);
    fixture.detectChanges();
    component = fixture.componentInstance;
    process = fixture.debugElement.injector.get(ScholarshipProcessFacade);
  }

  afterEach(() => TestBed.resetTestingModule());

  it('opens on the onboarding screen and only starts the flow when asked', () => {
    setup('fbr');

    expect(fixture.nativeElement.querySelector('app-scholarship-onboarding-step')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-process-layout')).toBeFalsy();

    component['startApplication']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-process-layout')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-scholarship-academic-step')).toBeTruthy();
  });

  it('renders the step of the current position in the flow', () => {
    setup('fbr');
    component['startApplication']();
    fixture.detectChanges();

    process.continue();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-scholarship-personal-step')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-scholarship-academic-step')).toBeFalsy();
  });

  it('shows the success screen once the application is confirmed', () => {
    setup('fbr');
    component['startApplication']();
    fixture.detectChanges();

    component['showSuccess']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-scholarship-success-step')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-process-layout')).toBeFalsy();
  });

  // Las cuatro becas comparten pantalla: la ruta define la variante.
  it('maps every route kind to its variant', () => {
    for (const kind of ['fbr', 'fbc', 'fcl'] as const) {
      setup(kind);

      expect(component['variant']()).toBe(kind);

      TestBed.resetTestingModule();
    }
  });

  // fexa es la excepcion: su variante sale del modo de postulacion del paso 1.
  it('derives the fexa variant from the application mode instead of the route', () => {
    setup('fexa');

    expect(component['variant']()).toBe('fexaCon');

    process.applicationForm.controls.inscription.controls.applicationMode.setValue(
      'sin declaracion'
    );

    expect(component['variant']()).toBe('fexaSin');
  });

  it('offers the back control only on small screens and while the flow can go back', () => {
    setup('fbr');
    component['startApplication']();

    expect(component['showBack']()).toBe(false);

    breakpoint.set({ isXSmall: true, isSmall: false });

    expect(component['showBack']()).toBe(true);
    expect(process.canGoBack()).toBe(false);
  });

  it('leaves the flow to the dashboard and logs out through the session service', () => {
    setup('fbr');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    component['goHome']();
    component['logout']();

    expect(navigate).toHaveBeenCalledWith(['/inicio']);
    expect(logout).toHaveBeenCalledOnce();
  });
});
