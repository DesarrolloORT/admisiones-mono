import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { OrtAccordionModule, OrtButton, OrtIconModule } from '@desarrolloort/components';
import { ErrorAlert } from 'src/app/shared/ui/error-alert/error-alert';

import { ScholarshipProcessFacade } from '../../facades/scholarship-process';
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
    ErrorAlert,
  ],
  templateUrl: './scholarship-personal-step.html',
  styleUrls: ['../../pages/fbr/fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipPersonalStep {
  protected readonly showErrorAlert = signal(false);
  private readonly facade = inject(ScholarshipProcessFacade);
  readonly variant = input.required<ScholarshipVariant>();
  private readonly personalDataComponent = viewChild(PersonalData);
  private readonly workHistoryComponent = viewChild(WorkHistory);
  private readonly declarationComponent = viewChild(Declaration);
  private readonly educationInfoComponent = viewChild(EducationInfo);
  private readonly educationInfoFbrComponent = viewChild(EducationInfoFbr);
  private readonly educationInfoFclComponent = viewChild(EducationInfoFcl);

  protected onContinue(): void {
    const variant = this.variant();

    const isPersonalDataValid =
      variant === 'fcl' || variant === 'fexaSin'
        ? true
        : (this.personalDataComponent()?.validateAndMarkTouched() ?? false);

    const isWorkHistoryValid =
      variant === 'fcl' ? (this.workHistoryComponent()?.validateAndMarkTouched() ?? false) : true;

    const isDeclarationValid =
      variant === 'fexaSin'
        ? true
        : (this.declarationComponent()?.validateAndMarkTouched() ?? false);

    let isEducationInfoValid = true;

    if (variant === 'fbr') {
      isEducationInfoValid = this.educationInfoFbrComponent()?.validateAndMarkTouched() ?? false;
    } else if (variant === 'fcl') {
      isEducationInfoValid = this.educationInfoFclComponent()?.validateAndMarkTouched() ?? false;
    } else {
      isEducationInfoValid = this.educationInfoComponent()?.validateAndMarkTouched() ?? false;
    }

    const isValid =
      isPersonalDataValid && isWorkHistoryValid && isDeclarationValid && isEducationInfoValid;

    this.showErrorAlert.set(!isValid);

    if (!isValid) {
      return;
    }

    this.facade.continue();
  }
}
