import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtCheckboxModule,
  OrtError,
  OrtIconModule,
  OrtSnackbarModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
import { ScholarshipVariant } from '../../models/scholarship-personal-forms';
import { TermsAndConditions } from '../terms-and-conditions/terms-and-conditions';

@Component({
  selector: 'app-scholarship-confirmation-step',
  imports: [
    OrtIconModule,
    OrtCardModule,
    OrtButtonModule,
    OrtSnackbarModule,
    OrtCheckboxModule,
    TermsAndConditions,
    OrtError,
  ],
  templateUrl: './scholarship-confirmation-step.html',
  styleUrls: [
    './scholarship-confirmation-step.scss',
    '../../pages/scholarship-process/scholarship-process.scss',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipConfirmationStep {
  private readonly document = inject(DOCUMENT);
  private readonly process = inject(ScholarshipProcessFacade);

  showTermsAndConditions = signal(false);
  termsAccepted = signal(false);
  readonly termsError = signal(false);

  onTermsAcceptedChange(value: string | boolean): void {
    const checked = value === true || value === 'true';

    this.termsAccepted.set(checked);

    if (checked) {
      this.termsError.set(false);
    }
  }
  openTermsAndConditions(): void {
    this.showTermsAndConditions.set(true);
    this.document.defaultView?.scrollTo({ behavior: 'instant', left: 0, top: 0 });
  }

  acceptTermsAndConditions(): void {
    this.termsError.set(false);
    this.termsAccepted.set(true);
    this.showTermsAndConditions.set(false);
  }

  readonly confirmApplication = output<void>();

  protected onConfirmApplication(): void {
    if (!this.termsAccepted()) {
      this.termsError.set(true);
      return;
    }
    this.termsError.set(false);
    this.confirmApplication.emit();
  }

  protected onBack(): void {
    this.process.back();
  }

  readonly variant = input.required<ScholarshipVariant>();

  readonly isSidebarLayout = computed(() => ['fexaCon', 'fexaSin', 'fbc'].includes(this.variant()));

  readonly isStackedLayout = computed(() => ['fbr', 'fcl'].includes(this.variant()));

  private readonly breakpointService = inject(BreakpointService);

  readonly cardVariant = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'outlined' : 'elevated';
  });
}
