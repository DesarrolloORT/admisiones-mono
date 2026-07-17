import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { of } from 'rxjs';
import { ResponsiveSelect } from 'src/app/shared/ui/responsive-select/responsive-select';
import { vi } from 'vitest';

import type { AcademicProposalForm } from '../../models/academic-proposal';
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
        { provide: BreakpointService, useValue: { breakpoint } },
        {
          provide: Catalogs,
          useValue: {
            getCareers: () =>
              of([
                {
                  idProducto: 20,
                  idNivelProducto: 1,
                  nombreProducto: 'Licenciatura en Diseño Gráfico',
                  nombreNivelProducto: 'Carrera universitaria',
                  nombreEscuela: 'Facultad de Diseño',
                },
                {
                  idProducto: 30,
                  idNivelProducto: 1,
                  nombreProducto: 'Analista Programador',
                  nombreNivelProducto: 'Carrera universitaria',
                  nombreEscuela: 'Facultad de Ingeniería',
                },
                {
                  idProducto: 40,
                  idNivelProducto: 3,
                  idProceso: 210,
                  nombreProducto: 'Programa de Asesoramiento Financiero',
                  nombreNivelProducto: 'Actualización profesional',
                },
              ]),
            getComienzos: () => of([]),
            getTurnos: () => of([]),
            getSeminarios: () =>
              of([{ idOferta: 300, idProceso: 200, nombre: 'Marco legal', fechaComienzo: null }]),
          },
        },
      ],
    });

    form = createForm();
    fixture = TestBed.createComponent(AcademicProposalSelect);
    fixture.componentRef.setInput('form', form);
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

  it('selects a career from the mobile drawer', () => {
    const component = fixture.componentInstance as unknown as {
      careerOptionGroups(): readonly {
        label: string;
        options: readonly { label: string; value: string; school?: string }[];
      }[];
    };

    form.controls.tipoPropuesta.setValue('1');
    fixture.detectChanges();

    expect(component.careerOptionGroups().map(group => group.label)).toEqual([
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

    expect(form.controls.carrera.value).toBe('20');
    expect(form.controls.carrera.touched).toBe(true);
  });

  it('keeps the current field labels for non-AP proposals (regression guard)', () => {
    form.controls.tipoPropuesta.setValue('1');
    fixture.detectChanges();

    expect(fieldLabel(fixture, 'academic-proposal-career')).toContain('Carrera');
    expect(fieldLabel(fixture, 'academic-proposal-start')).toContain('Comienzo');
    expect(fieldLabel(fixture, 'academic-proposal-shift')).toContain('Turno');
    expect(fixture.nativeElement.querySelector('#academic-proposal-seminars-mobile')).toBeNull();
  });

  it('shows Programa and reveals Seminario only after picking a program in AP', () => {
    form.controls.tipoPropuesta.setValue('3');
    fixture.detectChanges();

    expect(fieldLabel(fixture, 'academic-proposal-career')).toContain('Programa');
    expect(fixture.nativeElement.querySelector('#academic-proposal-seminars-mobile')).toBeNull();
    expect(fixture.nativeElement.querySelector('#academic-proposal-start-mobile')).toBeNull();
    expect(fixture.nativeElement.querySelector('#academic-proposal-shift-mobile')).toBeNull();

    form.controls.carrera.setValue('40');
    fixture.detectChanges();

    expect(fieldLabel(fixture, 'academic-proposal-seminars')).toContain('Seminario');
    expect(fixture.nativeElement.querySelector('#academic-proposal-start-mobile')).toBeNull();
    expect(fixture.nativeElement.querySelector('#academic-proposal-shift-mobile')).toBeNull();
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

function fieldLabel(fixture: ComponentFixture<AcademicProposalSelect>, id: string): string {
  return (
    (fixture.nativeElement.querySelector(`label[for="${id}-mobile"]`) as HTMLElement | null)
      ?.textContent ?? ''
  );
}

function createForm(): FormGroup<AcademicProposalForm> {
  return new FormGroup<AcademicProposalForm>({
    tipoPropuesta: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    carrera: new FormControl('', { nonNullable: true, validators: Validators.required }),
    comienzo: new FormControl('', { nonNullable: true, validators: Validators.required }),
    turno: new FormControl('', { nonNullable: true, validators: Validators.required }),
    seminarios: new FormControl<string[]>([], { nonNullable: true }),
  });
}
