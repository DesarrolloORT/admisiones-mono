import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { OrtButtonModule, OrtIconModule, OrtSnackbarModule } from '@desarrolloort/components';

import { ScholarshipRequirementsCard } from '../../../../components/scholarship-requirements-card/scholarship-requirements-card';
import { ScholarshipVariant } from '../../../../models/scholarship-personal-forms';

@Component({
  selector: 'app-scholarship-onboarding-step',
  imports: [OrtIconModule, ScholarshipRequirementsCard, OrtButtonModule, OrtSnackbarModule],
  templateUrl: './scholarship-onboarding-step.html',
  styleUrl: './scholarship-onboarding-step.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipOnboardingStep {
  readonly variant = input.required<ScholarshipVariant>();

  readonly title = computed(() => {
    const titles: Record<ScholarshipVariant, string> = {
      fbr: 'Fondo de becas de reválidas',
      fexaSin: 'Fondo de Excelencia Académica',
      fexaCon: 'Fondo de Excelencia Académica',
      fbc: 'Fondo de becas concursables',
      fcl: 'Fondo de becas de capacitación laboral',
    };

    return titles[this.variant()];
  });

  readonly continueRequested = output<void>();

  protected continue(): void {
    this.continueRequested.emit();
  }
}
