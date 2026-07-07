import { readFileSync } from 'node:fs';

import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { vi } from 'vitest';

import { InscripcionSurveyFacade } from '../../facades/inscription-survey';
import { InscripcionPersonalStep } from './inscription-personal-step';

describe('InscripcionPersonalStep', () => {
  let fixture: ComponentFixture<InscripcionPersonalStep>;
  const continueSpy = vi.fn();
  const breakpoint = signal({
    isXSmall: true,
    isSmall: false,
    isMedium: false,
    isLarge: false,
    currentBreakpoint: 'xs',
    screenWidth: 375,
  });

  beforeEach(() => {
    continueSpy.mockClear();
    breakpoint.set({
      isXSmall: true,
      isSmall: false,
      isMedium: false,
      isLarge: false,
      currentBreakpoint: 'xs',
      screenWidth: 375,
    });

    TestBed.configureTestingModule({
      imports: [InscripcionPersonalStep],
      providers: [
        {
          provide: BreakpointService,
          useValue: { breakpoint },
        },
        {
          provide: InscripcionSurveyFacade,
          useValue: { continue: continueSpy },
        },
      ],
    }).overrideComponent(InscripcionPersonalStep, {
      set: {
        imports: [],
        template: `
          <form (keydown.enter)="onFormEnter($event)" (submit)="onSubmit($event)">
            <input type="radio" name="personal" />
            <button type="submit">Continuar</button>
          </form>
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
  it('uses vertical radio groups on small breakpoints', () => {
    const component = fixture.componentInstance as unknown as {
      radioGroupOrientation: () => 'vertical' | 'horizontal';
    };

    expect(component.radioGroupOrientation()).toBe('vertical');
  });

  it('uses horizontal radio groups from medium and large breakpoints', () => {
    breakpoint.set({
      isXSmall: false,
      isSmall: false,
      isMedium: true,
      isLarge: false,
      currentBreakpoint: 'md',
      screenWidth: 768,
    });
    const component = fixture.componentInstance as unknown as {
      radioGroupOrientation: () => 'vertical' | 'horizontal';
    };

    expect(component.radioGroupOrientation()).toBe('horizontal');

    breakpoint.set({
      isXSmall: false,
      isSmall: false,
      isMedium: false,
      isLarge: true,
      currentBreakpoint: 'lg',
      screenWidth: 1280,
    });

    expect(component.radioGroupOrientation()).toBe('horizontal');
  });
  it('keeps generated survey fields bound in the real template', () => {
    const template = readFileSync(
      'src/app/features/inscriptions/components/inscription-personal-step/inscription-personal-step.html',
      'utf8'
    );

    expect(template).toContain('<app-responsive-select');
    expect(template).toContain('[options]="facade.orientationOptions()"');
    expect(template).toContain('formControlName="orientacion"');
    expect(template).toContain('formControlName="recursaAnioBachillerato"');
    expect(template).toContain('formControlName="vecesRecursaAnioBachillerato"');
    expect(template).toContain('ortNumberInput');
    expect(template).toContain('formControlName="universidadEducacionSuperiorOtro"');
    expect(template).toContain('formControlName="universidadInformadaOtro"');
    expect(template).toContain('ortInput');
    expect(template).toContain('formControlName="apoyoDecision"');
    expect(template).toContain('placeholder="Seleccioná..."');
  });
});
