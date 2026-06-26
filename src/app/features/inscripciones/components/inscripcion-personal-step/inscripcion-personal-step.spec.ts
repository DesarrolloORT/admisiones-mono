import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { OrtRadioModule } from '@desarrolloort/components';
import { vi } from 'vitest';

import { InscripcionSurveyFacade } from '../../facades/inscripcion-survey';
import { InscripcionPersonalStep } from './inscripcion-personal-step';

describe('InscripcionPersonalStep', () => {
  let fixture: ComponentFixture<InscripcionPersonalStep>;
  const continueSpy = vi.fn();

  beforeEach(() => {
    continueSpy.mockClear();

    TestBed.configureTestingModule({
      imports: [InscripcionPersonalStep],
      providers: [
        {
          provide: InscripcionSurveyFacade,
          useValue: {
            continue: continueSpy,
            educationForm: new FormGroup({
              tipoBachillerato: new FormControl('', { nonNullable: true }),
            }),
            baccalaureateOptions: signal([
              { value: '12', label: 'Científico' },
              { value: '13', label: 'Artístico' },
            ]),
          },
        },
      ],
    }).overrideComponent(InscripcionPersonalStep, {
      set: {
        imports: [OrtRadioModule, ReactiveFormsModule],
        template: `
          <form (keydown.enter)="onFormEnter($event)" (submit)="onSubmit($event)">
            <input type="radio" name="personal" />
            <button type="submit">Continuar</button>
          </form>
          <div [formGroup]="facade.educationForm">
            <ort-radio-group
              formControlName="tipoBachillerato"
              legend="¿Qué tipo de bachillerato?"
              name="baccalaureate-type">
              @for (option of facade.baccalaureateOptions(); track option.value) {
              <ort-radio-button [value]="option.value">{{ option.label }}</ort-radio-button>
              }
            </ort-radio-group>
          </div>
        `,
      },
    });

    fixture = TestBed.createComponent(InscripcionPersonalStep);
    fixture.detectChanges();
  });

  it('prevents native form submission before continuing the flow', () => {
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    const event = new SubmitEvent('submit', { bubbles: true, cancelable: true });

    form.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(continueSpy).toHaveBeenCalledOnce();
  });

  it('does not continue when Enter is pressed from a focused radio', () => {
    const radio = fixture.nativeElement.querySelector('input[type="radio"]') as HTMLInputElement;
    const event = new KeyboardEvent('keydown', {
      key: 'Enter',
      bubbles: true,
      cancelable: true,
    });

    radio.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(continueSpy).not.toHaveBeenCalled();
  });

  it('renders real baccalaureate options from the catalog facade', () => {
    expect(fixture.nativeElement.textContent).toContain('Científico');
    expect(fixture.nativeElement.textContent).toContain('Artístico');
  });
});
