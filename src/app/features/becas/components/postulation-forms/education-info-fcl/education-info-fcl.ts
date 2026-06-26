import { ChangeDetectionStrategy, Component } from '@angular/core';
import { OrtRadioModule } from '@desarrolloort/components';

@Component({
  selector: 'app-education-info-fcl',
  imports: [OrtRadioModule],
  templateUrl: './education-info-fcl.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EducationInfoFcl {}
