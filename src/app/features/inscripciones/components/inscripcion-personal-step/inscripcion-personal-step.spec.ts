import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';
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
          provide: InscripcionFlowFacade,
          useValue: { continue: continueSpy },
        },
      ],
    }).overrideComponent(InscripcionPersonalStep, {
      set: {
        imports: [],
        template:
          '<form (submit)="onSubmit($event)"><button type="submit">Continuar</button></form>',
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
});
