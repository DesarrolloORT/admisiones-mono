import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtIconModule,
  OrtStatusIconModule,
} from '@desarrolloort/components';

import { ScholarshipVariant } from '../../../models/scholarship-personal-forms';

@Component({
  selector: 'app-scholarship-success-step',
  imports: [OrtStatusIconModule, OrtCardModule, OrtIconModule, OrtButtonModule],
  templateUrl: './scholarship-success-step.html',
  styleUrls: [
    './scholarship-success-step.scss',
    '../scholarship-confirmation-step/scholarship-confirmation-step.scss',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipSuccessStep {
  private readonly router = inject(Router);
  protected goHome(): void {
    void this.router.navigate(['/inicio']);
  }

  readonly variant = input.required<ScholarshipVariant>();
}
