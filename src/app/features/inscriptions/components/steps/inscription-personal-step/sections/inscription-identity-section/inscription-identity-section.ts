import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtCheckboxModule,
  OrtDatePickerModule,
  OrtFileUploaderModule,
  OrtFormFieldModule,
} from '@desarrolloort/components';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';

@Component({
  selector: 'app-inscription-identity-section',
  imports: [
    OrtCheckboxModule,
    OrtDatePickerModule,
    OrtFileUploaderModule,
    OrtFormFieldModule,
    ReactiveFormsModule,
  ],
  templateUrl: './inscription-identity-section.html',
  styleUrls: [
    '../../../../../pages/inscription/inscription.scss',
    '../../inscription-personal-step.scss',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionIdentitySection {
  protected readonly facade = inject(InscripcionSurveyFacade);
}
