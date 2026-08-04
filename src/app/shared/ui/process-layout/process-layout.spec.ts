import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { ProcessLayout } from './process-layout';

describe('ProcessLayout', () => {
  let fixture: ComponentFixture<ProcessLayout>;
  let viewport: BehaviorSubject<BreakpointState>;

  beforeEach(() => {
    viewport = new BehaviorSubject<BreakpointState>({
      matches: true,
      breakpoints: { '(width < 26.25rem)': true },
    });

    TestBed.configureTestingModule({
      imports: [ProcessLayout],
      providers: [
        provideRouter([]),
        {
          provide: BreakpointObserver,
          useValue: { observe: vi.fn(() => viewport.asObservable()) },
        },
      ],
    });
    fixture = TestBed.createComponent(ProcessLayout);
  });

  it('switches progressively from compact through tablet to desktop', async () => {
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.process-layout--compact')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.process-layout__mobile-stepper')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.process-layout__rail')).toBeNull();

    viewport.next({
      matches: true,
      breakpoints: { '(width >= 26.25rem) and (width < 40.625rem)': true },
    });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.process-layout--wide-mobile')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.process-layout__mobile-stepper')).toBeTruthy();

    viewport.next({
      matches: true,
      breakpoints: { '(width >= 40.625rem) and (width < 52.5rem)': true },
    });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.process-layout--tablet')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.process-layout__mobile-stepper')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.process-layout__rail')).toBeNull();

    viewport.next({
      matches: true,
      breakpoints: { '(width >= 52.5rem)': true },
    });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.process-layout--desktop')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.process-layout__rail')).toBeTruthy();
  });

  it('renders process metadata received from its consumer', async () => {
    fixture.componentRef.setInput('showBack', true);
    fixture.componentRef.setInput('backLabel', 'Volver al paso 1');
    fixture.componentRef.setInput('processTitle', 'Inscripción a carrera');
    fixture.componentRef.setInput('stepperSubtitle', 'Paso 2 de 3 - Información personal');
    fixture.componentRef.setInput('currentStepId', 'encuesta');
    fixture.componentRef.setInput('steps', [
      { id: 'propuesta', overline: 'Paso 1', status: 'completed', title: 'Propuesta' },
      { id: 'encuesta', overline: 'Paso 2', status: 'current', title: 'Información personal' },
    ]);

    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Inscripción a carrera');
    expect(fixture.nativeElement.textContent).toContain('Paso 2 de 3 - Información personal');
    expect(
      fixture.nativeElement
        .querySelector('.process-layout__icon-button')
        ?.getAttribute('aria-label')
    ).toBe('Volver al paso 1');
    expect(
      fixture.nativeElement.querySelector('ort-expandable-stepper')?.getAttribute('aria-haspopup')
    ).toBe('true');
  });
});
