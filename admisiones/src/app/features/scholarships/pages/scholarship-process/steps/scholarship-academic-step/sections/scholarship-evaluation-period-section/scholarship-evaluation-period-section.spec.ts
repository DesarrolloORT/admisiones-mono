import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipProcessFacade } from '../../../../../../facades/scholarship-process';
import { ScholarshipProposalFacade } from '../../../../../../facades/scholarship-proposal';
import { ScholarshipEvaluationPeriodSection } from './scholarship-evaluation-period-section';

describe('ScholarshipEvaluationPeriodSection', () => {
  let fixture: ComponentFixture<ScholarshipEvaluationPeriodSection>;
  let facade: ScholarshipProposalFacade;
  let breakpoint: ReturnType<typeof signal<{ isXSmall: boolean; isSmall: boolean }>>;

  beforeEach(() => {
    breakpoint = signal({ isXSmall: false, isSmall: false });

    TestBed.configureTestingModule({
      imports: [ScholarshipEvaluationPeriodSection],
      providers: [
        ScholarshipProcessFacade,
        ScholarshipProposalFacade,
        { provide: BreakpointService, useValue: { breakpoint } },
      ],
    });

    facade = TestBed.inject(ScholarshipProposalFacade);
    fixture = TestBed.createComponent(ScholarshipEvaluationPeriodSection);
    fixture.detectChanges();
  });

  it('offers the available test dates', () => {
    const group = fixture.nativeElement.querySelector('ort-radio-group');
    const options = Array.from(fixture.nativeElement.querySelectorAll('ort-radio-card-button'));

    expect(group.getAttribute('legend')).toBe('Fecha de prueba');
    expect(options.map(option => (option as Element).getAttribute('value'))).toEqual([
      'fecha1',
      'fecha2',
    ]);
  });

  it('asks for a date once the person tried to continue', () => {
    // fbc rinde prueba, asi que la fecha es obligatoria.
    facade.setVariant('fbc');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('ort-error')).toBeFalsy();

    facade.submitted.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ort-error').textContent).toContain(
      'Seleccioná una fecha de prueba'
    );

    facade.evaluationDateControl.setValue('fecha1');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ort-error')).toBeFalsy();
  });

  it('stacks the radio group on small screens', () => {
    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('horizontal');
    expect(fixture.componentInstance['radioGroupIndicatorPosition']()).toBe('left');

    breakpoint.set({ isXSmall: false, isSmall: true });

    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('vertical');
    expect(fixture.componentInstance['radioGroupIndicatorPosition']()).toBe('right');
  });
});
