import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit } from '@angular/core';
import type { FormGroup } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtCardModule,
  OrtIconModule,
  OrtRadioModule,
  OrtSpinnerModule,
} from '@desarrolloort/components';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import {
  ResponsiveSelect,
  type ResponsiveSelectOptionGroup,
} from 'src/app/shared/ui/responsive-select/responsive-select';

import type { AcademicProposalForm, AcademicProposalOption } from '../../models/academic-proposal';
import { AcademicProposalSelection } from '../../services/academic-proposal-selection';

@Component({
  selector: 'app-academic-proposal-select',
  imports: [
    OrtCardModule,
    OrtIconModule,
    OrtRadioModule,
    OrtSpinnerModule,
    ReactiveFormsModule,
    ResponsiveSelect,
  ],
  providers: [AcademicProposalSelection],
  templateUrl: './academic-proposal-select.html',
  styleUrl: './academic-proposal-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AcademicProposalSelect implements OnInit {
  public readonly form = input.required<FormGroup<AcademicProposalForm>>();
  public readonly selection = input(inject(AcademicProposalSelection));
  private readonly breakpointService = inject(BreakpointService);

  protected readonly isMobile = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall;
  });

  protected readonly careerOptionGroups = computed(() =>
    groupCareerOptions(this.selection().careerOptions())
  );

  ngOnInit(): void {
    this.selection().connect(this.form());
  }

  // El selector de Actualización profesional (AP) permanece oculto hasta elegir un programa.
  protected hasProgramSelected(): boolean {
    return !!this.form().controls.carrera.value;
  }

  protected proposalTypeErrorId(): string | null {
    const control = this.form().controls.tipoPropuesta;
    return control.touched && control.hasError('required') ? 'academic-proposal-type-error' : null;
  }

  protected proposalTypeInvalid(): boolean {
    return this.proposalTypeErrorId() !== null;
  }

  protected proposalTypeOptionDescription(optionValue: string): string {
    return ['academic-proposal-type-hint-' + optionValue, this.proposalTypeErrorId()]
      .filter(Boolean)
      .join(' ');
  }
}

function groupCareerOptions(
  options: readonly AcademicProposalOption[]
): readonly ResponsiveSelectOptionGroup[] {
  const groups = new Map<string, AcademicProposalOption[]>();
  for (const option of options) {
    const school = option.school?.trim() || 'Sin escuela';
    groups.set(school, [...(groups.get(school) ?? []), option]);
  }

  return [...groups].map(([label, groupOptions]) => ({ label, options: groupOptions }));
}
