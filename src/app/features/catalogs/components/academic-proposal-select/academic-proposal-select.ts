import { ChangeDetectionStrategy, Component, inject, input, OnInit } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import {
  OrtFormFieldModule,
  OrtIconModule,
  OrtRadioModule,
  OrtSelectModule,
  OrtSpinnerModule,
} from '@desarrolloort/components';

import type { AcademicProposalForm } from '../../models/academic-proposal';
import { AcademicProposalSelection } from '../../services/academic-proposal-selection';

@Component({
  selector: 'app-academic-proposal-select',
  imports: [
    OrtFormFieldModule,
    OrtIconModule,
    OrtRadioModule,
    OrtSelectModule,
    OrtSpinnerModule,
    ReactiveFormsModule,
  ],
  providers: [AcademicProposalSelection],
  templateUrl: './academic-proposal-select.html',
  styleUrl: './academic-proposal-select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AcademicProposalSelect implements OnInit {
  public readonly form = input.required<FormGroup<AcademicProposalForm>>();
  public readonly selection = input(inject(AcademicProposalSelection));

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
