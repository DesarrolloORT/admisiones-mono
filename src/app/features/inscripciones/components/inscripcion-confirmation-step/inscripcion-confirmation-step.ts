import { ChangeDetectionStrategy, Component, ElementRef, inject, viewChild } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtRadioModule,
  OrtSelectModule,
} from '@desarrolloort/components';

import { InscripcionPaymentFacade } from '../../facades/inscripcion-payment';
import { InscripcionDialog } from '../inscripcion-dialog/inscripcion-dialog';
import { InscripcionErrorAlert } from '../inscripcion-error-alert/inscripcion-error-alert';

@Component({
  selector: 'app-inscripcion-confirmation-step',
  imports: [
    InscripcionDialog,
    InscripcionErrorAlert,
    OrtButtonModule,
    OrtCardModule,
    OrtFormFieldModule,
    OrtIconModule,
    OrtRadioModule,
    OrtSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './inscripcion-confirmation-step.html',
  styleUrls: ['../../pages/inscripcion/inscripcion.scss', './inscripcion-confirmation-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionConfirmationStep {
  protected readonly facade = inject(InscripcionPaymentFacade);
  private readonly paymentSubmit = viewChild<ElementRef<HTMLButtonElement>>('paymentSubmit');

  protected closeConfirmationDialog(): void {
    this.facade.cancelConfirmation();
    this.restorePaymentFocus();
  }

  private restorePaymentFocus(): void {
    const focusSubmit = () => this.paymentSubmit()?.nativeElement.focus({ preventScroll: true });

    focusSubmit();
    setTimeout(focusSubmit, 0);
    setTimeout(focusSubmit, 50);
    setTimeout(focusSubmit, 150);
  }

  protected onFormEnter(event: Event): void {
    if (event.target instanceof HTMLInputElement && event.target.type === 'radio') {
      event.preventDefault();
    }
  }
}
