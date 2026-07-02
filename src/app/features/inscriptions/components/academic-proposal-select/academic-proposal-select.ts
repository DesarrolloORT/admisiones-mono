import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit } from '@angular/core';
import type { FormGroup } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtIconModule, OrtRadioModule, OrtSpinnerModule } from '@desarrolloort/components';

import type {
  AcademicProposalForm,
  AcademicProposalOption,
} from '../../../catalogs/models/academic-proposal';
import { AcademicProposalSelection } from '../../../catalogs/services/academic-proposal-selection';
import {
  ResponsiveSelect,
  type ResponsiveSelectOptionGroup,
} from '../responsive-select/responsive-select';

@Component({
  selector: 'app-academic-proposal-select',
  imports: [OrtIconModule, OrtRadioModule, OrtSpinnerModule, ReactiveFormsModule, ResponsiveSelect],
  providers: [AcademicProposalSelection],
  templateUrl: './academic-proposal-select.html',
  styleUrl: './academic-proposal-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AcademicProposalSelect implements OnInit {
  public readonly form = input.required<FormGroup<AcademicProposalForm>>();
  public readonly selection = input(inject(AcademicProposalSelection));

  protected readonly careerOptionGroups = computed(() =>
    groupCareerOptions(this.selection().careerOptions())
  );

  ngOnInit(): void {
    this.selection().connect(this.form());
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
