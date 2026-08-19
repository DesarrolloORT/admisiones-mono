import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
} from '@desarrolloort/components';

@Component({
  selector: 'app-review-scholarship-result',
  imports: [OrtIconModule, OrtFormFieldModule, OrtInputModule, OrtButtonModule],
  templateUrl: './review-scholarship-result.html',
  styleUrl: './review-scholarship-result.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewScholarshipResult {
  protected readonly items = [
    { icon: 'workspace_premium', label: 'Beca', value: 'Fondo de Excelencia Académica' },
    { icon: 'fact_check', label: 'Resultado de la prueba', value: '320/1600 pts.' },
    { icon: 'percent', label: 'Beneficio', value: 'Beca del 25%' },
  ];

  readonly closeReview = output<void>();

  onCancelarClick() {
    this.closeReview.emit();
  }
}
