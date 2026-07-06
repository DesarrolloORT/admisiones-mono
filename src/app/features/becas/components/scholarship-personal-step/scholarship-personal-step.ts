import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import {
  OrtAccordionModule,
  OrtBadgeModule,
  OrtButton,
  OrtError,
  OrtIconModule,
} from '@desarrolloort/components';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

import { ScholarshipPersonalFacade } from '../../facades/scholarship-personal';
import type { ScholarshipVariant } from '../../models/scholarship-personal-forms';
import { Declaration } from '../postulation-forms/declaration/declaration';
import { EducationInfo } from '../postulation-forms/education-info/education-info';
import { EducationInfoFbr } from '../postulation-forms/education-info-fbr/education-info-fbr';
import { EducationInfoFcl } from '../postulation-forms/education-info-fcl/education-info-fcl';
import { PersonalData } from '../postulation-forms/personal-data/personal-data';
import { WorkHistory } from '../postulation-forms/work-history/work-history';

@Component({
  selector: 'app-scholarship-personal-step',
  imports: [
    OrtAccordionModule,
    OrtIconModule,
    OrtButton,
    OrtError,
    PersonalData,
    EducationInfoFbr,
    Declaration,
    EducationInfo,
    EducationInfoFcl,
    WorkHistory,
    ErrorAlert,
    OrtBadgeModule,
  ],
  providers: [ScholarshipPersonalFacade],
  templateUrl: './scholarship-personal-step.html',
  styleUrls: ['../../pages/scholarship-process/scholarship-process.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipPersonalStep {
  protected readonly facade = inject(ScholarshipPersonalFacade);
  readonly variant = input.required<ScholarshipVariant>();

  constructor() {
    effect(() => this.facade.setVariant(this.variant()));
  }

  protected onContinue(): void {
    this.facade.continue();
  }
}
