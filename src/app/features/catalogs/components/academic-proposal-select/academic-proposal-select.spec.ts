import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { of } from 'rxjs';
import { vi } from 'vitest';

import type { AcademicProposalForm } from '../../models/academic-proposal';
import { Catalogs } from '../../services/catalogs';
import { AcademicProposalSelect } from './academic-proposal-select';

describe('AcademicProposalSelect', () => {
  beforeAll(() => {
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
      configurable: true,
      value: vi.fn(),
    });
  });

  let fixture: ComponentFixture<AcademicProposalSelect>;
  let form: FormGroup<AcademicProposalForm>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [AcademicProposalSelect],
      providers: [
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
              ]),
            getComienzos: () => of([]),
            getTurnos: () => of([]),
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

  it('selects a career from the mobile drawer', () => {
    const component = fixture.componentInstance as unknown as {
      careerOptionGroups(): readonly {
        label: string;
        options: readonly { label: string; value: string; school?: string }[];
      }[];
      confirmMobileSelection(): void;
      filteredMobileOptionGroups(): readonly {
        label: string;
        options: readonly { label: string; value: string; school?: string }[];
      }[];
      filteredMobileOptions(): readonly { label: string; value: string; school?: string }[];
      mobileDrawerField(): 'career' | 'start' | 'shift' | null;
      onMobileSearchInput(event: Event): void;
      openMobileDrawer(field: 'career'): void;
      selectMobileOption(value: string): void;
    };

    form.controls.tipoPropuesta.setValue('1');
    fixture.detectChanges();

    expect(component.careerOptionGroups().map(group => group.label)).toEqual([
      'Facultad de Diseño',
      'Facultad de Ingeniería',
    ]);
    component.openMobileDrawer('career');
    component.onMobileSearchInput({ target: { value: 'gráfico' } } as unknown as Event);

    expect(component.filteredMobileOptions()).toEqual([
      { value: '20', label: 'Licenciatura en Diseño Gráfico', school: 'Facultad de Diseño' },
    ]);
    expect(component.filteredMobileOptionGroups()).toEqual([
      {
        label: 'Facultad de Diseño',
        options: [
          {
            value: '20',
            label: 'Licenciatura en Diseño Gráfico',
            school: 'Facultad de Diseño',
          },
        ],
      },
    ]);
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('.academic-proposal-select__drawer-group')?.textContent
    ).toContain('Facultad de Diseño');

    component.selectMobileOption('20');
    component.confirmMobileSelection();

    expect(form.controls.carrera.value).toBe('20');
    expect(form.controls.carrera.touched).toBe(true);
    expect(component.mobileDrawerField()).toBeNull();
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

function createForm(): FormGroup<AcademicProposalForm> {
  return new FormGroup<AcademicProposalForm>({
    tipoPropuesta: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    carrera: new FormControl('', { nonNullable: true, validators: Validators.required }),
    comienzo: new FormControl('', { nonNullable: true, validators: Validators.required }),
    turno: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });
}
