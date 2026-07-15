import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import {
  OrtAccordionModule,
  OrtBadgeModule,
  OrtButtonModule,
  OrtError,
  OrtIconModule,
} from '@desarrolloort/components';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

import { ScholarshipProposalFacade } from '../../../facades/scholarship-proposal';
import { ScholarshipVariant } from '../../../models/scholarship-personal-forms';
import { ScholarshipEvaluationPeriodSection } from './section/scholarship-evaluation-period-section/scholarship-evaluation-period-section';
import { ScholarshipInscriptionSection } from './section/scholarship-inscription-section/scholarship-inscription-section';

@Component({
  selector: 'app-scholarship-academic-step',
  imports: [
    OrtButtonModule,
    OrtAccordionModule,
    OrtIconModule,
    OrtError,
    ErrorAlert,
    ScholarshipInscriptionSection,
    ScholarshipEvaluationPeriodSection,
    OrtBadgeModule,
  ],
  providers: [ScholarshipProposalFacade],
  templateUrl: './scholarship-academic-step.html',
  styleUrls: ['../../../pages/scholarship-process/scholarship-process.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipAcademicStep {
  private readonly document = inject(DOCUMENT);
  protected readonly facade = inject(ScholarshipProposalFacade);
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
