import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ScholarshipProcessFacade } from '../../../../../../facades/scholarship-process';
import { ScholarshipProposalFacade } from '../../../../../../facades/scholarship-proposal';
import {
  ScholarshipAcademicData,
  type ScholarshipAcademicStepData,
} from '../../../../../../services/scholarship-academic-data';
import { ScholarshipInscriptionSection } from './scholarship-inscription-section';

const ANALISTA: ScholarshipAcademicStepData = {
  carrera: 'Analista Programador',
  comienzo: 'Marzo 2027',
  turno: 'Noche',
};
const DISENO: ScholarshipAcademicStepData = {
  carrera: 'Licenciatura en Diseño',
  comienzo: 'Marzo 2027',
  turno: 'Tarde',
};

describe('ScholarshipInscriptionSection', () => {
  let fixture: ComponentFixture<ScholarshipInscriptionSection>;
  let facade: ScholarshipProposalFacade;
  let breakpoint: ReturnType<typeof signal<{ isXSmall: boolean; isSmall: boolean }>>;

  function setup(data: ScholarshipAcademicStepData[]): void {
    breakpoint = signal({ isXSmall: false, isSmall: false });

    TestBed.configureTestingModule({
      imports: [ScholarshipInscriptionSection],
      providers: [
        ScholarshipProcessFacade,
        ScholarshipProposalFacade,
        { provide: BreakpointService, useValue: { breakpoint } },
        {
          provide: ScholarshipAcademicData,
          useValue: { getAcademicStepData: vi.fn().mockReturnValue(of(data)) },
        },
      ],
    });

    facade = TestBed.inject(ScholarshipProposalFacade);
    fixture = TestBed.createComponent(ScholarshipInscriptionSection);
    fixture.detectChanges();
  }

  afterEach(() => TestBed.resetTestingModule());

  // Con una sola inscripcion no hay nada que elegir: se muestra en modo lectura.
  it('shows the only enrollment as read-only data', () => {
    setup([ANALISTA]);

    expect(fixture.nativeElement.querySelector('ort-radio-group')).toBeFalsy();
    expect(fixture.nativeElement.querySelector('#scholarship-career').value).toBe(
      'Analista Programador'
    );
    expect(fixture.nativeElement.querySelector('#scholarship-start').value).toBe('Marzo 2027');
    expect(fixture.nativeElement.querySelector('#scholarship-shift').value).toBe('Noche');
  });

  it('asks which enrollment to apply with when there is more than one', () => {
    setup([ANALISTA, DISENO]);

    const options = Array.from(fixture.nativeElement.querySelectorAll('ort-radio-card-button'));

    // El valor va por property binding, asi que el texto de la card es lo observable.
    expect(options.map(option => (option as Element).textContent?.trim())).toEqual([
      'Analista Programador',
      'Licenciatura en Diseño',
    ]);
    // Nada seleccionado todavia: los datos de la carrera no se muestran.
    expect(fixture.nativeElement.querySelector('#scholarship-career')).toBeFalsy();
  });

  it('fills the read-only data with the enrollment the person picks', () => {
    setup([ANALISTA, DISENO]);

    facade.onInscriptionSelectionChange('Licenciatura en Diseño');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('#scholarship-career').value).toBe(
      'Licenciatura en Diseño'
    );
    expect(fixture.nativeElement.querySelector('#scholarship-shift').value).toBe('Tarde');
  });

  it('asks for a choice once the person tried to continue', () => {
    setup([ANALISTA, DISENO]);

    facade.submitted.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ort-error').textContent).toContain(
      'Seleccioná una opción'
    );
  });

  // La modalidad solo aplica a fexa: es lo que parte la beca en fexaCon / fexaSin.
  it('asks for the application mode only for the fexa variants', () => {
    setup([ANALISTA]);
    expect(fixture.nativeElement.textContent).not.toContain('Modalidad de postulación');

    facade.setVariant('fexaCon');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Modalidad de postulación');
  });

  it('stacks the radio groups on small screens', () => {
    setup([ANALISTA]);

    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('horizontal');

    breakpoint.set({ isXSmall: true, isSmall: false });

    expect(fixture.componentInstance['radioGroupOrientation']()).toBe('vertical');
    expect(fixture.componentInstance['radioGroupIndicatorPosition']()).toBe('right');
  });
});
