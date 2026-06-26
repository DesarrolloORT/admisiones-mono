import { ChangeDetectionStrategy, Component } from '@angular/core';
import { OrtRadioModule } from '@desarrolloort/components';

@Component({
  selector: 'app-work-history',
  imports: [OrtRadioModule],
  templateUrl: './work-history.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkHistory {}
