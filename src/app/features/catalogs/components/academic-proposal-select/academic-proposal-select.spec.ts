import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { of } from 'rxjs';
import { ResponsiveSelect } from 'src/app/shared/ui/responsive-select/responsive-select';
import { vi } from 'vitest';

import type { AcademicProposalForm } from '../../models/academic-proposal';
import { AcademicProposalSelection } from '../../services/academic-proposal-selection';
import { Catalogs } from '../../services/catalogs';
import { AcademicProposalSelect } from './academic-proposal-select';

describe('AcademicProposalSelect', () => {
  const breakpoint = signal({
    isXSmall: true,
    isSmall: false,
    isMedium: false,
    isLarge: false,
    currentBreakpoint: 'xs',
    screenWidth: 375,
  });

  beforeAll(() => {
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
      configurable: true,
      value: vi.fn(),
    });
  });

  let fixture: ComponentFixture<AcademicProposalSelect>;
  let form: FormGroup<AcademicProposalForm>;

  beforeEach(async () => {
    breakpoint.set({
      isXSmall: true,
      isSmall: false,
      isMedium: false,
      isLarge: false,
      currentBreakpoint: 'xs',
      screenWidth: 375,
    });

    TestBed.configureTestingModule({
      imports: [AcademicProposalSelect],
      providers: [
        AcademicProposalSelection,
        { provide: BreakpointService, useValue: { breakpoint } },
        {
          provide: Catalogs,
          useValue: {
            getDegreePrograms: () =>
              of([
                {
                  productId: 20,
                  productLevelId: 1,
                  productName: 'Licenciatura en Diseño Gráfico',
                  productLevelName: 'Carrera universitaria',
                  schoolName: 'Facultad de Diseño',
                },
                {
                  productId: 30,
                  productLevelId: 1,
                  productName: 'Analista Programador',
                  productLevelName: 'Carrera universitaria',
                  schoolName: 'Facultad de Ingeniería',
                },
                {
                  productId: 40,
                  productLevelId: 3,
                  admissionProcessId: 210,
                  productName: 'Programa de Asesoramiento Financiero',
                  productLevelName: 'Actualización profesional',
                  hasSeminar: false,
                },
                {
                  productId: 41,
                  productLevelId: 4,
                  admissionProcessId: 211,
                  productName: 'Programa de Finanzas Corporativas',
                  productLevelName: 'Actualización profesional',
                  hasSeminar: true,
                },
              ]),
            getIntakes: () => of([]),
            getShifts: () => of([]),
            getSeminars: () =>
              of([
                { offeringId: 300, admissionProcessId: 200, name: 'Marco legal', startDate: null },
              ]),
          },
        },
      ],
    });

    form = createForm();
    fixture = TestBed.createComponent(AcademicProposalSelect);
    fixture.componentRef.setInput('form', form);
    fixture.componentRef.setInput('selection', TestBed.inject(AcademicProposalSelection));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('connects a form to its academic selection state', () => {
    expect(fixture.componentInstance.selection().initialized()).toBe(true);
  });

  it('adapts the design-system card content to the current breakpoint', () => {
    const card = fixture.nativeElement.querySelector('ort-card') as HTMLElement;

    expect(card.classList).toContain('academic-proposal-select__card--mobile');
    expect(fixture.nativeElement.querySelector('.academic-proposal-select__icon')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.academic-proposal-select__hint')).toBeNull();

    breakpoint.set({
      isXSmall: false,
      isSmall: false,
      isMedium: true,
      isLarge: false,
      currentBreakpoint: 'md',
      screenWidth: 900,
    });
    fixture.detectChanges();

    expect(card.classList).not.toContain('academic-proposal-select__card--mobile');
    expect(fixture.nativeElement.querySelector('.academic-proposal-select__icon')).toBeNull();
    expect(fixture.nativeElement.querySelector('.academic-proposal-select__hint')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.academic-proposal-select').classList).toContain(
      'academic-proposal-select--desktop'
    );
  });

  it('selects a degreeProgram from the mobile drawer', () => {
    const component = fixture.componentInstance as unknown as {
      degreeProgramOptionGroups(): readonly {
        label: string;
        options: readonly { label: string; value: string; school?: string }[];
      }[];
    };

    form.controls.proposalType.setValue('1');
    fixture.detectChanges();

    expect(component.degreeProgramOptionGroups().map(group => group.label)).toEqual([
      'Facultad de Diseño',
      'Facultad de Ingeniería',
    ]);

    const select = fixture.debugElement.query(By.directive(ResponsiveSelect)).componentInstance as {
      confirmDrawerValue(): void;
      onSearchInput(event: Event): void;
      openDrawer(): void;
      toggleOption(value: string): void;
    };

    select.openDrawer();
    select.onSearchInput({ target: { value: 'gráfico' } } as unknown as Event);
    fixture.detectChanges();

    select.toggleOption('20');
    select.confirmDrawerValue();

    expect(form.controls.degreeProgram.value).toBe('20');
    expect(form.controls.degreeProgram.touched).toBe(true);
  });

  it('keeps the current field labels for non-AP proposals (regression guard)', () => {
    form.controls.proposalType.setValue('1');
    fixture.detectChanges();

    expect(fieldLabel(fixture, 'academic-proposal-degree-program')).toContain('Carrera');
    expect(fieldLabel(fixture, 'academic-proposal-start')).toContain('Comienzo');
    expect(fieldLabel(fixture, 'academic-proposal-shift')).toContain('Turno');
    expect(fixture.nativeElement.querySelector('#academic-proposal-seminars-mobile')).toBeNull();
  });

  it('shows Programa and reveals Seminario only after picking a program in AP', () => {
    form.controls.proposalType.setValue('3');
    fixture.detectChanges();

    expect(fieldLabel(fixture, 'academic-proposal-degree-program')).toContain('Programa');
    expect(fixture.nativeElement.querySelector('#academic-proposal-seminars-mobile')).toBeNull();
    expect(fixture.nativeElement.querySelector('#academic-proposal-start-mobile')).toBeNull();
    expect(fixture.nativeElement.querySelector('#academic-proposal-shift-mobile')).toBeNull();

    form.controls.degreeProgram.setValue('41');
    fixture.detectChanges();

    expect(fieldLabel(fixture, 'academic-proposal-seminars')).toContain('Seminario');
    expect(fixture.nativeElement.querySelector('#academic-proposal-start-mobile')).toBeNull();
    expect(fixture.nativeElement.querySelector('#academic-proposal-shift-mobile')).toBeNull();
  });

  // Sin `hasSeminar` el programa ofrece una sola oferta, no seminarios.
  it('labels the AP offering select as Próximo comienzo when the program has no seminars', () => {
    form.controls.proposalType.setValue('3');
    form.controls.degreeProgram.setValue('40');
    fixture.detectChanges();

    expect(fieldLabel(fixture, 'academic-proposal-seminars')).toContain('Próximo comienzo');
  });

  it('preselects the only offering the AP program has', () => {
    form.controls.proposalType.setValue('3');
    form.controls.degreeProgram.setValue('40');
    TestBed.tick();

    expect(form.controls.seminars.value).toEqual(['300']);
  });

  it('only allows multiple seminars when the AP program has them', () => {
    form.controls.proposalType.setValue('3');
    form.controls.degreeProgram.setValue('41');
    fixture.detectChanges();

    expect(seminarSelect(fixture).multiple()).toBe(true);

    form.controls.degreeProgram.setValue('40');
    fixture.detectChanges();

    const select = seminarSelect(fixture);
    expect(select.multiple()).toBe(false);

    (select as unknown as { openDrawer(): void }).openDrawer();
    (select as unknown as { toggleOption(value: string): void }).toggleOption('300');
    (select as unknown as { confirmDrawerValue(): void }).confirmDrawerValue();

    expect(form.controls.seminars.value).toEqual(['300']);
  });

  it('shows required errors after controls are touched', () => {
    form.markAllAsTouched();
    form.updateValueAndValidity();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Seleccioná una propuesta académica');
    expect(
      fixture.nativeElement.querySelector('ort-radio-group')?.getAttribute('aria-invalid')
    ).toBe('true');
    expect(
      fixture.nativeElement.querySelector('ort-radio-card-button')?.getAttribute('aria-describedby')
    ).toContain('academic-proposal-type-error');
    expect(text).toContain('Seleccioná una carrera');
    expect(text).toContain('Seleccioná un comienzo');
    expect(text).toContain('Seleccioná un turno');
  });
});

function seminarSelect(fixture: ComponentFixture<AcademicProposalSelect>): ResponsiveSelect {
  const select = fixture.debugElement
    .queryAll(By.directive(ResponsiveSelect))
    .map(node => node.componentInstance as ResponsiveSelect)
    .find(instance => instance.id() === 'academic-proposal-seminars');
  if (!select) throw new Error('Seminar select not rendered.');
  return select;
}

function fieldLabel(fixture: ComponentFixture<AcademicProposalSelect>, id: string): string {
  return (
    (fixture.nativeElement.querySelector(`label[for="${id}-mobile"]`) as HTMLElement | null)
      ?.textContent ?? ''
  );
}

function createForm(): FormGroup<AcademicProposalForm> {
  return new FormGroup<AcademicProposalForm>({
    proposalType: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    degreeProgram: new FormControl('', { nonNullable: true, validators: Validators.required }),
    intake: new FormControl('', { nonNullable: true, validators: Validators.required }),
    shift: new FormControl('', { nonNullable: true, validators: Validators.required }),
    seminars: new FormControl<string[]>([], { nonNullable: true }),
  });
}
