import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

import { ScholarshipProposalFacade } from '../../facades/scholarship-proposal';
import { ScholarshipVariant } from '../scholarship-personal-step/scholarship-personal-step';
import { ScholarshipRequirementsCard } from '../scholarship-requirements-card/scholarship-requirements-card';

@Component({
  selector: 'app-scholarship-onboarding',
  imports: [OrtIconModule, ScholarshipRequirementsCard, OrtButtonModule],
  templateUrl: './scholarship-onboarding.html',
  styleUrl: './scholarship-onboarding.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipOnboarding {
  protected readonly facade = inject(ScholarshipProposalFacade);

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
