import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { OrtButtonModule, OrtChipModule } from '@desarrolloort/components';

interface ScholarshipCardModel {
  title: string;
  description: string;
  test: boolean;
}

@Component({
  selector: 'app-scholarship-card',
  imports: [OrtButtonModule, OrtChipModule],
  templateUrl: './scholarship-card.html',
  styleUrl: './scholarship-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipCard {
  readonly beca = input.required<ScholarshipCardModel>();
  readonly inscripto = input.required<boolean>();
  readonly single = input<boolean>(false);
}
