import { readFileSync } from 'node:fs';

import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { vi } from 'vitest';

import { InscripcionSurveyFacade } from '../../../facades/inscription-survey';
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

  it('uses horizontal radio groups from the desktop breakpoint', () => {
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

    expect(component.radioGroupOrientation()).toBe('vertical');

    breakpoint.set({
      isXSmall: false,
      isSmall: false,
      isMedium: true,
      isLarge: false,
      currentBreakpoint: 'md',
      screenWidth: 840,
    });

    expect(component.radioGroupOrientation()).toBe('horizontal');
  });
  it('keeps generated survey fields bound in the section templates', () => {
    const education = readFileSync(
      'src/app/features/inscriptions/components/steps/inscription-personal-step/sections/inscription-education-section/inscription-education-section.html',
      'utf8'
    );
    const decision = readFileSync(
      'src/app/features/inscriptions/components/steps/inscription-personal-step/sections/inscription-academic-decision-section/inscription-academic-decision-section.html',
      'utf8'
    );

    expect(education).toContain('<app-responsive-select');
    expect(education).toContain('[options]="facade.options.orientationOptions()"');
    expect(education).toContain('formControlName="orientacion"');
    expect(education).toContain('formControlName="recursaAnioBachillerato"');
    expect(education).toContain('formControlName="vecesRecursaAnioBachillerato"');
    expect(education).toContain('ortNumberInput');
    expect(education).toContain('formControlName="universidadEducacionSuperiorOtro"');
    expect(education).toContain('ortInput');
    expect(decision).toContain('formControlName="universidadInformadaOtro"');
    expect(decision).toContain('formControlName="apoyoDecision"');
    expect(decision).toContain('placeholder="Seleccioná..."');
  });
});
