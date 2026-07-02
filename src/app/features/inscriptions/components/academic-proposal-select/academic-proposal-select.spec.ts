import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { vi } from 'vitest';

import type { AcademicProposalForm } from '../../../catalogs/models/academic-proposal';
import { Catalogs } from '../../../catalogs/services/catalogs';
import { ResponsiveSelect } from '../responsive-select/responsive-select';
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
