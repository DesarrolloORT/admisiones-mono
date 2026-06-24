import { ChangeDetectionStrategy, Component } from '@angular/core';
import { OrtAccordionModule, OrtRadioModule } from '@desarrolloort/components';

@Component({
  selector: 'app-personal-data',
  imports: [OrtAccordionModule, OrtRadioModule],
  templateUrl: './personal-data.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonalData {}
