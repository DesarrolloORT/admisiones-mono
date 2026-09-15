import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipPersonalFacade } from '../../../../../../facades/scholarship-personal';
import { ScholarshipProcessFacade } from '../../../../../../facades/scholarship-process';
import { ScholarshipPersonalDataSection } from './scholarship-personal-data-section';

describe('ScholarshipPersonalDataSection', () => {
  let fixture: ComponentFixture<ScholarshipPersonalDataSection>;
  let facade: ScholarshipPersonalFacade;
  let breakpoint: ReturnType<typeof signal<{ isXSmall: boolean; isSmall: boolean }>>;

  beforeEach(() => {
    breakpoint = signal({ isXSmall: false, isSmall: false });

    TestBed.configureTestingModule({
      imports: [ScholarshipPersonalDataSection],
      providers: [
        ScholarshipProcessFacade,
        ScholarshipPersonalFacade,
        { provide: BreakpointService, useValue: { breakpoint } },
      ],
    });

    facade = TestBed.inject(ScholarshipPersonalFacade);
    fixture = TestBed.createComponent(ScholarshipPersonalDataSection);
    fixture.detectChanges();
  });

  it('asks how the person will attend', () => {
    const group = fixture.nativeElement.querySelector('ort-radio-group');
    const options = Array.from(fixture.nativeElement.querySelectorAll('ort-radio-button'));

    expect(group.getAttribute('legend')).toBe('Si comenzás la carrera, ¿cómo asistirás?');
    expect(options.map(option => (option as Element).getAttribute('value'))).toEqual([
      'montevideo',
      'viajare',
    ]);
  });

  it('shows the error only after trying to continue and clears it with an answer', () => {
    expect(fixture.nativeElement.querySelector('ort-error')).toBeFalsy();

    facade.submitted.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ort-error').textContent).toContain(
      'Seleccioná una opción'
    );

    facade.personalDataForm.controls.attendanceMode.setValue('montevideo');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ort-error')).toBeFalsy();
  });

  it('stacks the radio group on small screens', () => {
    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('horizontal');
    expect(fixture.componentInstance['radioGroupIndicatorPosition']()).toBe('left');

    breakpoint.set({ isXSmall: true, isSmall: false });

    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('vertical');
    expect(fixture.componentInstance['radioGroupIndicatorPosition']()).toBe('right');
  });
});
