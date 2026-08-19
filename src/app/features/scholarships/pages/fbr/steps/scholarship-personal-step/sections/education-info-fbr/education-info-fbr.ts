import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  OrtFileUploaderModule,
  OrtFormFieldModule,
  OrtInput,
  OrtRadioModule,
} from '@desarrolloort/components';

@Component({
  selector: 'app-education-info-fbr',
  imports: [OrtFormFieldModule, OrtFileUploaderModule, OrtRadioModule, OrtInput],
  templateUrl: './education-info-fbr.html',
  styleUrl: '../../../../fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EducationInfoFbr {}
