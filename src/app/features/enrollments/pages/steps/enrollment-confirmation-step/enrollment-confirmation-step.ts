import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  Injector,
  viewChild,
} from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtDialog,
  OrtIconModule,
  OrtRadioModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';
import { ResponsiveSelect } from 'src/app/shared/ui/responsive-select/responsive-select';

import { EnrollmentPaymentFacade } from '../../../facades/enrollment-payment';
import { EnrollmentSeminarsSummary } from './sections/enrollment-seminars-summary/enrollment-seminars-summary';

@Component({
  selector: 'app-enrollment-confirmation-step',
  imports: [
    ErrorAlert,
    EnrollmentSeminarsSummary,
    OrtButtonModule,
    OrtCardModule,
    OrtDialog,
    OrtIconModule,
    OrtRadioModule,
    ReactiveFormsModule,
    ResponsiveSelect,
  ],
  templateUrl: './enrollment-confirmation-step.html',
  styleUrls: ['../../layout.scss', './enrollment-confirmation-step.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnrollmentConfirmationStep {
  protected readonly facade = inject(EnrollmentPaymentFacade);
  private readonly breakpointService = inject(BreakpointService);
  private readonly injector = inject(Injector);
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
    afterNextRender(focusSubmit, { injector: this.injector });
  }

  protected onFormEnter(event: Event): void {
    if (event.target instanceof HTMLInputElement && event.target.type === 'radio') {
      event.preventDefault();
    }
  }
}
