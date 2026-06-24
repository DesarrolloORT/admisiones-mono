import { ChangeDetectionStrategy, Component } from '@angular/core';
import { OrtAccordionModule, OrtButton, OrtIconModule } from '@desarrolloort/components';

import { Declaration } from '../postulation-forms/declaration/declaration';
import { EducationInfoFbr } from '../postulation-forms/education-info-fbr/education-info-fbr';
import { PersonalData } from '../postulation-forms/personal-data/personal-data';

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
  styleUrls: ['../../pages/fbr/fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipPersonalStep {}
