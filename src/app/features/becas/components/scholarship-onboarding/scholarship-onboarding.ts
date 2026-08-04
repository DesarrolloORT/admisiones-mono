import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import {
  ScholarshipRequirementsCard,
  ScholarshipType,
} from '../scholarship-requirements-card/scholarship-requirements-card';

@Component({
  selector: 'app-scholarship-onboarding',
  imports: [OrtIconModule, ScholarshipRequirementsCard, OrtButtonModule],
  templateUrl: './scholarship-onboarding.html',
  styleUrl: './scholarship-onboarding.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipOnboarding {
  readonly variant = input.required<ScholarshipType>();
  readonly title = input.required<string>();
}
