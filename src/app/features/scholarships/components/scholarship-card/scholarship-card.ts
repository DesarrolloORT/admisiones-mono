import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtChipModule } from '@desarrolloort/components';

interface ScholarshipCardModel {
  title: string;
  description: string;
  requiresExam: boolean;
  route: string;
}

@Component({
  selector: 'app-scholarship-card',
  imports: [OrtButtonModule, OrtChipModule, RouterLink],
  templateUrl: './scholarship-card.html',
  styleUrl: './scholarship-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipCard {
  readonly scholarship = input.required<ScholarshipCardModel>();
  readonly isEnrolled = input.required<boolean>();
  readonly single = input<boolean>(false);
}
