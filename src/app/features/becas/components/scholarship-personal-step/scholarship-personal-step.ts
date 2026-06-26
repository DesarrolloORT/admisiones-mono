import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { OrtAccordionModule, OrtButton, OrtIconModule } from '@desarrolloort/components';

import { Declaration } from '../postulation-forms/declaration/declaration';
import { EducationInfo } from '../postulation-forms/education-info/education-info';
import { EducationInfoFbr } from '../postulation-forms/education-info-fbr/education-info-fbr';
import { EducationInfoFcl } from '../postulation-forms/education-info-fcl/education-info-fcl';
import { PersonalData } from '../postulation-forms/personal-data/personal-data';
import { WorkHistory } from '../postulation-forms/work-history/work-history';

export type ScholarshipVariant = 'fbr' | 'fbc' | 'fcl' | 'fexaCon' | 'fexaSin';

@Component({
  selector: 'app-scholarship-personal-step',
  imports: [
    OrtAccordionModule,
    OrtIconModule,
    OrtButton,
    PersonalData,
    EducationInfoFbr,
    Declaration,
    EducationInfo,
    EducationInfoFcl,
    WorkHistory,
  ],
  templateUrl: './scholarship-personal-step.html',
  styleUrls: ['../../pages/fbr/fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipPersonalStep {
  readonly variant = input.required<ScholarshipVariant>();
}
