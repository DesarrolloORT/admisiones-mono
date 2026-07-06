import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrtError,
  OrtFormFieldModule,
  OrtInputModule,
  OrtRadioModule,
} from '@desarrolloort/components';

import { ScholarshipProposalFacade } from '../../../facades/scholarship-proposal';

@Component({
  selector: 'app-inscription',
  imports: [ReactiveFormsModule, OrtFormFieldModule, OrtInputModule, OrtRadioModule, OrtError],
  templateUrl: './inscription.html',
  styleUrl: '../../../pages/fbr/fbr.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Inscription {
  protected readonly facade = inject(ScholarshipProposalFacade);
  protected readonly inscriptionSection = this.facade.inscriptionSection;
  protected readonly selectionControl = this.facade.selectionControl;
  protected readonly applicationModeControl = this.facade.applicationModeControl;
}
