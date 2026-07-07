import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  viewChild,
} from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtIconModule,
  OrtRadioModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';
import { ResponsiveSelect } from 'src/app/shared/ui/responsive-select/responsive-select';

import { InscripcionPaymentFacade } from '../../facades/inscription-payment';
import { InscripcionDialog } from '../inscription-dialog/inscription-dialog';

@Component({
  selector: 'app-inscription-confirmation-step',
  imports: [
    InscripcionDialog,
    ErrorAlert,
    OrtButtonModule,
    OrtCardModule,
    OrtIconModule,
    OrtRadioModule,
    ReactiveFormsModule,
    ResponsiveSelect,
  ],
  templateUrl: './inscription-confirmation-step.html',
  styleUrls: ['../../pages/inscription/inscription.scss', './inscription-confirmation-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionConfirmationStep {
  protected readonly facade = inject(InscripcionPaymentFacade);
  private readonly breakpointService = inject(BreakpointService);
  private readonly paymentSubmit = viewChild<ElementRef<HTMLButtonElement>>('paymentSubmit');

  protected readonly divider = true;

  protected readonly radioGroupOrientation = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'vertical' : 'horizontal';
  });

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
