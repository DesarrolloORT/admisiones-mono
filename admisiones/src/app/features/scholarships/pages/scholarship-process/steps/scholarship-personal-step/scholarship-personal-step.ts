import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import {
  OrtAccordionModule,
  OrtBadgeModule,
  OrtButton,
  OrtIconModule,
} from '@desarrolloort/components';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

import { ScholarshipPersonalFacade } from '../../../../facades/scholarship-personal';
import type { ScholarshipVariant } from '../../../../models/scholarship-personal-forms';
import { ScholarshipDeclarationSection } from './sections/scholarship-declaration-section/scholarship-declaration-section';
import { ScholarshipEducationInfoFbrSection } from './sections/scholarship-education-info-fbr-section/scholarship-education-info-fbr-section';
import { ScholarshipEducationInfoFclSection } from './sections/scholarship-education-info-fcl-section/scholarship-education-info-fcl-section';
import { ScholarshipEducationInfoSection } from './sections/scholarship-education-info-section/scholarship-education-info-section';
import { ScholarshipPersonalDataSection } from './sections/scholarship-personal-data-section/scholarship-personal-data-section';
import { ScholarshipWorkHistorySection } from './sections/scholarship-work-history-section/scholarship-work-history-section';

@Component({
  selector: 'app-scholarship-personal-step',
  imports: [
    OrtAccordionModule,
    OrtIconModule,
    OrtButton,
    ScholarshipPersonalDataSection,
    ScholarshipEducationInfoFbrSection,
    ScholarshipDeclarationSection,
    ScholarshipEducationInfoSection,
    ScholarshipEducationInfoFclSection,
    ScholarshipWorkHistorySection,
    ErrorAlert,
    OrtBadgeModule,
  ],
  providers: [ScholarshipPersonalFacade],
  templateUrl: './scholarship-personal-step.html',
  styleUrls: ['../../scholarship-process.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipPersonalStep {
  private readonly document = inject(DOCUMENT);
  protected readonly facade = inject(ScholarshipPersonalFacade);
  readonly variant = input.required<ScholarshipVariant>();

  constructor() {
    effect(() => this.facade.setVariant(this.variant()));
  }

  protected onContinue(): void {
    this.facade.continue();

    if (this.facade.showErrorAlert()) {
      this.document.defaultView?.scrollTo({ behavior: 'smooth', left: 0, top: 0 });
    }
  }
}
