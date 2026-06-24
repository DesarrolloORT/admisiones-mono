import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ProcessLayout } from './process-layout';

describe('ProcessLayout', () => {
  let fixture: ComponentFixture<ProcessLayout>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProcessLayout],
      providers: [provideRouter([])],
    });
    fixture = TestBed.createComponent(ProcessLayout);
  });

  it('renders process metadata received from its consumer', async () => {
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
  });
});
