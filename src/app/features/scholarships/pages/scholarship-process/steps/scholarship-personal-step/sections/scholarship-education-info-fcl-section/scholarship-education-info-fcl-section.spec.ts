import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipPersonalFacade } from '../../../../../../facades/scholarship-personal';
import { ScholarshipProcessFacade } from '../../../../../../facades/scholarship-process';
import { ScholarshipEducationInfoFclSection } from './scholarship-education-info-fcl-section';

describe('ScholarshipEducationInfoFclSection', () => {
  let fixture: ComponentFixture<ScholarshipEducationInfoFclSection>;
  let facade: ScholarshipPersonalFacade;
  let breakpoint: ReturnType<typeof signal<{ isXSmall: boolean; isSmall: boolean }>>;

  beforeEach(() => {
    breakpoint = signal({ isXSmall: false, isSmall: false });

    TestBed.configureTestingModule({
      imports: [ScholarshipEducationInfoFclSection],
      providers: [
        ScholarshipProcessFacade,
        ScholarshipPersonalFacade,
        { provide: BreakpointService, useValue: { breakpoint } },
      ],
    });

    facade = TestBed.inject(ScholarshipPersonalFacade);
    fixture = TestBed.createComponent(ScholarshipEducationInfoFclSection);
    fixture.detectChanges();
  });

  // Capacitacion laboral no pide promedios ni certificado: solo si curso otros estudios.
  it('asks only whether the person studied something else', () => {
    const groups = Array.from(fixture.nativeElement.querySelectorAll('ort-radio-group'));

    expect(groups).toHaveLength(1);
    expect((groups[0] as Element).getAttribute('legend')).toBe('¿Cursaste otros estudios?');
    expect(fixture.nativeElement.querySelector('ort-file-uploader')).toBeFalsy();
  });

  it('shows the error once the person tried to continue and clears it with an answer', () => {
    facade.submitted.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ort-error').textContent).toContain(
      'Seleccioná una opción'
    );

    facade.educationInfoFclForm.controls.otherStudies.setValue('si');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ort-error')).toBeFalsy();
  });

  it('stacks the radio group on small screens', () => {
    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('horizontal');

    breakpoint.set({ isXSmall: true, isSmall: false });

    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('vertical');
    expect(fixture.componentInstance['radioGroupIndicatorPosition']()).toBe('right');
  });
});
