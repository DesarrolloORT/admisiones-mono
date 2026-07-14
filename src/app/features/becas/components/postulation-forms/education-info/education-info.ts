import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtError,
  OrtFileUploaderChange,
  OrtFileUploaderModule,
  OrtFormFieldModule,
  OrtInputModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';

import { ScholarshipPersonalFacade } from '../../../facades/scholarship-personal';

@Component({
  selector: 'app-education-info',
  imports: [
    OrtFileUploaderModule,
    OrtInputModule,
    OrtFormFieldModule,
    OrtError,
    ReactiveFormsModule,
  ],
  templateUrl: './education-info.html',
  styleUrl: '../../../pages/scholarship-process/scholarship-process.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EducationInfo {
  protected readonly facade = inject(ScholarshipPersonalFacade);
  private readonly breakpointService = inject(BreakpointService);

  protected readonly educationInfoForm = this.facade.educationInfoForm;
  protected readonly averageSecondYearControl = this.educationInfoForm.controls.averageSecondYear;
  protected readonly averageThirdYearControl = this.educationInfoForm.controls.averageThirdYear;
  protected readonly certificateFileControl = this.educationInfoForm.controls.certificateFile;

  protected readonly fileUploaderDisplay = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall ? 'inline' : 'block';
  });

  public onCertificateFilesChanged(change: OrtFileUploaderChange): void {
    this.facade.setFileFlag(this.certificateFileControl, change);
  }
}
