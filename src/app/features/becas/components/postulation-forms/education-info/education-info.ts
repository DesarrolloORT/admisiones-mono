import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  OrtFileUploaderModule,
  OrtFormFieldModule,
  OrtInputModule,
} from '@desarrolloort/components';

@Component({
  selector: 'app-education-info',
  imports: [OrtFileUploaderModule, OrtInputModule, OrtFormFieldModule],
  templateUrl: './education-info.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EducationInfo {}
