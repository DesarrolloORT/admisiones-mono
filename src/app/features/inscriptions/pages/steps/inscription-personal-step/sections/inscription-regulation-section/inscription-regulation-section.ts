import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { OrtButtonModule, OrtCheckboxModule } from '@desarrolloort/components';

import { InscripcionSurveyFacade } from '../../../../../facades/inscription-survey';

@Component({
  selector: 'app-inscription-regulation-section',
  imports: [DatePipe, OrtButtonModule, OrtCheckboxModule, ReactiveFormsModule],
  templateUrl: './inscription-regulation-section.html',
  styleUrl: '../../../../layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionRegulationSection {
  protected readonly facade = inject(InscripcionSurveyFacade);
}
