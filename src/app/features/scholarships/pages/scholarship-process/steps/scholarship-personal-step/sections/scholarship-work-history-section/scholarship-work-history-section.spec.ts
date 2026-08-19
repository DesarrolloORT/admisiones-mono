import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipPersonalFacade } from '../../../../../../facades/scholarship-personal';
import { ScholarshipProcessFacade } from '../../../../../../facades/scholarship-process';
import { ScholarshipWorkHistorySection } from './scholarship-work-history-section';

describe('ScholarshipWorkHistorySection', () => {
  let fixture: ComponentFixture<ScholarshipWorkHistorySection>;
  let facade: ScholarshipPersonalFacade;
  let breakpoint: ReturnType<typeof signal<{ isXSmall: boolean; isSmall: boolean }>>;

  beforeEach(() => {
    breakpoint = signal({ isXSmall: false, isSmall: false });

    TestBed.configureTestingModule({
      imports: [ScholarshipWorkHistorySection],
      providers: [
        ScholarshipProcessFacade,
        ScholarshipPersonalFacade,
        { provide: BreakpointService, useValue: { breakpoint } },
      ],
    });

    facade = TestBed.inject(ScholarshipPersonalFacade);
    fixture = TestBed.createComponent(ScholarshipWorkHistorySection);
    fixture.detectChanges();
  });

  it('asks whether the person has work experience', () => {
    const group = fixture.nativeElement.querySelector('ort-radio-group');

    expect(group.getAttribute('legend')).toBe('¿Contás con experiencia laboral?');
    expect(
      Array.from(fixture.nativeElement.querySelectorAll('ort-radio-button')).map(option =>
        (option as Element).getAttribute('value')
      )
    ).toEqual(['si', 'no']);
  });

  it('shows the error once the control is touched and clears it with an answer', () => {
    expect(fixture.nativeElement.querySelector('ort-error')).toBeFalsy();

    facade.workHistoryForm.controls.workHistory.markAsTouched();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ort-error').textContent).toContain(
      'Seleccioná una opción'
    );

    facade.workHistoryForm.controls.workHistory.setValue('no');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ort-error')).toBeFalsy();
  });

  it('stacks the radio group on small screens', () => {
    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('horizontal');

    breakpoint.set({ isXSmall: false, isSmall: true });

    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('vertical');
    expect(fixture.componentInstance['radioGroupIndicatorPosition']()).toBe('right');
  });
});
