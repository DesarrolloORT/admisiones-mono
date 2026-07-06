import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import {
  OrtAccordionModule,
  OrtButtonModule,
  OrtError,
  OrtIconModule,
} from '@desarrolloort/components';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

import { ScholarshipProposalFacade } from '../../facades/scholarship-proposal';
import { EvaluationPeriod } from '../postulation-forms/evaluation-period/evaluation-period';
import { Inscription } from '../postulation-forms/inscription/inscription';
import { ScholarshipVariant } from '../scholarship-personal-step/scholarship-personal-step';

@Component({
  selector: 'app-scholarship-academic-step',
  imports: [
    OrtButtonModule,
    OrtAccordionModule,
    OrtIconModule,
    OrtError,
    ErrorAlert,
    Inscription,
    EvaluationPeriod,
  ],
  providers: [ScholarshipProposalFacade],
  templateUrl: './scholarship-academic-step.html',
  styleUrls: ['../../pages/fbr/fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipAcademicStep {
  protected readonly facade = inject(ScholarshipProposalFacade);
  readonly variant = input.required<ScholarshipVariant>();

  constructor() {
    effect(() => this.facade.setVariant(this.variant()));
  }

  protected onContinue(): void {
    this.facade.continue();
  }
}
