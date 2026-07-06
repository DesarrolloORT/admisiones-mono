import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import {
  OrtButtonModule,
  OrtCardModule,
  OrtIconModule,
  OrtStatusIconModule,
} from '@desarrolloort/components';
import { HomeHeader } from 'src/app/shared/ui/home-header/home-header';

import { ScholarshipVariant } from '../../models/scholarship-personal-forms';

@Component({
  selector: 'app-scholarship-success',
  imports: [OrtStatusIconModule, HomeHeader, OrtCardModule, OrtIconModule, OrtButtonModule],
  templateUrl: './scholarship-success.html',
  styleUrls: [
    './scholarship-success.scss',
    '../scholarship-confirmation-step/scholarship-confirmation-step.scss',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipSuccess {
  private readonly router = inject(Router);
  protected goHome(): void {
    void this.router.navigate(['/inicio']);
  }

  readonly variant = input.required<ScholarshipVariant>();
}
