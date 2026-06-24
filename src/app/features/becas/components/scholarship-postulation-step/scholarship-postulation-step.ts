import { ChangeDetectionStrategy, Component } from '@angular/core';
import { OrtAccordionModule, OrtIconModule } from '@desarrolloort/components';

@Component({
  selector: 'app-scholarship-postulation-step',
  imports: [OrtAccordionModule, OrtIconModule],
  templateUrl: './scholarship-postulation-step.html',
  styleUrls: ['../../pages/fbr/fbr.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipPostulationStep {}
