import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtIconModule, OrtSpinnerModule } from '@desarrolloort/components';

import { InscripcionAcademicStep } from '../../components/inscripcion-academic-step/inscripcion-academic-step';
import { InscripcionConfirmationStep } from '../../components/inscripcion-confirmation-step/inscripcion-confirmation-step';
import { InscripcionDialog } from '../../components/inscripcion-dialog/inscripcion-dialog';
import { InscripcionPersonalStep } from '../../components/inscripcion-personal-step/inscripcion-personal-step';
import { InscripcionRegulationReader } from '../../components/inscripcion-regulation-reader/inscripcion-regulation-reader';
import { InscripcionReservationStep } from '../../components/inscripcion-reservation-step/inscripcion-reservation-step';
import { InscripcionShell } from '../../components/inscripcion-shell/inscripcion-shell';
import { InscripcionSuccessStep } from '../../components/inscripcion-success-step/inscripcion-success-step';
import { InscripcionFlowFacade } from '../../facades/inscripcion-flow.facade';

@Component({
  selector: 'app-inscripcion',
  imports: [
    InscripcionAcademicStep,
    InscripcionConfirmationStep,
    InscripcionDialog,
    InscripcionPersonalStep,
    InscripcionRegulationReader,
    InscripcionReservationStep,
    InscripcionShell,
    InscripcionSuccessStep,
    OrtButtonModule,
    OrtIconModule,
    OrtSpinnerModule,
    RouterLink,
  ],
  providers: [InscripcionFlowFacade],
  templateUrl: './inscripcion.html',
  styleUrl: './inscripcion.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Inscripcion {
  private readonly document = inject(DOCUMENT);
  protected readonly facade = inject(InscripcionFlowFacade);

  constructor() {
    effect(() => {
      const screen = this.facade.screen();
      this.facade.activeSection();
      if (screen === 'confirmacion-pago') return;

      setTimeout(() => this.focusCurrentScreen());
    });
  }

  private focusCurrentScreen(): void {
    const main = this.document.getElementById('main-content');
    main?.focus();
    this.document.defaultView?.scrollTo({ behavior: 'instant', left: 0, top: 0 });
  }
}
