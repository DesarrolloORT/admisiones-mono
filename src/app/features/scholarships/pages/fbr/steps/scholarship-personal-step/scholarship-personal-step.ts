import { ChangeDetectionStrategy, Component } from '@angular/core';
import { OrtAccordionModule, OrtButton, OrtIconModule } from '@desarrolloort/components';

import { Declaration } from './sections/declaration/declaration';
import { EducationInfoFbr } from './sections/education-info-fbr/education-info-fbr';
import { PersonalData } from './sections/personal-data/personal-data';

@Component({
  selector: 'app-scholarship-personal-step',
  imports: [
    OrtAccordionModule,
    OrtIconModule,
    OrtButton,
    PersonalData,
    EducationInfoFbr,
    Declaration,
  ],
  templateUrl: './scholarship-personal-step.html',
  styleUrls: ['../../fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipPersonalStep {}
